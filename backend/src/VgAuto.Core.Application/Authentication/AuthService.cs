using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Email;
using VgAuto.Core.Application.Model;
using VgAuto.Core.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VgAuto.Core.Application.Authentication
{
    public enum AuthStatus
    {
        Success,
        CodeRequired,
        InvalidCredentials,
        Locked,
        NoEmail,
        InvalidCode,
        NoAccount,
        Ambiguous,
        Disabled,
        InvalidPassword,
        EmailFailed,
        TooManyRequests,
        AccountDisabled
    }

    public record AuthResult(AuthStatus Status, AuthTokens Tokens = null, Guid? ChallengeId = null, string EmailHint = null, string Message = null)
    {
        public static AuthResult Fail(AuthStatus status, string message = null) => new(status, Message: message);

        public static AuthResult AccountDisabled() =>
            Fail(AuthStatus.AccountDisabled, "This account has been disabled. Contact an administrator.");
    }

    /// <summary>
    /// Login flows: password (+ optional emailed code), password reset by emailed code,
    /// and Microsoft sign in (linked to an existing user by proving ownership of the user's email).
    /// </summary>
    public class AuthService
    {
        public const string MicrosoftProvider = "microsoft";

        private readonly IUserRepository users;
        private readonly IAuthChallengeRepository challenges;
        private readonly IExternalLoginRepository externalLogins;
        private readonly IEmailSender email;
        private readonly AuthTokenService tokens;
        private readonly ITenantConfigService tenantConfig;
        private readonly IMicrosoftIdentityClient microsoft;
        private readonly AuthenticationOptions options;
        private readonly byte[] codeKey;
        private readonly ILogger<AuthService> logger;

        public AuthService(IUserRepository users, IAuthChallengeRepository challenges, IExternalLoginRepository externalLogins,
            IEmailSender email, AuthTokenService tokens, ITenantConfigService tenantConfig, IMicrosoftIdentityClient microsoft,
            IOptions<AuthenticationOptions> options, IOptions<JwtOptions> jwt, ILogger<AuthService> logger)
        {
            this.users = users;
            this.challenges = challenges;
            this.externalLogins = externalLogins;
            this.email = email;
            this.tokens = tokens;
            this.tenantConfig = tenantConfig;
            this.microsoft = microsoft;
            this.options = options.Value;
            this.codeKey = SHA256.HashData(Encoding.UTF8.GetBytes("login-code:" + jwt.Value.Secret));
            this.logger = logger;
        }

        public AuthenticationOptions Options => options;

        // ---------------------------------------------------------------- password login

        public async Task<AuthResult> PasswordLoginAsync(string userName, string password)
        {
            var user = string.IsNullOrWhiteSpace(userName) ? null : users.GetBy(userName.Trim());
            var now = DateTime.UtcNow;

            if (user != null && user.IsLockedOut(now))
            {
                logger.LogWarning("Authentication refused, account locked: {user}", userName);
                return AuthResult.Fail(AuthStatus.Locked);
            }

            if (user == null || !PasswordHasher.verifyHash(password ?? string.Empty, user.Password))
            {
                if (user != null)
                {
                    user.LoginFailed(now);
                    users.Update(user);
                }
                logger.LogInformation("Authentication failure: {user}", userName);
                return AuthResult.Fail(AuthStatus.InvalidCredentials);
            }

            if (user.FailedLoginCount > 0 || user.LockedUntil != null)
            {
                user.LoginSucceeded();
                users.Update(user);
            }

            if (user.Disabled)
            {
                logger.LogInformation("Authentication refused, account disabled: {user}", userName);
                return AuthResult.AccountDisabled();
            }

            if (!options.EmailCode.RequireForPasswordLogin)
            {
                return new AuthResult(AuthStatus.Success, tokens.Issue(user, "pwd"));
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                if (options.EmailCode.AllowUsersWithoutEmail)
                {
                    logger.LogWarning("User {user} has no email address, login code skipped", user.UserName);
                    return new AuthResult(AuthStatus.Success, tokens.Issue(user, "pwd"));
                }
                return AuthResult.Fail(AuthStatus.NoEmail, "No email address is set for this account. Ask an administrator to add one.");
            }

            return await StartChallengeAsync(user, ChallengePurpose.Login, null);
        }

        // ---------------------------------------------------------------- codes

        public async Task<AuthResult> VerifyCodeAsync(Guid challengeId, string code)
        {
            var (challenge, status) = await CheckCodeAsync(challengeId, code, ChallengePurpose.Login, ChallengePurpose.LinkExternal);
            if (status != AuthStatus.Success) return AuthResult.Fail(status);

            var user = users.GetBy(challenge.User);
            if (user == null) return AuthResult.Fail(AuthStatus.InvalidCode);
            if (user.IsLockedOut(DateTime.UtcNow)) return AuthResult.Fail(AuthStatus.Locked);
            if (user.Disabled) return AuthResult.AccountDisabled();

            if (!user.Validated)
            {
                user.MarkEmailValidated();
                users.Update(user);
            }

            if (challenge.Purpose == ChallengePurpose.LinkExternal)
            {
                var parts = (challenge.Payload ?? string.Empty).Split('|', 3);
                if (parts.Length < 2) return AuthResult.Fail(AuthStatus.InvalidCode);
                if (await externalLogins.FindAsync(parts[0], parts[1]) == null)
                {
                    await externalLogins.AddAsync(new ExternalLogin(parts[0], parts[1], user.Id.TenantName, user.Id.EmployeeId,
                        parts.Length > 2 ? parts[2] : null, DateTime.UtcNow));
                    logger.LogInformation("Linked {provider} account to user {user}", parts[0], user.UserName);
                }
                return new AuthResult(AuthStatus.Success, tokens.Issue(user, parts[0]));
            }

            return new AuthResult(AuthStatus.Success, tokens.Issue(user, "pwd+otp"));
        }

        public async Task<AuthResult> ResendCodeAsync(Guid challengeId)
        {
            var challenge = await challenges.GetAsync(challengeId);
            if (challenge == null || challenge.ConsumedAt != null || challenge.ExpiresAt <= DateTime.UtcNow)
                return AuthResult.Fail(AuthStatus.InvalidCode);
            if (challenge.Sends >= options.EmailCode.MaxSends)
                return AuthResult.Fail(AuthStatus.TooManyRequests);

            var user = users.GetBy(challenge.User);
            if (user == null || string.IsNullOrWhiteSpace(user.Email) || user.Disabled) return AuthResult.Fail(AuthStatus.InvalidCode);

            var code = NewCode();
            challenge.CodeHash = HashCode(challenge.Id, code);
            challenge.Sends++;
            challenge.Attempts = 0;
            challenge.ExpiresAt = DateTime.UtcNow.AddMinutes(options.EmailCode.CodeLifetimeMinutes);
            await challenges.UpdateAsync(challenge);

            if (!await SendCodeAsync(user, challenge.Purpose, code)) return AuthResult.Fail(AuthStatus.EmailFailed);
            return new AuthResult(AuthStatus.CodeRequired, ChallengeId: challenge.Id, EmailHint: MaskEmail(user.Email));
        }

        // ---------------------------------------------------------------- password reset

        /// <summary>
        /// Starts a password reset. Always returns a challenge id so the response does not reveal
        /// whether the account exists.
        /// </summary>
        public async Task<AuthResult> ForgotPasswordAsync(string login)
        {
            if (!options.PasswordReset.Enabled) return AuthResult.Fail(AuthStatus.Disabled);

            User user = null;
            if (!string.IsNullOrWhiteSpace(login))
            {
                user = users.GetBy(login.Trim());
                if (user == null && login.Contains('@'))
                {
                    var byEmail = users.GetAllByEmail(login);
                    if (byEmail.Count == 1) user = byEmail[0];
                }
            }

            if (user == null || string.IsNullOrWhiteSpace(user.Email) || user.Disabled)
            {
                logger.LogInformation("Password reset requested for unknown, disabled or email-less account: {login}", login);
                return new AuthResult(AuthStatus.CodeRequired, ChallengeId: Guid.NewGuid());
            }

            var result = await StartChallengeAsync(user, ChallengePurpose.PasswordReset, null);
            // do not reveal the (masked) address to anonymous callers
            return result with { EmailHint = null };
        }

        public async Task<AuthResult> ResetPasswordAsync(Guid challengeId, string code, string newPassword)
        {
            if (!options.PasswordReset.Enabled) return AuthResult.Fail(AuthStatus.Disabled);

            var challenge = await challenges.GetAsync(challengeId);
            if (challenge == null) return AuthResult.Fail(AuthStatus.InvalidCode);
            var user = users.GetBy(challenge.User);
            if (user == null) return AuthResult.Fail(AuthStatus.InvalidCode);
            if (user.Disabled) return AuthResult.AccountDisabled();

            // check the password first so a weak password does not burn the code
            var policyError = PasswordPolicy.Validate(newPassword, user.UserName);
            if (policyError != null) return AuthResult.Fail(AuthStatus.InvalidPassword, policyError);

            var (_, status) = await CheckCodeAsync(challengeId, code, ChallengePurpose.PasswordReset);
            if (status != AuthStatus.Success) return AuthResult.Fail(status);

            user.ChangePassword(PasswordHasher.getHash(newPassword));
            if (!user.Validated) user.MarkEmailValidated();
            users.Update(user);
            logger.LogInformation("Password reset for user {user}", user.UserName);
            return new AuthResult(AuthStatus.Success);
        }

        // ---------------------------------------------------------------- Microsoft

        public async Task<AuthResult> MicrosoftLoginAsync(string code, string codeVerifier, string redirectUri, string nonce)
        {
            if (!options.Microsoft.Enabled) return AuthResult.Fail(AuthStatus.Disabled);

            ExternalIdentity identity;
            try
            {
                identity = await microsoft.RedeemCodeAsync(code, codeVerifier, redirectUri, nonce);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Microsoft sign in failed");
                return AuthResult.Fail(AuthStatus.InvalidCredentials, "Microsoft sign in failed.");
            }

            var linked = await externalLogins.FindAsync(identity.Provider, identity.Subject);
            if (linked != null)
            {
                var linkedUser = users.GetBy(new UserIdentifier(linked.TenantName, linked.EmployeeId));
                if (linkedUser == null) return AuthResult.Fail(AuthStatus.NoAccount);
                if (linkedUser.IsLockedOut(DateTime.UtcNow)) return AuthResult.Fail(AuthStatus.Locked);
                if (linkedUser.Disabled) return AuthResult.AccountDisabled();
                return new AuthResult(AuthStatus.Success, tokens.Issue(linkedUser, identity.Provider));
            }

            // First sign in with this Microsoft account: find the user by email and prove that the person
            // controls the user's mailbox before linking (the email claim of a Microsoft token alone is not
            // proof of ownership for arbitrary tenants).
            var candidates = users.GetAllByEmail(identity.Email);
            if (candidates.Count == 0)
                return AuthResult.Fail(AuthStatus.NoAccount, "No user with this email address exists. Ask an administrator to create one.");
            if (candidates.Count > 1)
                return AuthResult.Fail(AuthStatus.Ambiguous, "Several users share this email address. Sign in with your password.");

            var user = candidates[0];
            if (user.IsLockedOut(DateTime.UtcNow)) return AuthResult.Fail(AuthStatus.Locked);
            if (user.Disabled) return AuthResult.AccountDisabled();
            var payload = $"{identity.Provider}|{identity.Subject}|{Truncate(identity.Email, 200)}";
            return await StartChallengeAsync(user, ChallengePurpose.LinkExternal, payload);
        }

        // ---------------------------------------------------------------- helpers

        private async Task<AuthResult> StartChallengeAsync(User user, string purpose, string payload)
        {
            var now = DateTime.UtcNow;
            var code = NewCode();
            var challenge = new AuthChallenge
            {
                Id = Guid.NewGuid(),
                Purpose = purpose,
                TenantName = user.Id.TenantName,
                EmployeeId = user.Id.EmployeeId,
                Payload = payload,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(options.EmailCode.CodeLifetimeMinutes),
                Sends = 1,
            };
            challenge.CodeHash = HashCode(challenge.Id, code);
            await challenges.DeleteExpiredAsync(now);
            await challenges.AddAsync(challenge);

            if (!await SendCodeAsync(user, purpose, code))
                return AuthResult.Fail(AuthStatus.EmailFailed, "The code could not be sent by email. Contact an administrator.");

            return new AuthResult(AuthStatus.CodeRequired, ChallengeId: challenge.Id, EmailHint: MaskEmail(user.Email));
        }

        private async Task<(AuthChallenge, AuthStatus)> CheckCodeAsync(Guid challengeId, string code, params string[] purposes)
        {
            var challenge = await challenges.GetAsync(challengeId);
            var now = DateTime.UtcNow;
            if (challenge == null || !purposes.Contains(challenge.Purpose) || challenge.ConsumedAt != null ||
                challenge.ExpiresAt <= now || challenge.Attempts >= options.EmailCode.MaxAttempts)
            {
                return (null, AuthStatus.InvalidCode);
            }

            var normalized = new string((code ?? string.Empty).Where(char.IsDigit).ToArray());
            var expected = Convert.FromHexString(challenge.CodeHash);
            var actual = Convert.FromHexString(HashCode(challenge.Id, normalized));
            if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            {
                challenge.Attempts++;
                await challenges.UpdateAsync(challenge);
                // wrong codes count towards the account lockout as well
                var owner = users.GetBy(challenge.User);
                if (owner != null)
                {
                    owner.LoginFailed(now);
                    users.Update(owner);
                }
                return (null, AuthStatus.InvalidCode);
            }

            challenge.ConsumedAt = now;
            await challenges.UpdateAsync(challenge);
            return (challenge, AuthStatus.Success);
        }

        private async Task<bool> SendCodeAsync(User user, string purpose, string code)
        {
            string company = null;
            try { company = (await tenantConfig.GetRequisitesAsync())?.Name; }
            catch (Exception ex) { logger.LogDebug(ex, "Company name not available"); }

            var (subject, action) = purpose switch
            {
                ChallengePurpose.PasswordReset => ("Password reset code", "reset your password"),
                ChallengePurpose.LinkExternal => ("Confirm your Microsoft sign in", "link your Microsoft account and sign in"),
                _ => ("Your sign in code", "sign in"),
            };
            var minutes = options.EmailCode.CodeLifetimeMinutes;
            var text =
                $"Hello {user.UserName},\n\n" +
                $"Use this code to {action}: {code}\n\n" +
                $"The code expires in {minutes} minutes. If you did not request it, ignore this email and consider changing your password.\n";

            try
            {
                await email.SendAsync(new EmailMessage(user.Email, $"{subject}: {code}", text) { FromName = company });
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not send {purpose} code to user {user}", purpose, user.UserName);
                return false;
            }
        }

        private static string NewCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        private string HashCode(Guid challengeId, string code)
        {
            using var hmac = new HMACSHA256(codeKey);
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(challengeId.ToString("N") + ":" + code)));
        }

        public static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var at = email.IndexOf('@');
            if (at <= 0) return "***";
            var name = email[..at];
            var visible = name.Length <= 2 ? name[..1] : name[..2];
            return $"{visible}***{email[at..]}";
        }

        private static string Truncate(string value, int max) => value == null ? null : (value.Length <= max ? value : value[..max]);
    }
}
