using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Carmasters.Core.Application.Authentication
{
    public static class AuthenticationServiceCollectionExtensions
    {
        public static IServiceCollection AddLoginFlows(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AuthenticationOptions>(configuration.GetSection("Authentication"));
            services.AddHttpClient(MicrosoftIdentityClient.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(30));
            services.AddSingleton<MicrosoftMetadata>();
            services.AddScoped<IMicrosoftIdentityClient, MicrosoftIdentityClient>();
            services.AddScoped<AuthService>();
            return services;
        }
    }
}
