using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Database;

namespace VgAuto.Core.Application.Email
{
    /// <summary>Email transports saved in the administration.</summary>
    public interface IEmailSettingsRepository
    {
        /// <summary>The built-in transport set by a super administrator; null when none is saved (the server configuration applies).</summary>
        Task<EmailTransportSettings> GetSystemAsync();
        Task SaveSystemAsync(EmailTransportSettings settings);

        /// <summary>The transport of a company; the built-in one, not allowed, when nothing is saved.</summary>
        Task<EmailTransportSettings> GetCompanyAsync(string tenantName, Guid companyId);
        Task<IReadOnlyDictionary<Guid, EmailTransportSettings>> GetCompaniesAsync(string tenantName);
        Task SaveCompanyAsync(string tenantName, Guid companyId, EmailTransportSettings settings);
    }

    /// <summary>Senders for a given set of options: SMTP (also Gmail) or Microsoft Graph.</summary>
    public class EmailSenderFactory
    {
        private readonly IServiceProvider services;
        public EmailSenderFactory(IServiceProvider services) { this.services = services; }

        public IEmailSender Create(EmailOptions options) => options.Provider == EmailProvider.Graph
            ? new GraphEmailSender(Options.Create(options), Get<System.Net.Http.IHttpClientFactory>(), Get<GraphTokenCache>(), Get<ILogger<GraphEmailSender>>())
            : new SmtpEmailSender(Options.Create(options), Get<ILogger<SmtpEmailSender>>());

        private T Get<T>() => (T)services.GetService(typeof(T));
    }

    /// <summary>
    /// The built-in transport: sign in codes, password resets and the documents of companies allowed to use it.
    /// Uses the transport a super administrator saved, otherwise the Email section of the server configuration.
    /// </summary>
    public class SystemEmailSender : IEmailSender
    {
        private readonly IEmailSettingsRepository repository;
        private readonly EmailOptions configured;
        private readonly EmailSenderFactory factory;
        private readonly ILogger<SystemEmailSender> logger;

        public SystemEmailSender(IEmailSettingsRepository repository, IOptions<EmailOptions> configured, EmailSenderFactory factory, ILogger<SystemEmailSender> logger)
        {
            this.repository = repository;
            this.configured = configured.Value;
            this.factory = factory;
            this.logger = logger;
        }

        public string Name => "Built-in";

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            var (options, _) = await ResolveAsync();
            await factory.Create(options).SendAsync(message, cancellationToken);
        }

        /// <summary>The options used now and a description ("SMTP (smtp.example.com)", "server configuration: ...").</summary>
        public async Task<(EmailOptions Options, string Description)> ResolveAsync()
        {
            try
            {
                var saved = await repository.GetSystemAsync();
                if (saved is { IsOwnTransport: true })
                {
                    if (saved.UnreadableSecret) throw new EmailDeliveryException("The password of the built-in email can no longer be read. A super administrator has to enter it again.");
                    return (saved.ToOptions(), saved.Describe());
                }
            }
            catch (Exception ex) when (ex is not EmailDeliveryException)
            {
                logger.LogWarning(ex, "Email settings of the system could not be read, using the server configuration");
            }
            return (configured, "server configuration: " + DescribeConfigured());
        }

        /// <summary>The Email section of the server configuration, without secrets.</summary>
        public string DescribeConfigured()
        {
            if (configured.Provider == EmailProvider.Graph)
                return string.IsNullOrWhiteSpace(configured.Graph.Sender) ? "not configured" : $"Microsoft 365 ({configured.Graph.Sender})";
            return string.IsNullOrWhiteSpace(configured.Smtp.Host) ? "not configured" : $"SMTP ({configured.Smtp.Host})";
        }

        public bool ConfiguredInServer => configured.Provider == EmailProvider.Graph
            ? !string.IsNullOrWhiteSpace(configured.Graph.Sender)
            : !string.IsNullOrWhiteSpace(configured.Smtp.Host);
    }

    /// <summary>Sends the estimates and invoices of a company through its own transport or, when allowed, the built-in one.</summary>
    public interface ICompanyEmailSender
    {
        /// <param name="companyId">the company; the company of the request when null</param>
        /// <returns>the transport used, e.g. "SMTP (smtp.example.com)"</returns>
        Task<string> SendAsync(EmailMessage message, Guid? companyId = null, CancellationToken cancellationToken = default);
    }

    public class CompanyEmailSender : ICompanyEmailSender
    {
        public const string NotSetUp = "Email sending is not set up for this company. An administrator can connect the company's own SMTP, Microsoft 365 or Gmail account under Administration → Company, or a super administrator can allow the built-in email.";

        private readonly IEmailSettingsRepository repository;
        private readonly IEmailSender system;
        private readonly EmailSenderFactory factory;
        private readonly ICompanyScope scope;
        private readonly IHttpContextAccessor http;

        public CompanyEmailSender(IEmailSettingsRepository repository, IEmailSender system, EmailSenderFactory factory, ICompanyScope scope, IHttpContextAccessor http)
        {
            this.repository = repository;
            this.system = system;
            this.factory = factory;
            this.scope = scope;
            this.http = http;
        }

        public async Task<string> SendAsync(EmailMessage message, Guid? companyId = null, CancellationToken cancellationToken = default)
        {
            var settings = await repository.GetCompanyAsync(http.HttpContext?.CurrentAccount()?.Id.TenantName, companyId ?? scope.CompanyId);
            if (!settings.IsOwnTransport)
            {
                if (!settings.SystemAllowed) throw new EmailDeliveryException(NotSetUp);
                await system.SendAsync(message, cancellationToken);
                return "built-in email";
            }
            if (settings.UnreadableSecret) throw new EmailDeliveryException("The saved email password can no longer be read. An administrator has to enter it again under Administration → Company.");
            await factory.Create(settings.ToOptions()).SendAsync(message, cancellationToken);
            return settings.Describe();
        }
    }
}
