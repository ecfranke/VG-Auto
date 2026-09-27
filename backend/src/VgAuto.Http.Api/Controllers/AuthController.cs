using System;
using System.Threading.Tasks;
using VgAuto.Core.Application.Authentication;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace VgAuto.Http.Api.Controllers
{
    /// <summary>
    /// Login flows used by the Next.js server. Every call must carry the shared server secret,
    /// so browsers cannot call these endpoints directly.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly AuthService auth;
        private readonly JwtOptions jwtOptions;

        public AuthController(AuthService auth, IOptions<JwtOptions> jwtOptions)
        {
            this.auth = auth;
            this.jwtOptions = jwtOptions.Value;
        }

        public record PasswordLoginDto(string UserName, string Password, string ServerSecret);
        public record VerifyCodeDto(Guid ChallengeId, string Code, string ServerSecret);
        public record ChallengeDto(Guid ChallengeId, string ServerSecret);
        public record ForgotPasswordDto(string Login, string ServerSecret);
        public record ResetPasswordDto(Guid ChallengeId, string Code, string NewPassword, string ServerSecret);
        public record MicrosoftLoginDto(string Code, string CodeVerifier, string RedirectUri, string Nonce, string ServerSecret);

        [LimitRequests(MaxRequests = 10, TimeWindow = 60)]
        [HttpPost("login")]
        [HttpPost("/api/users/authenticate")] // legacy route
        public async Task<IActionResult> Login(PasswordLoginDto model)
        {
            if (!ServerSecretOk(model.ServerSecret)) return await Reject();
            return await ToResponse(await auth.PasswordLoginAsync(model.UserName, model.Password));
        }

        [LimitRequests(MaxRequests = 10, TimeWindow = 60)]
        [HttpPost("verify")]
        public async Task<IActionResult> Verify(VerifyCodeDto model)
        {
            if (!ServerSecretOk(model.ServerSecret)) return await Reject();
            return await ToResponse(await auth.VerifyCodeAsync(model.ChallengeId, model.Code));
        }

        [LimitRequests(MaxRequests = 5, TimeWindow = 60)]
        [HttpPost("resend")]
        public async Task<IActionResult> Resend(ChallengeDto model)
        {
            if (!ServerSecretOk(model.ServerSecret)) return await Reject();
            return await ToResponse(await auth.ResendCodeAsync(model.ChallengeId));
        }

        [LimitRequests(MaxRequests = 5, TimeWindow = 60)]
        [HttpPost("password/forgot")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (!ServerSecretOk(model.ServerSecret)) return await Reject();
            return await ToResponse(await auth.ForgotPasswordAsync(model.Login));
        }

        [LimitRequests(MaxRequests = 10, TimeWindow = 60)]
        [HttpPost("password/reset")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            if (!ServerSecretOk(model.ServerSecret)) return await Reject();
            return await ToResponse(await auth.ResetPasswordAsync(model.ChallengeId, model.Code, model.NewPassword));
        }

        /// <summary>Public login options for the login page.</summary>
        [HttpGet("providers")]
        public IActionResult Providers()
        {
            var o = auth.Options;
            return Ok(new
            {
                passwordReset = o.PasswordReset.Enabled,
                emailCode = o.EmailCode.RequireForPasswordLogin,
                microsoft = o.Microsoft.Enabled
                    ? new { enabled = true, clientId = o.Microsoft.ClientId, authorizeUrl = $"{o.Microsoft.Instance.TrimEnd('/')}/{o.Microsoft.TenantId}/oauth2/v2.0/authorize" }
                    : new { enabled = false, clientId = (string)null, authorizeUrl = (string)null },
            });
        }

        [LimitRequests(MaxRequests = 10, TimeWindow = 60)]
        [HttpPost("microsoft")]
        public async Task<IActionResult> Microsoft(MicrosoftLoginDto model)
        {
            if (!ServerSecretOk(model.ServerSecret)) return await Reject();
            return await ToResponse(await auth.MicrosoftLoginAsync(model.Code, model.CodeVerifier, model.RedirectUri, model.Nonce));
        }

        private bool ServerSecretOk(string secret) => AppJwtToken.SecretsEqual(jwtOptions.ConsumerSecret, secret);

        private async Task<IActionResult> Reject()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            return Unauthorized();
        }

        private async Task<IActionResult> ToResponse(AuthResult result)
        {
            switch (result.Status)
            {
                case AuthStatus.Success:
                    return result.Tokens != null ? Ok(result.Tokens) : Ok(new { status = "ok" });
                case AuthStatus.CodeRequired:
                    return Ok(new { codeRequired = true, challengeId = result.ChallengeId, emailHint = result.EmailHint });
                case AuthStatus.InvalidCredentials:
                case AuthStatus.InvalidCode:
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    return Unauthorized(Error(result));
                case AuthStatus.Locked:
                    return Unauthorized(Error(result));
                case AuthStatus.AccountDisabled:
                    return StatusCode(StatusCodes.Status403Forbidden, Error(result));
                case AuthStatus.TooManyRequests:
                    return StatusCode(StatusCodes.Status429TooManyRequests, Error(result));
                case AuthStatus.Disabled:
                    return NotFound(Error(result));
                case AuthStatus.EmailFailed:
                    return StatusCode(StatusCodes.Status502BadGateway, Error(result));
                default:
                    return BadRequest(Error(result));
            }
        }

        private static object Error(AuthResult result) => new
        {
            error = char.ToLowerInvariant(result.Status.ToString()[0]) + result.Status.ToString()[1..],
            message = result.Message,
            locked = result.Status == AuthStatus.Locked,
        };
    }
}
