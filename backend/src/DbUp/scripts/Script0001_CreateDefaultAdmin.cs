using System.Data;
using Npgsql;

namespace DbUp.Scripts
{
    /// <summary>
    /// Script to create a default admin user with a pre-defined password hash
    /// and profile image during database initialization
    /// </summary> 
    internal class Script0001_CreateDefaultAdmin : DbUp.Engine.IScript
    {
        public string ProvideScript(Func<IDbCommand> dbCommandFactory)
        {
            // Get connection string from configuration
            // Password hash for the default admin
            string passwordHash = "$2a$11$zsTS62pGn5Cfca4CgqRJxebx45je/3nJj.puxIArFwtAjHew67m6i";

            // Read profile image
            byte[] profileImage;
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "default_admin.png");

            Console.WriteLine($"Loading admin profile image from: {imagePath}");

            if (File.Exists(imagePath))
            {
                profileImage = File.ReadAllBytes(imagePath);
                Console.WriteLine($"Successfully loaded profile image: {profileImage.Length} bytes");
            }
            else
            {
                Console.WriteLine($"Warning: Profile image not found at {imagePath}");
                // Create a fallback array so the script doesn't fail
                profileImage = new byte[0];
            }

            // Generate a default employee ID for the admin
            var employeeId = Guid.NewGuid();

            var command = (NpgsqlCommand)dbCommandFactory();
            command.CommandText = @"INSERT INTO domain.employee (
                        id, firstname, lastname, email, phone, proffession, description, introducedat
                    ) VALUES (
                        @Id, 'System', 'Administrator', 'admin@example.com', '', 'Administrator', 'Default system administrator', CURRENT_TIMESTAMP
                    )";

            using (command)
            {
                command.Parameters.AddWithValue("@Id", employeeId);
                command.ExecuteNonQuery();
                Console.WriteLine($"Created employee record with ID: {employeeId}");
            }
            command = (NpgsqlCommand)dbCommandFactory();
            command.CommandText = @"INSERT INTO public.user (
                        username, password, tenantname, email, validated, profile_image, employeeid
                    ) VALUES (
                        @Username, @Password, 'template', @Email, @Validated, @ProfileImage, @EmployeeId
                    )";

            using (command)
            {
                command.Parameters.AddWithValue("@Username", "admin");
                command.Parameters.AddWithValue("@Password", passwordHash);
                command.Parameters.AddWithValue("@Email", "admin@example.com");
                command.Parameters.AddWithValue("@Validated", true);
                command.Parameters.AddWithValue("@ProfileImage", profileImage);
                command.Parameters.AddWithValue("@EmployeeId", employeeId);
                command.ExecuteNonQuery();
                Console.WriteLine("Successfully created default admin user");
            }

            return "";
        }
    }
}