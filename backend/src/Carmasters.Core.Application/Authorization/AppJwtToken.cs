using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Carmasters.Core.Application.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Carmasters.Core.Application.Authorization
{
    public class AppJwtToken
    {
        public const int MinimumSecretBytes = 32;

        public static TokenValidationParameters ValidationParameters(string secret)
        {
            EnsureJwtSecret(secret);
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                // tokens expire exactly at token expiration time (instead of 5 minutes later)
                ClockSkew = TimeSpan.Zero
            };
        }

        public static JwtSecurityToken LoadJwt(JwtOptions options, string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, ValidationParameters(options.Secret), out SecurityToken validatedToken);
            return (JwtSecurityToken)validatedToken;
        }

        public static string Generate(JwtOptions options, ClaimsPrincipal principal)
        {
            EnsureJwtSecret(options.Secret);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(options.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = (ClaimsIdentity)principal.Identity,
                IssuedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.Add(options.SessionTimeout),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>Constant time comparison for shared secrets.</summary>
        public static bool SecretsEqual(string expected, string provided)
        {
            if (string.IsNullOrEmpty(expected) || provided == null) return false;
            var a = Encoding.UTF8.GetBytes(expected);
            var b = Encoding.UTF8.GetBytes(provided);
            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        public static void EnsureJwtSecret(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentException("JwtOptions:Secret is not configured.");
            if (Encoding.UTF8.GetByteCount(secret) < MinimumSecretBytes)
                throw new ArgumentException($"JwtOptions:Secret must be at least {MinimumSecretBytes} bytes long.");
        }
    }
}
