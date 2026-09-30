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
    /// <summary>Administrators delete vehicles that no work order uses; other users cannot delete vehicles.</summary>
    [Collection("api")]
    public class VehicleDeleteTests
    {
        private readonly ApiFixture api;
        public VehicleDeleteTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new Exception($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private static async Task Ok(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new Exception($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        private static HttpRequestMessage Delete(string resource, Guid id) =>
            new(HttpMethod.Delete, resource) { Content = JsonContent.Create(new[] { id }) };

        private async Task<HttpClient> Account(HttpClient creator, string userName, string role, Guid companyId)
        {
            var created = await Json(await creator.PostAsJsonAsync("/api/admin/users", new
            {
                firstName = "Test", lastName = userName, email = $"{userName}@example.com", createAccount = true, userName, role, companyId,
            }));
            var temporary = created.GetProperty("temporaryPassword").GetString();
            var (server, _, _) = await api.LoginAsync(userName, temporary);
            const string password = "Werkstatt-Neu-2026!";
            await Ok(await server.PutAsJsonAsync("/api/profile/changepassword", new { currentPassword = temporary, newPassword = password, confirmPassword = password }));
            return (await api.LoginAsync(userName, password)).Server;
        }

        private async Task<long> Count(string sql) => Convert.ToInt64(await TestDatabase.Scalar(api.DatabaseName, sql));

        [DbFact]
        public async Task Administrators_delete_vehicles_that_no_work_order_uses()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var companyId = (await Json(await owner.GetAsync("/api/admin/me"))).GetProperty("companyId").GetGuid();
            var admin = await Account(owner, "vadmin" + suffix, "admin", companyId);
            var user = await Account(owner, "vuser" + suffix, "user", companyId);
            var companyB = (await Json(await owner.PostAsJsonAsync("/api/admin/companies", new { name = "Vehicles B " + suffix, currency = "CAD" }))).GetGuid();
            var adminB = await Account(owner, "vadminb" + suffix, "admin", companyB);

            var clientId = (await Json(await owner.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "Vera", lastName = "Wheels" + suffix, emailAddresses = Array.Empty<string>(), introducedAt = DateTime.UtcNow,
            }))).GetGuid();
            async Task<Guid> Vehicle(string plate) => (await Json(await owner.PostAsJsonAsync("/api/vehicles", new
            {
                licensePlate = plate + suffix, manufacturer = "Honda", model = "Civic", year = 2019, introducedAt = DateTime.UtcNow, ownerId = clientId,
            }))).GetGuid();
            var unused = await Vehicle("UN");
            var used = await Vehicle("WK");
            await Json(await owner.PostAsJsonAsync("/api/work", new { clientId, vehicleId = used, description = "d", startWithOffer = false }));

            // only administrators, and only of the vehicle's company
            Assert.Equal(HttpStatusCode.Forbidden, (await user.SendAsync(Delete("/api/vehicles", unused))).StatusCode);
            Assert.False((await adminB.SendAsync(Delete("/api/vehicles", unused))).IsSuccessStatusCode);
            Assert.Equal(1L, await Count($"SELECT COUNT(*) FROM domain.vehicle WHERE id = '{unused}'"));

            // a vehicle a work order uses stays, and the administrator is told why
            var refused = await admin.SendAsync(Delete("/api/vehicles", used));
            Assert.False(refused.IsSuccessStatusCode);
            var error = await refused.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(error.GetProperty("isUserError").GetBoolean());
            Assert.Contains("1 work order", error.GetProperty("exceptionMessage").GetString());
            Assert.Equal(1L, await Count($"SELECT COUNT(*) FROM domain.vehicle WHERE id = '{used}'"));

            // the unused one goes, with its ownership history
            await Ok(await admin.SendAsync(Delete("/api/vehicles", unused)));
            Assert.Equal(0L, await Count($"SELECT COUNT(*) FROM domain.vehicle WHERE id = '{unused}'"));
            Assert.Equal(0L, await Count($"SELECT COUNT(*) FROM domain.vehicleregistration WHERE vehicleid = '{unused}'"));
            var owned = (await Json(await owner.GetAsync($"/api/vehicles/client/{clientId}"))).EnumerateArray().Select(v => v.GetProperty("id").GetGuid()).ToList();
            Assert.Equal(new[] { used }, owned);

            // written to the company's audit log
            var entries = (await Json(await admin.GetAsync("/api/admin/audit?limit=50"))).GetProperty("items").EnumerateArray().ToList();
            var entry = entries.First(e => e.GetProperty("action").GetString() == "vehicle.delete" && e.GetProperty("target").GetString() == "UN" + suffix);
            Assert.Contains("2019 Honda Civic", entry.GetProperty("details").GetString());
            Assert.Contains("Wheels" + suffix, entry.GetProperty("details").GetString());
        }
    }
}
