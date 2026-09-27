using System.Data;
using System.Security.Cryptography;
using Carmasters.Core.Application.Database;

namespace DbUp.Scripts
{
    /// <summary>
    /// Creates the first administrator when the database has no users yet.
    /// Credentials come from configuration (DefaultAdmin:UserName / Password / Email);
    /// when no password is configured a random one is generated and printed once.
    /// The administrator must change the password on first login.
    /// </summary>
    internal class Script0004_CreateInitialAdmin : DbUp.Engine.IScript
    {
        public string ProvideScript(Func<IDbCommand> dbCommandFactory)
        {
            var dialect = SqlDialect.Current;
            var userTable = dialect.TableName("public", "user");
            var employeeTable = dialect.TableName("domain", "employee");

            using (var count = dbCommandFactory())
            {
                count.CommandText = $"SELECT COUNT(*) FROM {userTable}";
                if (Convert.ToInt64(count.ExecuteScalar()) > 0)
                {
                    Console.WriteLine("Users already exist, initial administrator not created.");
                    return "";
                }
            }

            var settings = DatabaseMigrator.InitialAdmin;
            var userName = string.IsNullOrWhiteSpace(settings.UserName) ? "admin" : settings.UserName.Trim();
            var email = string.IsNullOrWhiteSpace(settings.Email) ? null : settings.Email.Trim();
            var generated = string.IsNullOrWhiteSpace(settings.Password);
            var password = generated ? GeneratePassword() : settings.Password;
            var passwordHash = Carmasters.Core.Application.Authorization.PasswordHasher.getHash(password);

            byte[] profileImage = Array.Empty<byte>();
            var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "default_admin.png");
            if (File.Exists(imagePath)) profileImage = File.ReadAllBytes(imagePath);

            var employeeId = Guid.NewGuid();
            using (var command = dbCommandFactory())
            {
                command.CommandText = $@"INSERT INTO {employeeTable} (id, firstname, lastname, email, phone, proffession, description, introducedat)
                    VALUES (@Id, 'System', 'Administrator', @Email, '', 'Administrator', 'Initial system administrator', {dialect.CurrentTimestamp})";
                AddParameter(command, "@Id", employeeId);
                AddParameter(command, "@Email", (object)email ?? DBNull.Value);
                command.ExecuteNonQuery();
            }

            using (var command = dbCommandFactory())
            {
                command.CommandText = $@"INSERT INTO {userTable} (username, password, tenantname, email, validated, profile_image, employeeid, must_change_password)
                    VALUES (@Username, @Password, 'template', @Email, @Validated, @ProfileImage, @EmployeeId, @MustChange)";
                AddParameter(command, "@Username", userName);
                AddParameter(command, "@Password", passwordHash);
                AddParameter(command, "@Email", (object)email ?? DBNull.Value);
                AddParameter(command, "@Validated", false);
                AddParameter(command, "@ProfileImage", profileImage);
                AddParameter(command, "@EmployeeId", employeeId);
                AddParameter(command, "@MustChange", true);
                command.ExecuteNonQuery();
            }

            Console.WriteLine("==============================================================");
            Console.WriteLine($" Initial administrator created: {userName}");
            if (generated)
            {
                Console.WriteLine($" Generated password: {password}");
                Console.WriteLine(" It is shown only once. You must change it on first login.");
            }
            else
            {
                Console.WriteLine(" Password taken from DefaultAdmin:Password. You must change it on first login.");
            }
            Console.WriteLine("==============================================================");
            return "";
        }

        private static void AddParameter(IDbCommand command, string name, object value)
        {
            var p = command.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            command.Parameters.Add(p);
        }

        private static string GeneratePassword()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            return string.Create(16, 0, (span, _) =>
            {
                for (int i = 0; i < span.Length; i++) span[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            });
        }
    }
}
