using System;
using System.Threading.Tasks;

namespace Carmasters.Tests.Integration
{
    public static class TestDatabase
    {
        public static string Provider => Environment.GetEnvironmentVariable("CARCARE_TEST_DB_PROVIDER") ?? "PostgreSql";
        public static bool IsMySql => Provider.Equals("MySql", StringComparison.OrdinalIgnoreCase);
        public static string Host => Environment.GetEnvironmentVariable("CARCARE_TEST_DB_HOST");
        public static int Port => int.TryParse(Environment.GetEnvironmentVariable("CARCARE_TEST_DB_PORT"), out var p) ? p : (IsMySql ? 3306 : 5432);
        public static string User => Environment.GetEnvironmentVariable("CARCARE_TEST_DB_USER") ?? "carcare";
        public static string Password => Environment.GetEnvironmentVariable("CARCARE_TEST_DB_PASSWORD") ?? "";
        public static bool IsConfigured => !string.IsNullOrWhiteSpace(Host);

        /// <summary>Runs SQL (PostgreSQL table names, rewritten for MySQL) against a test database.</summary>
        public static async Task Execute(string database, string sql)
        {
            var dialect = Carmasters.Core.Application.Database.SqlDialect.For(IsMySql
                ? Carmasters.Core.Application.Database.DatabaseProvider.MySql
                : Carmasters.Core.Application.Database.DatabaseProvider.PostgreSql);
            System.Data.Common.DbConnection connection = IsMySql
                ? new MySqlConnector.MySqlConnection($"Server={Host};Port={Port};User ID={User};Password={Password};Database={database}")
                : new Npgsql.NpgsqlConnection($"Host={Host};Port={Port};Username={User};Password={Password};Database={database}");
            await using (connection)
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = dialect.Sql(sql);
                await command.ExecuteNonQueryAsync();
            }
        }

        public static async Task Drop(string database)
        {
            if (string.IsNullOrWhiteSpace(database)) return;
            if (IsMySql)
            {
                MySqlConnector.MySqlConnection.ClearAllPools();
                await using var my = new MySqlConnector.MySqlConnection($"Server={Host};Port={Port};User ID={User};Password={Password}");
                await my.OpenAsync();
                await using var drop = my.CreateCommand();
                drop.CommandText = $"DROP DATABASE IF EXISTS `{database}`";
                await drop.ExecuteNonQueryAsync();
                return;
            }
            {
                Npgsql.NpgsqlConnection.ClearAllPools();
                await using var connection = new Npgsql.NpgsqlConnection(
                    $"Host={Host};Port={Port};Username={User};Password={Password};Database=postgres");
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)";
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
