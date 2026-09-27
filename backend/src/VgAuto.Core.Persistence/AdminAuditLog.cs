using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Database;

namespace VgAuto.Core.Persistence
{
    public class AdminAuditLog : IAdminAuditLog
    {
        private readonly IDbConnectionFactory connections;
        public AdminAuditLog(IDbConnectionFactory connections) { this.connections = connections; }

        private static string Sql(string sql) => SqlDialect.Current.Sql(sql);

        private class Row
        {
            public long Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public string TenantName { get; set; }
            public string Actor { get; set; }
            public string Action { get; set; }
            public string Target { get; set; }
            public string Details { get; set; }
        }

        public async Task WriteAsync(string tenantName, string actor, string action, string target, string details = null)
        {
            await using var db = connections.Open(connections.UserListDatabase);
            await db.ExecuteAsync(Sql(@"INSERT INTO public.admin_audit_log (created_at, tenantname, actor, action, target, details)
                                        VALUES (@CreatedAt, @TenantName, @Actor, @Action, @Target, @Details)"),
                new
                {
                    CreatedAt = DateTime.UtcNow,
                    TenantName = tenantName ?? "",
                    Actor = Truncate(actor, 255) ?? "",
                    Action = Truncate(action, 50),
                    Target = Truncate(target, 255),
                    Details = Truncate(details, 2000),
                });
        }

        public async Task<(IReadOnlyList<AuditEntry> Items, int Total)> PageAsync(string tenantName, int limit, int offset)
        {
            limit = Math.Clamp(limit, 1, 200);
            offset = Math.Max(0, offset);
            await using var db = connections.Open(connections.UserListDatabase);
            var total = await db.ExecuteScalarAsync<long>(Sql("SELECT COUNT(*) FROM public.admin_audit_log WHERE tenantname = @tenantName"), new { tenantName });
            var rows = await db.QueryAsync<Row>(Sql(
                "SELECT id, created_at as CreatedAt, tenantname as TenantName, actor, action, target, details FROM public.admin_audit_log " +
                "WHERE tenantname = @tenantName ORDER BY created_at DESC, id DESC " + SqlDialect.Current.Paging("@limit", "@offset")),
                new { tenantName, limit, offset });
            var items = rows.Select(r => new AuditEntry(r.Id, AuthChallengeRepository.AsUtc(r.CreatedAt), r.TenantName, r.Actor, r.Action, r.Target, r.Details)).ToList();
            return (items, (int)total);
        }

        private static string Truncate(string value, int max) => value == null || value.Length <= max ? value : value[..max];
    }
}
