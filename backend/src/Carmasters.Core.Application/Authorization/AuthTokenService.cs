using Carmasters.Core.Application.Configuration;
using Carmasters.Core.Application.Database;
using Microsoft.Extensions.Options;

namespace Carmasters.Core.Application.Authorization
{
    public record AuthTokens(string Jwt, string PublicJwt, int Timeout, bool MustChangePassword);

    /// <summary>Issues the token pair used by the frontend: a server side (Root) token and a browser token.</summary>
    public class AuthTokenService
    {
        private readonly IUserRepository users;
        private readonly JwtOptions jwtOptions;

        public AuthTokenService(IUserRepository users, IOptions<JwtOptions> jwtOptions)
        {
            this.users = users;
            this.jwtOptions = jwtOptions.Value;
        }

        public AuthTokens Issue(User user, string authMethod)
        {
            var fullName = users.GetFullName(user.UserName);
            var internalUsePrincipal = ClaimsPrincipalBuilder.Build(user, fullName, false, authMethod);
            var publicUsePrincipal = ClaimsPrincipalBuilder.Build(user, fullName, true, authMethod);

            return new AuthTokens(
                AppJwtToken.Generate(jwtOptions, internalUsePrincipal),
                AppJwtToken.Generate(jwtOptions, publicUsePrincipal),
                (int)jwtOptions.SessionTimeout.TotalSeconds,
                user.MustChangePassword);
        }
    }

    public static class PasswordPolicy
    {
        public const int MinimumLength = 10;

        /// <summary>Returns an error message, or null when the password is acceptable.</summary>
        public static string Validate(string password, string userName = null)
        {
            if (string.IsNullOrWhiteSpace(password)) return "Password cannot be empty.";
            if (password.Length < MinimumLength) return $"Password must be at least {MinimumLength} characters long.";
            if (password.Length > 128) return "Password is too long.";
            if (!string.IsNullOrEmpty(userName) && password.Contains(userName, System.StringComparison.OrdinalIgnoreCase))
                return "Password must not contain the username.";
            if (password.Equals("carcare", System.StringComparison.OrdinalIgnoreCase) || password.Distinct() < 4)
                return "Password is too simple.";
            return null;
        }

        private static int Distinct(this string s) => new System.Collections.Generic.HashSet<char>(s).Count;
    }
}
