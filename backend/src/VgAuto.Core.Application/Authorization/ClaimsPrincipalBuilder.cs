using System.Collections.Generic;
using System.Security.Claims;
using NHibernate.Mapping;

namespace VgAuto.Core.Application.Authorization
{
    public class ClaimsPrincipalBuilder 
    {
        private static ClaimsPrincipal Build(string name, string fullName, string tenantName, string employeeId, bool publicUse, bool passwordChangeRequired, string authMethod, string role)
        {
            var claims = new List<Claim> {
            new Claim(ClaimTypes.Name, name),
            new Claim(AppClaims.FullName, fullName ?? name),
            new Claim(ClaimTypes.Spn, tenantName),
            new Claim(ClaimTypes.UserData, employeeId),
        };

            if (!publicUse)
            {
                claims.Add(new Claim(ClaimTypes.Role, AppClaims.RootRole));
            }
            if (passwordChangeRequired)
            {
                claims.Add(new Claim(AppClaims.PasswordChangeRequired, "true"));
            }
            claims.Add(new Claim(AppClaims.AccountRole, UserRoles.Normalize(role)));
            if (!string.IsNullOrWhiteSpace(authMethod))
            {
                claims.Add(new Claim(AppClaims.AuthMethod, authMethod));
            }

            var identity = new ClaimsIdentity(claims, "Basic");

            var principal = new ClaimsPrincipal(identity);

            return principal;
        }

        public static ClaimsPrincipal Build(User user, string fullName, bool publicUse, string authMethod = "pwd") =>
            Build(user.UserName, fullName, user.Id.TenantName, user.Id.EmployeeId.ToString(), publicUse,
                  user.MustChangePassword, authMethod, user.Role);
    }
} 