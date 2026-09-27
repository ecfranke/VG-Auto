using Carmasters.Core.Application.Authorization;
using Carmasters.Core.Application.Database;
using Carmasters.Core.Application.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Carmasters.Core.Application.Extensions.DependencyInjection
{
    public static class ControllersExtensions
    {
        public const string ServerSidePolicy = "ServerSidePolicy";

        public static IServiceCollection AddControllersWithViewsToApp(this IServiceCollection services)
        {
            services.AddControllersWithViews(options =>
            {
                options.Filters.Add<JsonResponseExceptionFilter>();
                options.Filters.Add(typeof(UnitOfWorkAspect));
            });

            services.AddAuthorization(options =>
            {
                // Tokens issued to the Next.js server carry the Root role, browser tokens do not.
                options.AddPolicy(ServerSidePolicy, policy => policy.RequireAuthenticatedUser().RequireRole(AppClaims.RootRole));
                // Every endpoint requires a valid token unless it is explicitly marked [AllowAnonymous].
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            });

            services.Configure<ApiBehaviorOptions>(o =>
            {
                o.InvalidModelStateResponseFactory = actionContext => InvalidModelStateJsonResponseFactory.Handle(actionContext);
            });

            return services;
        }
    }
}
