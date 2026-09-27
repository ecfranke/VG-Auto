using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace VgAuto.Core.Application.Authorization
{
    /// <summary>
    /// Users that must replace their password (e.g. the initial admin account) can only reach
    /// the endpoints needed to do that.
    /// </summary>
    public class PasswordChangeRequiredMiddleware
    {
        private static readonly string[] AllowedPaths =
        {
            "/api/profile/changepassword",
            "/api/profile",
            "/api/users/extendsession",
            "/api/users/profilepicture",
            "/health",
        };

        private readonly RequestDelegate next;

        public PasswordChangeRequiredMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated == true &&
                user.HasClaim(c => c.Type == AppClaims.PasswordChangeRequired && c.Value == "true"))
            {
                var path = context.Request.Path.Value ?? string.Empty;
                var allowed = AllowedPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                                                   path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
                if (!allowed)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"isUserError\":true,\"exceptionMessage\":\"Password change required.\",\"passwordChangeRequired\":true}");
                    return;
                }
            }
            await next(context);
        }
    }
}
