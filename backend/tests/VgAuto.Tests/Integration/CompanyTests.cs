using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VgAuto.Tests.Integration
{
    [Collection("api")]
    public class CompanyTests
    {
        private readonly ApiFixture api;
        public CompanyTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private async Task<HttpClient> UserOf(HttpClient admin, Guid companyId, string userName)
        {
            var created = await Json(await admin.PostAsJsonAsync("/api/admin/users", new
            {
                firstName = "Test", lastName = userName, email = $"{userName}@example.com",
                createAccount = true, userName, role = "user", companyId,
            }));
            var temporary = created.GetProperty("temporaryPassword").GetString();
            var (server, _, _) = await api.LoginAsync(userName, temporary);
            const string password = "Werkstatt-Neu-2026!";
            (await server.PutAsJsonAsync("/api/profile/changepassword", new { currentPassword = temporary, newPassword = password, confirmPassword = password })).EnsureSuccessStatusCode();
            return (await api.LoginAsync(userName, password)).Server;
        }

        private static async Task<(Guid ClientId, Guid WorkId, int Number)> CreateWork(HttpClient server, string name)
        {
            var clientId = (await Json(await server.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "Client", lastName = name, phone = "1",
                emailAddresses = new[] { $"{name}@example.com" }, currentEmail = $"{name}@example.com", introducedAt = DateTime.UtcNow,
            }))).GetGuid();
            var started = await Json(await server.PostAsJsonAsync("/api/work", new { clientId, description = "Service " + name, startWithOffer = false }));
            var workId = started.GetProperty("workId").GetGuid();
            var work = await Json(await server.GetAsync($"/api/work/{workId}"));
            return (clientId, workId, int.Parse(work.GetProperty("number").GetString()!));
        }

        [DbFact]
        public async Task Companies_keep_their_data_numbers_and_settings_apart()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];

            // a second company, created and configured by an administrator
            var companyB = (await Json(await owner.PostAsJsonAsync("/api/admin/companies", new { name = "Branch " + suffix, currency = "USD" }))).GetGuid();
            var list = await Json(await owner.GetAsync("/api/admin/companies"));
            var row = list.EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == companyB);
            Assert.Equal("USD", row.GetProperty("currency").GetString());
            var optionsB = await Json(await owner.GetAsync($"/api/admin/companies/{companyB}/options"));
            Assert.Equal("Branch " + suffix, optionsB.GetProperty("requisites").GetProperty("name").GetString());
            var edited = System.Text.Json.Nodes.JsonNode.Parse(optionsB.GetRawText())!;
            edited["requisites"]!["regNr"] = "B-" + suffix;
            (await owner.PutAsJsonAsync($"/api/admin/companies/{companyB}/options", edited)).EnsureSuccessStatusCode();

            var staffB = await UserOf(owner, companyB, "staffb" + suffix);

            // the user of company B sees B's settings, not the owner's
            var own = await Json(await staffB.GetAsync("/api/options"));
            Assert.Equal("B-" + suffix, own.GetProperty("requisites").GetProperty("regNr").GetString());
            Assert.Equal("USD", own.GetProperty("pricing").GetProperty("currency").GetString());
            var ownerOptions = await Json(await owner.GetAsync("/api/options"));
            Assert.NotEqual("B-" + suffix, ownerOptions.GetProperty("requisites").GetProperty("regNr").GetString());

            // data of company A is invisible to B, and the other way round
            var (clientA, workA, _) = await CreateWork(owner, "alpha" + suffix);
            var (clientB, workB, numberB) = await CreateWork(staffB, "beta" + suffix);
            Assert.Equal(1, numberB); // numbers are counted per company

            Assert.Empty((await Json(await staffB.GetAsync($"/api/clients/page?limit=10&searchText=alpha{suffix}"))).GetProperty("items").EnumerateArray());
            Assert.Single((await Json(await staffB.GetAsync($"/api/clients/page?limit=10&searchText=beta{suffix}"))).GetProperty("items").EnumerateArray());
            Assert.Empty((await Json(await owner.GetAsync($"/api/clients/page?limit=10&searchText=beta{suffix}"))).GetProperty("items").EnumerateArray());
            Assert.Empty((await Json(await staffB.GetAsync($"/api/query/alpha{suffix}"))).EnumerateArray());
            Assert.DoesNotContain((await Json(await staffB.GetAsync("/api/work/page?limit=100"))).GetProperty("items").EnumerateArray(),
                w => w.GetProperty("id").GetGuid() == workA);
            Assert.False((await staffB.GetAsync($"/api/work/{workA}")).IsSuccessStatusCode);
            Assert.False((await staffB.GetAsync($"/api/privateclients/{clientA}")).IsSuccessStatusCode);
            Assert.False((await staffB.GetAsync($"/api/work/{workA}/activities")).IsSuccessStatusCode);
            Assert.False((await owner.GetAsync($"/api/work/{workB}")).IsSuccessStatusCode);
            Assert.DoesNotContain((await Json(await staffB.GetAsync("/api/employees"))).EnumerateArray(),
                e => e.GetProperty("name").GetString() == "System Administrator");

            // the administration sees everybody with their company
            var users = await Json(await owner.GetAsync("/api/admin/users"));
            var userB = users.EnumerateArray().Single(u => u.GetProperty("userName").GetString() == "staffb" + suffix);
            Assert.Equal(companyB, userB.GetProperty("companyId").GetGuid());
            Assert.Equal("Branch " + suffix, userB.GetProperty("companyName").GetString());

            // moving the user to the first company shows that company's data
            var firstCompany = ownerOptions.GetProperty("requisites").GetProperty("name").GetString();
            var companyA = (await Json(await owner.GetAsync("/api/admin/companies"))).EnumerateArray()
                .First(c => c.GetProperty("id").GetGuid() != companyB).GetProperty("id").GetGuid();
            (await owner.PutAsJsonAsync($"/api/admin/users/{userB.GetProperty("employeeId").GetGuid()}/company", new { companyId = companyA })).EnsureSuccessStatusCode();
            Assert.Single((await Json(await staffB.GetAsync($"/api/clients/page?limit=10&searchText=alpha{suffix}"))).GetProperty("items").EnumerateArray());
            Assert.Empty((await Json(await staffB.GetAsync($"/api/clients/page?limit=10&searchText=beta{suffix}"))).GetProperty("items").EnumerateArray());
            Assert.NotNull(firstCompany);
        }
    }
}
