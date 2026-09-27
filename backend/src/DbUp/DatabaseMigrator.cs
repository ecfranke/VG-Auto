using System.Reflection;
using System.Text.RegularExpressions;
using Carmasters.Core.Application.Configuration;
using Carmasters.Core.Application.Database;
using DbUp;
using DbUp.Engine;
using DbUp.Builder;
using Microsoft.Extensions.Configuration;

/// <summary>Creates / upgrades the database schema. Used by the DbUp console app and by the tests.</summary>
public static class DatabaseMigrator
{
    /// <summary>Configuration of the running migration (read by code scripts).</summary>
    public static IConfiguration Configuration { get; private set; } = new ConfigurationBuilder().Build();

    public static DatabaseUpgradeResult Run(IConfiguration configuration, bool logToConsole = true)
    {
        Configuration = configuration;
        var options = new DbOptions();
        configuration.GetSection("DbOptions").Bind(options);

        SqlDialect.Use(options.Provider);
        UpgradeEngineBuilder builder;
        string scriptFolder;

        if (options.Provider == DatabaseProvider.MySql)
        {
            var connectionString = MySqlConnectionString(options);
            EnsureMySqlDatabase(options);
            builder = DeployChanges.To.MySqlDatabase(connectionString);
            scriptFolder = "DbUp.scripts_mysql.";
        }
        else
        {
            var connectionString = PostgreSqlConnectionString(options);
            // Creates the database on first run (the configured user needs CREATEDB, or create it beforehand).
            EnsureDatabase.For.PostgresqlDatabase(connectionString);
            builder = DeployChanges.To.PostgresqlDatabase(connectionString);
            scriptFolder = "DbUp.scripts.";
        }

        builder = builder
            .WithScriptsAndCodeEmbeddedInAssembly(Assembly.GetExecutingAssembly(), scriptPath => scriptPath.EndsWith(".cs"))
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), script => script.StartsWith(scriptFolder) && script.EndsWith(".sql"))
            .WithScriptNameComparer(new CustomScriptComparer())
            .WithVariablesDisabled();

        builder = logToConsole ? builder.LogToConsole() : builder.LogToNowhere();
        return builder.Build().PerformUpgrade();
    }

    public static string PostgreSqlConnectionString(DbOptions options)
    {
        var connectionBuilder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = options.Host,
            Port = options.Port,
            Username = options.UserId,
            Password = options.Password,
            Database = options.Name
        };
        return connectionBuilder.ToString();
    }

    public static string MySqlConnectionString(DbOptions options, bool withDatabase = true)
    {
        var connectionBuilder = new MySqlConnector.MySqlConnectionStringBuilder
        {
            Server = options.Host,
            Port = (uint)options.Port,
            UserID = options.UserId,
            Password = options.Password,
            GuidFormat = MySqlConnector.MySqlGuidFormat.Char36,
            CharacterSet = "utf8mb4",
            AllowUserVariables = true,
        };
        if (withDatabase) connectionBuilder.Database = options.Name;
        return connectionBuilder.ToString();
    }

    /// <summary>Creates the database with utf8mb4 / case insensitive collation when it does not exist.</summary>
    private static void EnsureMySqlDatabase(DbOptions options)
    {
        if (!Regex.IsMatch(options.Name ?? "", "^[A-Za-z0-9_\\-]+$"))
            throw new ArgumentException("DbOptions:Name may only contain letters, digits, '_' and '-'.");
        using var connection = new MySqlConnector.MySqlConnection(MySqlConnectionString(options, withDatabase: false));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{options.Name}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci";
        command.ExecuteNonQuery();
    }

    public record InitialAdminSettings(string? UserName, string? Password, string? Email);

    public static InitialAdminSettings InitialAdmin => new InitialAdminSettings(
        Configuration["DefaultAdmin:UserName"], Configuration["DefaultAdmin:Password"], Configuration["DefaultAdmin:Email"]);
}

public class CustomScriptComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        x ??= string.Empty;
        y ??= string.Empty;
        var xMatch = Regex.Match(x, @"Script(\d+)_");
        var yMatch = Regex.Match(y, @"Script(\d+)_");

        if (xMatch.Success && yMatch.Success)
        {
            int xNum = int.Parse(xMatch.Groups[1].Value);
            int yNum = int.Parse(yMatch.Groups[1].Value);
            return xNum.CompareTo(yNum);
        }
        return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }
}
