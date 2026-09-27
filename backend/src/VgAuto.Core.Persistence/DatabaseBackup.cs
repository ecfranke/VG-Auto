using System.Collections.Generic;
using System.Threading.Tasks;
using VgAuto.Core.Application;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VgAuto.Core.Persistence
{
    /// <summary>
    /// Creates an SQL dump with the database vendor's own tool (pg_dump / mysqldump) installed on the API host.
    /// The password is passed through the environment, never on the command line.
    /// </summary>
    public class DatabaseBackup
    {
        private readonly ILogger<DatabaseBackup> logger;
        private readonly DbOptions options;
        private readonly string program;

        public DatabaseBackup(ILogger<DatabaseBackup> logger, IConfiguration configuration)
        {
            options = new DbOptions(); configuration.GetSection("DbOptions").Bind(options);
            program = configuration.GetSection("DatabaseBackup:Program").Value;
            this.logger = logger;
        }

        public async Task<string> Dump(string databaseName)
        {
            var shell = new ShellCommand();
            logger.LogInformation("Creating database dump of {Database}", databaseName);
            if (SqlDialect.Current.Provider == DatabaseProvider.MySql)
            {
                return await shell.Run(string.IsNullOrWhiteSpace(program) ? "mysqldump" : program,
                    new[] { "--host", options.Host, "--port", options.Port.ToString(), "--user", options.UserId,
                            "--single-transaction", "--routines", "--no-tablespaces", databaseName },
                    new Dictionary<string, string> { ["MYSQL_PWD"] = options.Password });
            }

            return await shell.Run(string.IsNullOrWhiteSpace(program) ? "pg_dump" : program,
                new[] { "--host", options.Host, "--port", options.Port.ToString(), "--username", options.UserId, "--no-password", "--dbname", databaseName },
                new Dictionary<string, string> { ["PGPASSWORD"] = options.Password });
        }
    }
}
