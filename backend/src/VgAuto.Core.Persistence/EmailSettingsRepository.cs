using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Email;

namespace VgAuto.Core.Persistence
{
    /// <summary>
    /// Email transports saved in the administration: the built-in one (public.system_email, one row) and one per
    /// company (tenant_config.email). Passwords and client secrets are stored encrypted.
    /// </summary>
    public class EmailSettingsRepository : IEmailSettingsRepository
    {
        private const string Columns = @"provider as Provider, from_address as FromAddress, from_name as FromName,
            smtp_host as SmtpHost, smtp_port as SmtpPort, smtp_user as SmtpUser, smtp_password as SmtpPassword, smtp_security as SmtpSecurity,
            graph_tenant_id as GraphTenantId, graph_client_id as GraphClientId, graph_client_secret as GraphClientSecret,
            graph_sender as GraphSender, updated_at as UpdatedAt";

        private readonly IDbConnectionFactory connections;
        private readonly SecretProtector protector;

        public EmailSettingsRepository(IDbConnectionFactory connections, SecretProtector protector)
        {
            this.connections = connections;
            this.protector = protector;
        }

        private static string Sql(string sql) => SqlDialect.Current.Sql(sql);

        private class Row
        {
            public string CompanyId { get; set; }
            public string Provider { get; set; }
            public bool SystemAllowed { get; set; }
            public string FromAddress { get; set; }
            public string FromName { get; set; }
            public string SmtpHost { get; set; }
            public int? SmtpPort { get; set; }
            public string SmtpUser { get; set; }
            public string SmtpPassword { get; set; }
            public string SmtpSecurity { get; set; }
            public string GraphTenantId { get; set; }
            public string GraphClientId { get; set; }
            public string GraphClientSecret { get; set; }
            public string GraphSender { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public async Task<EmailTransportSettings> GetSystemAsync()
        {
            await using var db = connections.Open(connections.UserListDatabase);
            var row = await db.QuerySingleOrDefaultAsync<Row>(Sql($"SELECT {Columns} FROM public.system_email WHERE id = 1"));
            return row == null ? null : ToSettings(row);
        }

        public async Task SaveSystemAsync(EmailTransportSettings settings)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            var values = Values(settings);
            var updated = await db.ExecuteAsync(Sql(@"UPDATE public.system_email SET provider = @Provider, from_address = @FromAddress, from_name = @FromName,
                smtp_host = @SmtpHost, smtp_port = @SmtpPort, smtp_user = @SmtpUser, smtp_password = @SmtpPassword, smtp_security = @SmtpSecurity,
                graph_tenant_id = @GraphTenantId, graph_client_id = @GraphClientId, graph_client_secret = @GraphClientSecret,
                graph_sender = @GraphSender, updated_at = @UpdatedAt WHERE id = 1"), values);
            if (updated == 0)
            {
                await db.ExecuteAsync(Sql(@"INSERT INTO public.system_email (id, provider, from_address, from_name, smtp_host, smtp_port, smtp_user, smtp_password,
                    smtp_security, graph_tenant_id, graph_client_id, graph_client_secret, graph_sender, updated_at)
                    VALUES (1, @Provider, @FromAddress, @FromName, @SmtpHost, @SmtpPort, @SmtpUser, @SmtpPassword, @SmtpSecurity,
                    @GraphTenantId, @GraphClientId, @GraphClientSecret, @GraphSender, @UpdatedAt)"), values);
            }
        }

        public async Task<EmailTransportSettings> GetCompanyAsync(string tenantName, Guid companyId)
        {
            await using var db = OpenTenant(tenantName);
            var row = await db.QuerySingleOrDefaultAsync<Row>(Sql($"SELECT {Columns}, system_allowed as SystemAllowed FROM tenant_config.email WHERE company_id = @companyId"),
                new { companyId });
            return row == null ? new EmailTransportSettings() : ToSettings(row);
        }

        public async Task<IReadOnlyDictionary<Guid, EmailTransportSettings>> GetCompaniesAsync(string tenantName)
        {
            await using var db = OpenTenant(tenantName);
            var rows = await db.QueryAsync<Row>(Sql($"SELECT {SqlDialect.Current.CastToText("company_id")} as CompanyId, {Columns}, system_allowed as SystemAllowed FROM tenant_config.email"));
            return rows.ToDictionary(r => Guid.Parse(r.CompanyId), ToSettings);
        }

        public async Task SaveCompanyAsync(string tenantName, Guid companyId, EmailTransportSettings settings)
        {
            await using var db = OpenTenant(tenantName);
            var values = new DynamicParameters(Values(settings));
            values.Add("CompanyId", companyId);
            values.Add("SystemAllowed", settings.SystemAllowed);
            var updated = await db.ExecuteAsync(Sql(@"UPDATE tenant_config.email SET provider = @Provider, system_allowed = @SystemAllowed,
                from_address = @FromAddress, from_name = @FromName, smtp_host = @SmtpHost, smtp_port = @SmtpPort, smtp_user = @SmtpUser,
                smtp_password = @SmtpPassword, smtp_security = @SmtpSecurity, graph_tenant_id = @GraphTenantId, graph_client_id = @GraphClientId,
                graph_client_secret = @GraphClientSecret, graph_sender = @GraphSender, updated_at = @UpdatedAt WHERE company_id = @CompanyId"), values);
            if (updated == 0)
            {
                await db.ExecuteAsync(Sql(@"INSERT INTO tenant_config.email (company_id, provider, system_allowed, from_address, from_name, smtp_host, smtp_port,
                    smtp_user, smtp_password, smtp_security, graph_tenant_id, graph_client_id, graph_client_secret, graph_sender, updated_at)
                    VALUES (@CompanyId, @Provider, @SystemAllowed, @FromAddress, @FromName, @SmtpHost, @SmtpPort, @SmtpUser, @SmtpPassword,
                    @SmtpSecurity, @GraphTenantId, @GraphClientId, @GraphClientSecret, @GraphSender, @UpdatedAt)"), values);
            }
        }

        private DbConnection OpenTenant(string tenantName) => connections.Open(connections.TenantDatabase(tenantName));

        private object Values(EmailTransportSettings s) => new
        {
            Provider = EmailTransportKind.Normalize(s.Kind),
            FromAddress = Clean(s.FromAddress),
            FromName = Clean(s.FromName),
            SmtpHost = Clean(s.SmtpHost),
            s.SmtpPort,
            SmtpUser = Clean(s.SmtpUser),
            SmtpPassword = protector.Protect(s.SmtpPassword),
            SmtpSecurity = s.SmtpSecurity.ToString(),
            GraphTenantId = Clean(s.GraphTenantId),
            GraphClientId = Clean(s.GraphClientId),
            GraphClientSecret = protector.Protect(s.GraphClientSecret),
            GraphSender = Clean(s.GraphSender),
            UpdatedAt = DateTime.UtcNow,
        };

        private EmailTransportSettings ToSettings(Row row)
        {
            var kind = EmailTransportKind.Normalize(row.Provider);
            var smtpReadable = protector.TryUnprotect(row.SmtpPassword, out var smtpPassword);
            var graphReadable = protector.TryUnprotect(row.GraphClientSecret, out var graphSecret);
            // only the secret of the chosen transport matters
            var readable = kind == EmailTransportKind.Graph ? graphReadable : kind is EmailTransportKind.Smtp or EmailTransportKind.Gmail ? smtpReadable : true;
            return new EmailTransportSettings
            {
                Kind = kind,
                SystemAllowed = row.SystemAllowed,
                FromAddress = row.FromAddress,
                FromName = row.FromName,
                SmtpHost = row.SmtpHost,
                SmtpPort = row.SmtpPort,
                SmtpUser = row.SmtpUser,
                SmtpPassword = smtpPassword,
                SmtpSecurity = Enum.TryParse<SmtpSecurity>(row.SmtpSecurity, true, out var security) ? security : SmtpSecurity.Auto,
                GraphTenantId = row.GraphTenantId,
                GraphClientId = row.GraphClientId,
                GraphClientSecret = graphSecret,
                GraphSender = row.GraphSender,
                UpdatedAt = row.UpdatedAt == null ? null : AuthChallengeRepository.AsUtc(row.UpdatedAt.Value),
                UnreadableSecret = !readable,
            };
        }

        private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
