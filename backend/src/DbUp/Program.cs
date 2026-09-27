using Microsoft.Extensions.Configuration;

// Configuration: appsettings.json (+ Development/Production) + appsettings.Secrets.json + environment variables.
var builder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false);
#if DEBUG
builder.AddJsonFile("appsettings.Development.json", optional: true);
#else
builder.AddJsonFile("appsettings.Production.json", optional: true);
#endif
builder.AddJsonFile("appsettings.Secrets.json", optional: false)
       .AddEnvironmentVariables();

var result = DatabaseMigrator.Run(builder.Build());

if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(result.Error);
    Console.ResetColor();
    return -1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Success!");
Console.ResetColor();
return 0;
