using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VgAuto.Core.Application.Authorization
{
    public record AuditEntry(long Id, DateTime CreatedAt, string TenantName, string Actor, string Action, string Target, string Details);

    /// <summary>What administrators did: accounts, roles, passwords and company settings.</summary>
    public interface IAdminAuditLog
    {
        Task WriteAsync(string tenantName, string actor, string action, string target, string details = null);
        Task<(IReadOnlyList<AuditEntry> Items, int Total)> PageAsync(string tenantName, int limit, int offset);
    }
}
