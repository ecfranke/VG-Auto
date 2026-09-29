using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Model;

namespace VgAuto.Core.Application.Authorization
{
    /// <summary>
    /// Loads the account behind an authenticated request. Requests of disabled or deleted accounts are
    /// rejected even while their token is still valid. The account is available to the rest of the
    /// request through <see cref="CurrentAccountExtensions.CurrentAccount(HttpContext)"/>.
    /// </summary>
    public class AccountStatusMiddleware
    {
        public const string ItemKey = "vg.account";
        private readonly RequestDelegate next;

        public AccountStatusMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var principal = context.User;
            if (principal?.Identity?.IsAuthenticated == true)
            {
                var tenant = principal.FindFirst(ClaimTypes.Spn)?.Value;
                var employee = principal.FindFirst(ClaimTypes.UserData)?.Value;
                User account = null;
                if (!string.IsNullOrEmpty(tenant) && Guid.TryParse(employee, out var employeeId))
                {
                    var users = context.RequestServices.GetRequiredService<IUserRepository>();
                    account = users.GetBy(new UserIdentifier(tenant, employeeId));
                }
                if (account == null || account.Disabled)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"isUserError\":true,\"exceptionMessage\":\"This account has been disabled.\",\"accountDisabled\":true}");
                    return;
                }
                context.Items[ItemKey] = account;
            }
            await next(context);
        }
    }

    public static class CurrentAccountExtensions
    {
        /// <summary>The account of the current request (null for anonymous requests).</summary>
        public static User CurrentAccount(this HttpContext context) =>
            context.Items.TryGetValue(AccountStatusMiddleware.ItemKey, out var value) ? value as User : null;

        public static User CurrentAccount(this ControllerBase controller) => controller.HttpContext.CurrentAccount();
    }

    /// <summary>Only administrators (admin or superadmin) may call the action.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireAdminAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var account = context.HttpContext.CurrentAccount();
            if (account == null || !account.IsAdmin)
            {
                context.Result = new ObjectResult(new { isUserError = true, exceptionMessage = "Administrator rights are required." })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }

    /// <summary>Only super administrators may call the action (all companies, the built-in email).</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireSuperAdminAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var account = context.HttpContext.CurrentAccount();
            if (account == null || account.Role != UserRoles.SuperAdmin)
            {
                context.Result = new ObjectResult(new { isUserError = true, exceptionMessage = "Super administrator rights are required." })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
