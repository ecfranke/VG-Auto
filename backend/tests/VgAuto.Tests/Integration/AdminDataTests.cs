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
    public class AdminDataTests
    {
        private readonly ApiFixture api;
        public AdminDataTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private async Task<HttpClient> NewUser(HttpClient owner, Guid companyId, string role, string suffix)
        {
            var name = role + suffix;
            var created = await Json(await owner.PostAsJsonAsync("/api/admin/users", new
            {
                firstName = role, lastName = suffix, email = $"{name}@example.com", createAccount = true, userName = name, role, companyId,
            }));
            var temporary = created.GetProperty("temporaryPassword").GetString();
            var (first, _, _) = await api.LoginAsync(name, temporary);
            const string password = "Werkstatt-Neu-2026!";
            (await first.PutAsJsonAsync("/api/profile/changepassword", new { currentPassword = temporary, newPassword = password, confirmPassword = password })).EnsureSuccessStatusCode();
            return (await api.LoginAsync(name, password)).Server;
        }

        private static async Task<string[]> Names(HttpClient client, string list, string field, string search) =>
            (await Json(await client.GetAsync($"/api/admin/{list}?searchText={search}"))).GetProperty("items").EnumerateArray()
                .Select(x => x.GetProperty(field).GetString()).ToArray();

        [DbFact]
        public async Task Administrators_delete_unused_clients_and_vehicles_of_their_company()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var companyId = (await Json(await owner.PostAsJsonAsync("/api/admin/companies", new { name = "Del " + suffix, currency = "CAD" }))).GetGuid();
            var admin = await NewUser(owner, companyId, "admin", suffix);
            var user = await NewUser(owner, companyId, "user", suffix);

            // data of the new company, plus a client of the owner's company with the same search text
            var clientId = (await Json(await admin.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "Del", lastName = suffix, phone = "1", emailAddresses = Array.Empty<string>(), introducedAt = DateTime.UtcNow,
            }))).GetGuid();
            var vehicleId = (await Json(await admin.PostAsJsonAsync("/api/vehicles", new { licensePlate = "DL" + suffix, manufacturer = "Kia", model = "Niro", year = 2025, introducedAt = DateTime.UtcNow, ownerId = clientId }))).GetGuid();
            var otherId = (await Json(await owner.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "Other", lastName = suffix, phone = "1", emailAddresses = Array.Empty<string>(), introducedAt = DateTime.UtcNow,
            }))).GetGuid();

            // an administrator sees only the own company, the super administrator every company
            Assert.Equal(new[] { "Del " + suffix }, await Names(admin, "clients", "name", suffix));
            Assert.Equal(2, (await Names(owner, "clients", "name", suffix)).Length);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/clients/{otherId}")).StatusCode);

            // normal users cannot delete, neither in the administration nor in the app
            Assert.Equal(HttpStatusCode.Forbidden, (await user.DeleteAsync($"/api/admin/vehicles/{vehicleId}")).StatusCode);
            var inApp = await user.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/clients") { Content = JsonContent.Create(new[] { clientId }) });
            Assert.Contains("Only administrators", await inApp.Content.ReadAsStringAsync());

            // the client still owns the vehicle; the vehicle has work: both are kept, with the reason
            var started = await Json(await admin.PostAsJsonAsync("/api/work", new { vehicleId, description = "d", startWithOffer = false }));
            var clientError = await admin.DeleteAsync($"/api/admin/clients/{clientId}");
            Assert.Contains("1 vehicle", await clientError.Content.ReadAsStringAsync());
            var vehicleError = await admin.DeleteAsync($"/api/admin/vehicles/{vehicleId}");
            Assert.Contains("1 work order uses", await vehicleError.Content.ReadAsStringAsync());

            // without the work: the vehicle, then the client can be deleted
            (await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/work") { Content = JsonContent.Create(new[] { started.GetProperty("workId").GetGuid() }) })).EnsureSuccessStatusCode();
            (await admin.DeleteAsync($"/api/admin/vehicles/{vehicleId}")).EnsureSuccessStatusCode();
            Assert.Empty(await Names(admin, "vehicles", "licensePlate", suffix));
            (await admin.DeleteAsync($"/api/admin/clients/{clientId}")).EnsureSuccessStatusCode();
            Assert.Empty(await Names(admin, "clients", "name", suffix));

            // the super administrator deletes in any company
            (await owner.DeleteAsync($"/api/admin/clients/{otherId}")).EnsureSuccessStatusCode();
        }
    }
}
