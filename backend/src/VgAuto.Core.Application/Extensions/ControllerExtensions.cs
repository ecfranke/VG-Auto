using VgAuto.Core.Application;
using VgAuto.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;

namespace VgAuto.Core.Application.Extensions
{
    public static class ControllerExtensions
    {
        public static Guid? EmployeeId(this ControllerBase controller)
        {
            var empString = controller.HttpContext.User.Claims.First(x => x.Type == ClaimTypes.UserData)?.Value;
            if (string.IsNullOrWhiteSpace(empString)) return null;
            return Guid.Parse(empString);

        }
        public static string TenantName(this ControllerBase controller)
        {
            return controller.HttpContext.User.Claims.First(x => x.Type == ClaimTypes.Spn)?.Value;
        }
        /// <summary>Company whose data the request works with.</summary>
        public static Guid CompanyId(this ControllerBase controller) =>
            Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                .GetRequiredService<VgAuto.Core.Application.Database.ICompanyScope>(controller.HttpContext.RequestServices).CompanyId;

        public static string UserName(this ControllerBase controller)
        {
            return controller.HttpContext.User.Identity.Name;
        }
    }
}
