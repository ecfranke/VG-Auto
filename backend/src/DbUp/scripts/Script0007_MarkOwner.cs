using System.Data;
using VgAuto.Core.Application.Database;

namespace DbUp.Scripts
{
    /// <summary>
    /// Marks the initial administrator as the owner (super administrator that nobody else can change).
    /// The account is found by DefaultAdmin:UserName (default "admin"); on a database with a single
    /// user that user becomes the owner. Every other existing account starts as a normal user.
    /// </summary>
    internal class Script0007_MarkOwner : DbUp.Engine.IScript
    {
        public string ProvideScript(Func<IDbCommand> dbCommandFactory)
        {
            var userTable = SqlDialect.Current.TableName("public", "user");

            using (var existing = dbCommandFactory())
            {
                existing.CommandText = $"SELECT COUNT(*) FROM {userTable} WHERE is_owner = @True";
                AddParameter(existing, "@True", true);
                if (Convert.ToInt64(existing.ExecuteScalar()) > 0) return "";
            }

            var configured = DatabaseMigrator.InitialAdmin.UserName;
            var userName = string.IsNullOrWhiteSpace(configured) ? "admin" : configured.Trim();

            if (MarkOwner(dbCommandFactory, userTable, "username = @UserName", ("@UserName", userName)) == 0)
            {
                using var count = dbCommandFactory();
                count.CommandText = $"SELECT COUNT(*) FROM {userTable}";
                var users = Convert.ToInt64(count.ExecuteScalar());
                if (users == 1)
                {
                    MarkOwner(dbCommandFactory, userTable, "1 = 1");
                }
                else if (users > 1)
                {
                    Console.WriteLine("==============================================================");
                    Console.WriteLine($" No user named '{userName}' found, so no owner account was marked.");
                    Console.WriteLine(" Mark the owner yourself, for example:");
                    Console.WriteLine($"   UPDATE {userTable} SET role = 'superadmin', is_owner = true WHERE username = '<name>';");
                    Console.WriteLine("==============================================================");
                }
            }
            return "";
        }

        private static int MarkOwner(Func<IDbCommand> dbCommandFactory, string userTable, string where, params (string Name, object Value)[] parameters)
        {
            using var command = dbCommandFactory();
            command.CommandText = $"UPDATE {userTable} SET role = 'superadmin', is_owner = @True WHERE {where}";
            AddParameter(command, "@True", true);
            foreach (var (name, value) in parameters) AddParameter(command, name, value);
            var updated = command.ExecuteNonQuery();
            if (updated > 0) Console.WriteLine(" Owner account (super administrator) marked.");
            return updated;
        }

        private static void AddParameter(IDbCommand command, string name, object value)
        {
            var p = command.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            command.Parameters.Add(p);
        }
    }
}
