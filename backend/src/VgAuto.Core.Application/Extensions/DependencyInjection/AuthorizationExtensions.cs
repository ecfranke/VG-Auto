using System;
using System.Threading.Tasks;
using VgAuto.Core.Application.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VgAuto.Core.Application.Extensions.DependencyInjection
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddJwtAuthenticationToApp(this IServiceCollection services, IConfiguration configuration)
        {
            var secret = configuration.GetSection("JwtOptions")["Secret"];

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = AppJwtToken.ValidationParameters(secret);
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var jwt = context.Request.Cookies["jwt_token"];
                        if (!string.IsNullOrWhiteSpace(jwt))
                        {
                            context.Token = jwt;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            return services;
        }
    }
}
