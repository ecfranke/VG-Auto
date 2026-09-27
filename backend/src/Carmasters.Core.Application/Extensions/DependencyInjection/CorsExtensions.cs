using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Carmasters.Core.Application.Extensions.DependencyInjection
{
    public static class CorsExtensions
    {
        public static IServiceCollection AddCorsToApp(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddCors(options =>
            {
                options.AddPolicy("DefaultPolicy", policy =>
                {
                    var corsMode = configuration.GetValue<string>("Cors:Mode") ?? "restricted";

                    if (corsMode.Equals("open", StringComparison.OrdinalIgnoreCase))
                    {
                        // development only, rejected by StartupValidation otherwise
                        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                        return;
                    }

                    // Exact origins, e.g. "https://app.example.com". Falls back to the legacy Cors:AppHost setting.
                    var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                    var appHost = configuration.GetValue<string>("Cors:AppHost");
                    if (origins.Length == 0 && string.IsNullOrWhiteSpace(appHost))
                        throw new InvalidOperationException("Cors:AllowedOrigins is not configured.");

                    var normalized = origins.Select(o => o.TrimEnd('/')).ToArray();
                    policy.SetIsOriginAllowed(origin =>
                        normalized.Contains(origin.TrimEnd('/'), StringComparer.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(appHost) && Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                         uri.Host.Equals(appHost, StringComparison.OrdinalIgnoreCase)));
                    policy.WithHeaders("Content-Type", "Authorization");
                    policy.WithMethods("GET", "PUT", "POST", "DELETE", "OPTIONS");
                });
            });
        }
    }
}
