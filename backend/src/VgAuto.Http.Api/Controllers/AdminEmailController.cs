using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NHibernate;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Email;
using VgAuto.Core.Application.Extensions;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Domain;

namespace VgAuto.Http.Api.Controllers
{
    /// <summary>
    /// Email transports in the administration. Super administrators set the built-in transport of the system and allow
    /// companies to use it; company administrators connect their company's own SMTP, Microsoft 365 or Gmail account
    /// or choose the built-in one when it is allowed. Passwords and client secrets are never returned.
    /// </summary>
    [TenantRateLimit]
    [Authorize(Policy = "ServerSidePolicy")]
    [Route("api/admin")]
    [ApiController]
    public class AdminEmailController : ControllerBase
    {
        private readonly IEmailSettingsRepository settings;
        private readonly SystemEmailSender systemSender;
        private readonly IEmailSender builtIn;
        private readonly ICompanyEmailSender companySender;
        private readonly ISession session;
        private readonly ICompanyScope companies;
        private readonly IAdminAuditLog audit;
        private readonly ITenantConfigService tenantConfig;
        private readonly DbOptions dbOptions;

        public AdminEmailController(IEmailSettingsRepository settings, SystemEmailSender systemSender, IEmailSender builtIn,
            ICompanyEmailSender companySender, ISession session, ICompanyScope companies, IAdminAuditLog audit,
            ITenantConfigService tenantConfig, IOptions<DbOptions> dbOptions)
        {
            this.settings = settings;
            this.systemSender = systemSender;
            this.builtIn = builtIn;
            this.companySender = companySender;
            this.session = session;
            this.companies = companies;
            this.audit = audit;
            this.tenantConfig = tenantConfig;
            this.dbOptions = dbOptions.Value;
        }

        // ---------------------------------------------------------------- models

        /// <summary>Kind: system (built-in, companies) | config (server configuration, system) | smtp | graph | gmail.</summary>
        public record EmailSettingsDto(string Kind, string FromAddress, string FromName,
            string SmtpHost, int? SmtpPort, string SmtpUser, bool HasSmtpPassword, string SmtpSecurity,
            string GraphTenantId, string GraphClientId, bool HasGraphClientSecret, string GraphSender,
            DateTime? UpdatedAt, bool UnreadableSecret, string Description);

        public record CompanyEmailRow(Guid Id, string Name, string Kind, bool SystemAllowed, string Description);

        /// <summary>Editable is false when the server runs several tenants: the built-in email then comes from the server configuration.</summary>
        public record SystemEmailDto(EmailSettingsDto Settings, string InUse, string ServerConfiguration, bool Editable, IReadOnlyList<CompanyEmailRow> Companies);

        /// <summary>SystemAllowed: a super administrator allowed the built-in email for this company; CanAllowSystem: the signed in account may change it.</summary>
        public record CompanyEmailDto(Guid CompanyId, string CompanyName, EmailSettingsDto Settings, bool SystemAllowed, bool CanAllowSystem, string InUse);

        /// <summary>Empty passwords and secrets keep the saved ones (for the same account).</summary>
        public record EmailSettingsInput(string Kind, string FromAddress, string FromName,
            string SmtpHost, int? SmtpPort, string SmtpUser, string SmtpPassword, string SmtpSecurity,
            string GraphTenantId, string GraphClientId, string GraphClientSecret, string GraphSender);

        public record AllowInput(bool Allowed);

        public record TestInput(string To);

        // ---------------------------------------------------------------- built-in email (super administrators)

        [RequireSuperAdmin]
        [HttpGet("email/system")]
        public async Task<ActionResult<SystemEmailDto>> GetSystem()
        {
            var saved = await settings.GetSystemAsync() ?? new EmailTransportSettings { Kind = EmailTransportKind.Config };
            var (_, inUse) = await systemSender.ResolveAsync();
            var names = CompanyNames();
            var perCompany = await settings.GetCompaniesAsync(this.TenantName());
            var rows = names.OrderBy(n => n.Value, StringComparer.CurrentCultureIgnoreCase).Select(n =>
            {
                var company = perCompany.TryGetValue(n.Key, out var s) ? s : new EmailTransportSettings();
                return new CompanyEmailRow(n.Key, n.Value, company.Kind, company.SystemAllowed, Describe(company));
            }).ToList();
            return new SystemEmailDto(ToDto(saved), inUse, systemSender.DescribeConfigured(), !MultiTenancy, rows);
        }

        [RequireSuperAdmin]
        [HttpPut("email/system")]
        public async Task<IActionResult> SaveSystem([FromBody] EmailSettingsInput input)
        {
            if (MultiTenancy) throw new UserException("The built-in email is set in the server configuration when several tenants share the server.");
            var current = await settings.GetSystemAsync() ?? new EmailTransportSettings { Kind = EmailTransportKind.Config };
            var next = Apply(current, input, company: false);
            if (next.IsOwnTransport) next.Validate();
            await settings.SaveSystemAsync(next);
            await Log("email.system", null, next.IsOwnTransport ? next.Describe() : "server configuration", null);
            return Ok();
        }

        [RequireSuperAdmin]
        [HttpPost("email/system/test")]
        public async Task<IActionResult> TestSystem([FromBody] TestInput input)
        {
            var to = Required(input?.To, "Recipient");
            var (_, inUse) = await systemSender.ResolveAsync();
            await Send(() => builtIn.SendAsync(new EmailMessage(to, "Test email", $"This is a test of the built-in email of VG Auto ({inUse}).")));
            await Log("email.test", to, "built-in email: " + inUse, null);
            return Ok(new { transport = inUse });
        }

        // ---------------------------------------------------------------- company email (company and super administrators)

        [RequireAdmin]
        [HttpGet("companies/{companyId:guid}/email")]
        public async Task<ActionResult<CompanyEmailDto>> GetCompany(Guid companyId)
        {
            var name = CompanyName(companyId);
            if (name == null) return NotFound();
            var company = await settings.GetCompanyAsync(this.TenantName(), companyId);
            return new CompanyEmailDto(companyId, name, ToDto(company), company.SystemAllowed, IsSuper, await InUse(company));
        }

        [RequireAdmin]
        [HttpPut("companies/{companyId:guid}/email")]
        public async Task<IActionResult> SaveCompany(Guid companyId, [FromBody] EmailSettingsInput input)
        {
            var name = CompanyName(companyId);
            if (name == null) return NotFound();
            var current = await settings.GetCompanyAsync(this.TenantName(), companyId);
            var next = Apply(current, input, company: true);
            if (next.IsOwnTransport) next.Validate();
            else if (!next.SystemAllowed) throw new UserException("The built-in email is not enabled for this company. Ask a super administrator, or connect the company's own email account.");
            await settings.SaveCompanyAsync(this.TenantName(), companyId, next);
            await Log("email.company", name, Describe(next), companyId);
            return Ok();
        }

        /// <summary>Allows or stops a company to use the built-in email.</summary>
        [RequireSuperAdmin]
        [HttpPut("companies/{companyId:guid}/email/system")]
        public async Task<IActionResult> AllowSystem(Guid companyId, [FromBody] AllowInput input)
        {
            var name = CompanyName(companyId);
            if (name == null) return NotFound();
            var company = await settings.GetCompanyAsync(this.TenantName(), companyId);
            if (company.SystemAllowed == input.Allowed) return Ok();
            company.SystemAllowed = input.Allowed;
            await settings.SaveCompanyAsync(this.TenantName(), companyId, company);
            await Log("email.allow", name, input.Allowed ? "built-in email allowed" : "built-in email no longer allowed", companyId);
            return Ok();
        }

        [RequireAdmin]
        [HttpPost("companies/{companyId:guid}/email/test")]
        public async Task<IActionResult> TestCompany(Guid companyId, [FromBody] TestInput input)
        {
            var name = CompanyName(companyId);
            if (name == null) return NotFound();
            var to = Required(input?.To, "Recipient");
            companies.SwitchTo(companyId);
            var requisites = await tenantConfig.GetRequisitesAsync();
            string transport = null;
            await Send(async () => transport = await companySender.SendAsync(
                new EmailMessage(to, "Test email", $"This is a test email of {requisites.Name} sent by VG Auto.")
                {
                    FromName = requisites.Name,
                    ReplyTo = requisites.Email,
                    FallbackFromAddress = requisites.Email,
                }, companyId));
            await Log("email.test", to, $"{name}: {transport}", companyId);
            return Ok(new { transport });
        }

        // ---------------------------------------------------------------- helpers

        private bool IsSuper => this.CurrentAccount()?.Role == UserRoles.SuperAdmin;

        private bool MultiTenancy => dbOptions.MultiTenancy?.Enabled == true;

        private async Task<string> InUse(EmailTransportSettings company)
        {
            if (company.IsOwnTransport) return company.Describe();
            if (!company.SystemAllowed) return null;
            var (_, description) = await systemSender.ResolveAsync();
            return "built-in email: " + description;
        }

        private static string Describe(EmailTransportSettings company) =>
            company.IsOwnTransport ? company.Describe() : company.SystemAllowed ? "built-in email" : "not set up";

        /// <summary>Name of a company the signed in administrator may manage; null otherwise.</summary>
        private string CompanyName(Guid companyId)
        {
            var me = this.CurrentAccount();
            if (me == null || (!IsSuper && me.CompanyId != companyId)) return null;
            return CompanyNames().TryGetValue(companyId, out var name) ? name : null;
        }

        private Dictionary<Guid, string> CompanyNames() =>
            session.CreateSQLQuery(SqlDialect.Current.Sql(
                "select c.id, coalesce(r.name, c.name) as name from domain.company c left join tenant_config.requisites r on r.company_id = c.id"))
                .List<object[]>()
                .ToDictionary(x => x[0] is Guid g ? g : Guid.Parse(x[0].ToString()!), x => x[1]?.ToString());

        private static EmailTransportSettings Apply(EmailTransportSettings current, EmailSettingsInput input, bool company)
        {
            if (input == null) throw new UserException("Incomplete settings.");
            var kind = EmailTransportKind.Normalize(input.Kind);
            if (company && kind == EmailTransportKind.Config) kind = EmailTransportKind.System;
            if (!company && kind == EmailTransportKind.System) kind = EmailTransportKind.Config;

            var next = new EmailTransportSettings
            {
                Kind = kind,
                SystemAllowed = current.SystemAllowed, // changed by super administrators only, see AllowSystem
                FromAddress = input.FromAddress?.Trim(),
                FromName = input.FromName?.Trim(),
                SmtpHost = input.SmtpHost?.Trim(),
                SmtpPort = input.SmtpPort,
                SmtpUser = input.SmtpUser?.Trim(),
                SmtpSecurity = Enum.TryParse<SmtpSecurity>(input.SmtpSecurity, true, out var security) ? security : SmtpSecurity.Auto,
                GraphTenantId = input.GraphTenantId?.Trim(),
                GraphClientId = input.GraphClientId?.Trim(),
                GraphSender = input.GraphSender?.Trim(),
            };
            if (kind == EmailTransportKind.Gmail) next.SmtpUser = next.FromAddress;

            // an empty password keeps the saved one, but only for the same account
            var sameSmtpAccount = current.Kind == kind && Same(current.SmtpHost, next.SmtpHost) && Same(current.SmtpUser, next.SmtpUser);
            var sameGraphApp = current.Kind == kind && Same(current.GraphTenantId, next.GraphTenantId) && Same(current.GraphClientId, next.GraphClientId);
            var usesSmtp = kind is EmailTransportKind.Smtp or EmailTransportKind.Gmail;
            next.SmtpPassword = !string.IsNullOrEmpty(input.SmtpPassword) ? input.SmtpPassword
                : !usesSmtp || sameSmtpAccount ? current.SmtpPassword : null;
            next.GraphClientSecret = !string.IsNullOrEmpty(input.GraphClientSecret) ? input.GraphClientSecret
                : kind != EmailTransportKind.Graph || sameGraphApp ? current.GraphClientSecret : null;
            next.UnreadableSecret = current.UnreadableSecret &&
                (usesSmtp ? string.IsNullOrEmpty(input.SmtpPassword) : kind == EmailTransportKind.Graph && string.IsNullOrEmpty(input.GraphClientSecret));
            return next;
        }

        private static bool Same(string a, string b) => string.Equals(a?.Trim() ?? "", b?.Trim() ?? "", StringComparison.OrdinalIgnoreCase);

        private static EmailSettingsDto ToDto(EmailTransportSettings s) => new(
            s.Kind, s.FromAddress, s.FromName, s.SmtpHost, s.SmtpPort, s.SmtpUser, !string.IsNullOrEmpty(s.SmtpPassword), s.SmtpSecurity.ToString(),
            s.GraphTenantId, s.GraphClientId, !string.IsNullOrEmpty(s.GraphClientSecret), s.GraphSender, s.UpdatedAt, s.UnreadableSecret,
            s.IsOwnTransport ? s.Describe() : null);

        private static async Task Send(Func<Task> send)
        {
            try
            {
                await send();
            }
            catch (EmailDeliveryException ex)
            {
                throw new UserException(ex.Message);
            }
        }

        private static string Required(string value, string field) =>
            string.IsNullOrWhiteSpace(value) ? throw new UserException($"{field} is required.") : value.Trim();

        private Task Log(string action, string target, string details, Guid? companyId) =>
            audit.WriteAsync(this.TenantName(), this.CurrentAccount()?.UserName ?? this.UserName(), action, target, details, companyId);
    }
}
