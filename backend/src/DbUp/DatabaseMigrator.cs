using System.Reflection;
using System.Text.RegularExpressions;
using Carmasters.Core.Application.Configuration;
using Carmasters.Core.Application.Database;
using DbUp;
using DbUp.Engine;
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

        SqlDialect.Use(DatabaseProvider.PostgreSql);
        var connectionString = PostgreSqlConnectionString(options);

        // Creates the database on first run (the configured user needs CREATEDB, or create it beforehand).
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var builder = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsAndCodeEmbeddedInAssembly(Assembly.GetExecutingAssembly(), scriptPath => scriptPath.EndsWith(".cs"))
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), script => script.StartsWith("DbUp.scripts.") && script.EndsWith(".sql"))
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
