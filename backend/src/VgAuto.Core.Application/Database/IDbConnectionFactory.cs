using System.Data.Common;

namespace VgAuto.Core.Application.Database
{
    /// <summary>Opens raw ADO.NET connections for the configured database vendor.</summary>
    public interface IDbConnectionFactory
    {
        DatabaseProvider Provider { get; }

        /// <summary>Opens a connection to the given database.</summary>
        DbConnection Open(string databaseName);

        /// <summary>Creates an unopened connection to the given database.</summary>
        DbConnection Create(string databaseName);

        /// <summary>Database holding the user accounts (the tenancy database when multi-tenancy is enabled).</summary>
        string UserListDatabase { get; }

        /// <summary>Database holding the workshop data of a tenant.</summary>
        string TenantDatabase(string tenantName);

        /// <summary>Connection string for a database.</summary>
        string ConnectionString(string databaseName);
    }
}
