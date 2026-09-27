using System;
using System.Data.Common;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Database;

namespace VgAuto.Core.Persistence
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
                case DatabaseProvider.MySql:
                    var builder = new MySqlConnector.MySqlConnectionStringBuilder
                    {
                        Server = options.Host,
                        Port = (uint)options.Port,
                        UserID = options.UserId,
                        Password = options.Password,
                        GuidFormat = MySqlConnector.MySqlGuidFormat.Char36,
                        CharacterSet = "utf8mb4",
                    };
                    if (!string.IsNullOrEmpty(databaseName)) builder.Database = databaseName;
                    return builder.ToString();
                default:
                    throw new NotSupportedException($"Database provider {options.Provider} is not supported.");
            }
        }

        public DbConnection Create(string databaseName) => options.Provider switch
        {
            DatabaseProvider.PostgreSql => new Npgsql.NpgsqlConnection(ConnectionString(databaseName)),
            DatabaseProvider.MySql => new MySqlConnector.MySqlConnection(ConnectionString(databaseName)),
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
