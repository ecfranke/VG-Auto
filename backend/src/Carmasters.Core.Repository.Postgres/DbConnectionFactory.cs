using System;
using System.Data.Common;
using Carmasters.Core.Application.Configuration;
using Carmasters.Core.Application.Database;

namespace Carmasters.Core.Persistence
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly DbOptions options;

        public DbConnectionFactory(DbOptions options)
        {
            this.options = options;
        }

        public DatabaseProvider Provider => options.Provider;

        public string UserListDatabase => options.MultiTenancy?.Enabled == true
            ? new MultiTenancyDbName(options, DbKind.Tenancy).Value
            : options.Name;

        public string TenantDatabase(string tenantName) => options.MultiTenancy?.Enabled == true
            ? new MultiTenancyDbName(options, tenantName).Value
            : options.Name;

        public string ConnectionString(string databaseName) => BuildConnectionString(options, databaseName);

        public static string BuildConnectionString(DbOptions options, string databaseName)
        {
            switch (options.Provider)
            {
                case DatabaseProvider.PostgreSql:
                    return new Npgsql.NpgsqlConnectionStringBuilder
                    {
                        Host = options.Host,
                        Port = options.Port,
                        Username = options.UserId,
                        Password = options.Password,
                        Database = databaseName
                    }.ToString();
                default:
                    throw new NotSupportedException($"Database provider {options.Provider} is not supported.");
            }
        }

        public DbConnection Create(string databaseName) => options.Provider switch
        {
            DatabaseProvider.PostgreSql => new Npgsql.NpgsqlConnection(ConnectionString(databaseName)),
            _ => throw new NotSupportedException($"Database provider {options.Provider} is not supported.")
        };

        public DbConnection Open(string databaseName)
        {
            var connection = Create(databaseName);
            connection.Open();
            return connection;
        }
    }
}
