using VgAuto.Core.Application.Services;
using VgAuto.Core.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace VgAuto.Core.Application.Extensions.DependencyInjection
{
    public static class TenantConfigurationExtensions
    {
        public static IServiceCollection AddTenantConfigurationServices(this IServiceCollection services)
        {
            // Register the repository and service
            services.AddScoped<ITenantConfigRepository, TenantConfigRepository>();
            services.AddScoped<ITenantConfigService, TenantConfigService>();

            return services;
        }
    }
}
