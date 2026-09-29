using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VgAuto.Core.Application.Authorization
{
    /// <summary>CompanyId: the company the entry is about; null for the whole system.</summary>
    public record AuditEntry(long Id, DateTime CreatedAt, string TenantName, string Actor, string Action, string Target, string Details, Guid? CompanyId = null);

    /// <summary>What administrators did: accounts, roles, passwords and company settings.</summary>
    public interface IAdminAuditLog
    {
        /// <param name="companyId">the company the entry is about (its administrators see it); null for the whole system</param>
        Task WriteAsync(string tenantName, string actor, string action, string target, string details = null, Guid? companyId = null);
        /// <param name="companyId">only the entries of this company; all when null</param>
        Task<(IReadOnlyList<AuditEntry> Items, int Total)> PageAsync(string tenantName, int limit, int offset, Guid? companyId = null);
    }
}
