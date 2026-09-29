using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace VgAuto.Core.Application.Email
{
    public static class EmailServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IEmailSender"/> for the provider chosen in Email:Provider.
        /// When Email:Smtp:Host is empty the legacy "SmtpOptions" section is used.
        /// </summary>
        public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<EmailOptions>().Configure(options =>
            {
                configuration.GetSection("Email").Bind(options);

                // legacy configuration: SmtpOptions { Host, Port, User, Password }
                var legacy = configuration.GetSection("SmtpOptions");
                if (options.Provider == EmailProvider.Smtp && string.IsNullOrWhiteSpace(options.Smtp.Host) && !string.IsNullOrWhiteSpace(legacy["Host"]))
                {
                    options.Smtp.Host = legacy["Host"];
                    options.Smtp.Port = int.TryParse(legacy["Port"], out var port) ? port : 25;
                    options.Smtp.User = legacy["User"];
                    options.Smtp.Password = legacy["Password"];
                }
            });

            services.AddHttpClient(GraphEmailSender.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(60));
            services.AddSingleton<GraphTokenCache>();
            services.AddSingleton<SecretProtector>();
            services.AddSingleton<EmailSenderFactory>();
            // built-in transport: set by a super administrator in the administration, otherwise the Email section above
            services.AddSingleton<SystemEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<SystemEmailSender>());
            // estimates and invoices: the company's own transport or, when allowed, the built-in one
            services.AddScoped<ICompanyEmailSender, CompanyEmailSender>();
            return services;
        }
    }
}
