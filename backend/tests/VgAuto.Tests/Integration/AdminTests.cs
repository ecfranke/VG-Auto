using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VgAuto.Tests.Integration
{
    [Collection("api")]
    public class AdminTests
    {
        private readonly ApiFixture api;
        public AdminTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        /// <summary>Creates an account through the administration and signs in with it (changing the temporary password).</summary>
        private async Task<(Guid Id, HttpClient Server, string Password)> CreateAndLogin(HttpClient admin, string userName, string role)
        {
            var created = await Json(await admin.PostAsJsonAsync("/api/admin/users", new
            {
                firstName = "Test", lastName = userName, email = $"{userName}@example.com",
                createAccount = true, userName, role,
            }));
            var id = created.GetProperty("employeeId").GetGuid();
            var temporary = created.GetProperty("temporaryPassword").GetString();
            Assert.False(string.IsNullOrEmpty(temporary));

            var (server, _, login) = await api.LoginAsync(userName, temporary);
            Assert.True(login.MustChangePassword);
            var password = "Werkstatt-Neu-2026!";
            (await server.PutAsJsonAsync("/api/profile/changepassword",
                new { currentPassword = temporary, newPassword = password, confirmPassword = password })).EnsureSuccessStatusCode();
            var (fresh, _, _) = await api.LoginAsync(userName, password);
            return (id, fresh, password);
        }

        [DbFact]
        public async Task Owner_is_a_super_administrator()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var me = await Json(await owner.GetAsync("/api/admin/me"));
            Assert.Equal("superadmin", me.GetProperty("role").GetString());
            Assert.True(me.GetProperty("isOwner").GetBoolean());
        }

        [DbFact]
        public async Task Roles_limit_what_administrators_can_do()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var (adminId, admin, _) = await CreateAndLogin(owner, "office1", "admin");
            var (staffId, staff, _) = await CreateAndLogin(owner, "staff1", "user");

            // normal users: no administration, no company settings, no logins through /api/employees
            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/admin/users")).StatusCode);
            Assert.Equal("user", (await Json(await staff.GetAsync("/api/admin/me"))).GetProperty("role").GetString());
            var options = await Json(await owner.GetAsync("/api/options"));
            Assert.Equal(HttpStatusCode.Forbidden, (await staff.PutAsJsonAsync("/api/options", options)).StatusCode);
            Assert.False((await staff.PostAsJsonAsync("/api/employees", new { firstName = "X", lastName = "Y", userName = "sneaky", password = "Sneaky-Pass-2026" })).IsSuccessStatusCode);
            (await staff.PostAsJsonAsync("/api/employees", new { firstName = "Mechanic", lastName = "Only" })).EnsureSuccessStatusCode();

            // administrators: company settings and normal users, but no administrators
            (await admin.PutAsJsonAsync("/api/options", options)).EnsureSuccessStatusCode();
            (await admin.PutAsJsonAsync($"/api/admin/users/{staffId}", new { firstName = "Staff", lastName = "Renamed", email = "staff1@example.com" })).EnsureSuccessStatusCode();
            Assert.False((await admin.PostAsJsonAsync("/api/admin/users", new { firstName = "A", lastName = "B", email = "x@example.com", createAccount = true, userName = "another_admin", role = "admin" })).IsSuccessStatusCode);
            Assert.False((await admin.PutAsJsonAsync($"/api/admin/users/{staffId}/role", new { role = "admin" })).IsSuccessStatusCode);
            var ownerId = (await Json(await owner.GetAsync("/api/admin/users"))).EnumerateArray().First(u => u.GetProperty("isOwner").GetBoolean()).GetProperty("employeeId").GetGuid();
            Assert.False((await admin.PostAsync($"/api/admin/users/{ownerId}/disable", null)).IsSuccessStatusCode);

            // super administrators change roles; the owner stays untouchable
            (await owner.PutAsJsonAsync($"/api/admin/users/{staffId}/role", new { role = "admin" })).EnsureSuccessStatusCode();
            (await owner.PutAsJsonAsync($"/api/admin/users/{staffId}/role", new { role = "user" })).EnsureSuccessStatusCode();
            Assert.False((await owner.PutAsJsonAsync($"/api/admin/users/{ownerId}/role", new { role = "user" })).IsSuccessStatusCode);
            Assert.False((await owner.PostAsync($"/api/admin/users/{ownerId}/disable", null)).IsSuccessStatusCode);

            var list = await Json(await admin.GetAsync("/api/admin/users"));
            var adminRow = list.EnumerateArray().First(u => u.GetProperty("employeeId").GetGuid() == adminId);
            Assert.True(adminRow.GetProperty("isSelf").GetBoolean());
        }

        [DbFact]
        public async Task Disabled_accounts_lose_access_immediately()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var (id, staff, password) = await CreateAndLogin(owner, "leaver1", "user");
            Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/work/page?limit=5")).StatusCode);

            (await owner.PostAsync($"/api/admin/users/{id}/disable", null)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Unauthorized, (await staff.GetAsync("/api/work/page?limit=5")).StatusCode);
            var login = await api.NewClient().PostAsJsonAsync("/api/auth/login", new { userName = "leaver1", password, serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
            Assert.Equal("accountDisabled", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString());

            (await owner.PostAsync($"/api/admin/users/{id}/enable", null)).EnsureSuccessStatusCode();
            var (again, _, _) = await api.LoginAsync("leaver1", password);
            Assert.Equal(HttpStatusCode.OK, (await again.GetAsync("/api/work/page?limit=5")).StatusCode);
        }

        [DbFact]
        public async Task Password_reset_sets_a_temporary_password_and_everything_is_logged()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var (id, _, _) = await CreateAndLogin(owner, "forgetful1", "user");

            var reset = await Json(await owner.PostAsJsonAsync($"/api/admin/users/{id}/password", new { }));
            var temporary = reset.GetProperty("temporaryPassword").GetString();
            var (_, _, login) = await api.LoginAsync("forgetful1", temporary);
            Assert.True(login.MustChangePassword);

            (await owner.PostAsync($"/api/admin/users/{id}/unlock", null)).EnsureSuccessStatusCode();
            var audit = await Json(await owner.GetAsync("/api/admin/audit?limit=100"));
            var actions = audit.GetProperty("items").EnumerateArray()
                .Where(e => e.GetProperty("target").GetString() == "forgetful1")
                .Select(e => e.GetProperty("action").GetString()).ToList();
            Assert.Contains("user.create", actions);
            Assert.Contains("user.password_reset", actions);
            Assert.Contains("user.unlock", actions);
            Assert.All(audit.GetProperty("items").EnumerateArray().Where(e => e.GetProperty("target").GetString() == "forgetful1"),
                e => Assert.Equal("admin", e.GetProperty("actor").GetString()));
        }

        [DbFact]
        public async Task Mechanic_without_login_gets_an_account_later_and_accounts_cannot_be_deleted()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var created = await Json(await owner.PostAsJsonAsync("/api/admin/users", new { firstName = "Mick", lastName = "Wrench", email = "mick@example.com", createAccount = false }));
            var id = created.GetProperty("employeeId").GetGuid();
            var row = await Json(await owner.GetAsync($"/api/admin/users/{id}"));
            Assert.False(row.GetProperty("hasAccount").GetBoolean());

            var account = await Json(await owner.PostAsJsonAsync($"/api/admin/users/{id}/account", new { userName = "mick", role = "user" }));
            Assert.False(string.IsNullOrEmpty(account.GetProperty("temporaryPassword").GetString()));
            Assert.Equal("user", (await Json(await owner.GetAsync($"/api/admin/users/{id}"))).GetProperty("role").GetString());

            var delete = new HttpRequestMessage(HttpMethod.Delete, "/api/employees") { Content = JsonContent.Create(new[] { id }) };
            Assert.False((await owner.SendAsync(delete)).IsSuccessStatusCode);
        }
    }
}
