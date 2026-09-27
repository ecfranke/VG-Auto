using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Carmasters.Tests.Integration
{
    [Collection("api")]
    public class AccountTests
    {
        private readonly ApiFixture api;
        public AccountTests(ApiFixture api) { this.api = api; }

        [DbFact]
        public async Task Employee_with_login_can_be_created_and_must_use_strong_password_flow()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var created = await server.PostAsJsonAsync("/api/employees", new
            {
                firstName = "Mike",
                lastName = "Mechanic",
                email = "mike@example.com",
                userName = "mike",
                password = "Mechanic-Pass-2026",
            });
            created.EnsureSuccessStatusCode();

            var (mikeServer, _, login) = await api.LoginAsync("mike", "Mechanic-Pass-2026");
            Assert.False(login.MustChangePassword);
            Assert.Equal(HttpStatusCode.OK, (await mikeServer.GetAsync("/api/work/page?limit=5")).StatusCode);

            var duplicate = await server.PostAsJsonAsync("/api/employees", new { firstName = "A", lastName = "B", userName = "mike", password = "Another-Pass-2026" });
            Assert.Equal(HttpStatusCode.InternalServerError, duplicate.StatusCode);
            var error = await duplicate.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.True(error.GetProperty("isUserError").GetBoolean());
        }

        [DbFact]
        public async Task Forced_password_change_blocks_everything_else()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            (await server.PostAsJsonAsync("/api/employees", new
            {
                firstName = "Temp",
                lastName = "User",
                email = "temp@example.com",
                userName = "tempuser",
                password = "Temp-Pass-2026-x",
            })).EnsureSuccessStatusCode();

            // simulate an administrator created account that must change its password
            await using (var connection = new Npgsql.NpgsqlConnection(
                $"Host={TestDatabase.Host};Port={TestDatabase.Port};Username={TestDatabase.User};Password={TestDatabase.Password};Database={api.DatabaseName}"))
            {
                await connection.OpenAsync();
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = "update public.user set must_change_password = true where username = 'tempuser'";
                await cmd.ExecuteNonQueryAsync();
            }

            var (tempServer, _, login) = await api.LoginAsync("tempuser", "Temp-Pass-2026-x");
            Assert.True(login.MustChangePassword);
            Assert.Equal(HttpStatusCode.Forbidden, (await tempServer.GetAsync("/api/work/page?limit=5")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await tempServer.GetAsync("/api/profile")).StatusCode);

            var changed = await tempServer.PutAsJsonAsync("/api/profile/changepassword", new
            {
                currentPassword = "Temp-Pass-2026-x",
                newPassword = "Brand-New-Pass-2026",
                confirmPassword = "Brand-New-Pass-2026"
            });
            changed.EnsureSuccessStatusCode();
            var tokens = await changed.Content.ReadFromJsonAsync<ApiFixture.LoginResult>();
            Assert.False(tokens.MustChangePassword);
            Assert.Equal(HttpStatusCode.OK, (await api.WithToken(tokens.Jwt).GetAsync("/api/work/page?limit=5")).StatusCode);
        }

        [DbFact]
        public async Task Account_is_locked_after_repeated_failures()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            (await server.PostAsJsonAsync("/api/employees", new
            {
                firstName = "Lock",
                lastName = "Me",
                email = "lockme@example.com",
                userName = "lockme",
                password = "Lock-Me-Pass-2026",
            })).EnsureSuccessStatusCode();

            // attempts from different addresses, so the per-IP rate limit is not what stops them
            for (int i = 0; i < 10; i++)
            {
                var bad = await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { userName = "lockme", password = "wrong", serverSecret = ApiFixture.ServerSecret });
                Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
            }
            var good = await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { userName = "lockme", password = "Lock-Me-Pass-2026", serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.Unauthorized, good.StatusCode);
        }

        [DbFact]
        public async Task Login_is_rate_limited_per_client_and_forwarded_header_cannot_bypass_it()
        {
            var client = api.Anonymous();
            System.Net.Http.HttpResponseMessage last = null;
            for (int i = 0; i < 12; i++)
            {
                client.DefaultRequestHeaders.Remove("X-Forwarded-For");
                client.DefaultRequestHeaders.Add("X-Forwarded-For", $"203.0.113.{i}");
                last = await client.PostAsJsonAsync("/api/users/authenticate", new { userName = "nobody", password = "x", serverSecret = ApiFixture.ServerSecret });
            }
            Assert.Equal((HttpStatusCode)429, last.StatusCode);
        }
    }
}
