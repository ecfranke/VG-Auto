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
    public class WalkInCustomerTests
    {
        private readonly ApiFixture api;
        public WalkInCustomerTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            var text = await response.Content.ReadAsStringAsync();
            return string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
        }

        private static async Task<Guid> IssueInvoice(HttpClient server, JsonElement started)
        {
            var workId = started.GetProperty("workId").GetGuid();
            var activityId = started.GetProperty("activityId").GetGuid();
            await Json(await server.PutAsJsonAsync($"/api/work/repairjob/{activityId}/productsorservices", new[]
            {
                new { id = Guid.Empty, code = "OC", name = "Oil change", quantity = 1m, unit = "pcs", price = 90m, discount = (short?)0 },
            }));
            await Json(await server.PutAsJsonAsync($"/api/work/{workId}/invoice/issue", new { paymentType = 2, dueDays = 14, sendClientEmail = false, showVehicleOnPricing = true }));
            return workId;
        }

        [DbFact]
        public async Task A_walk_in_customer_is_saved_as_a_client_and_a_vehicle()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var started = await Json(await server.PostAsJsonAsync("/api/work", new
            {
                description = "walk in", startWithOffer = false, odo = 5000,
                walkIn = new { name = "Walk " + suffix, phone = "555-" + suffix, manufacturer = "Mazda", model = "CX-5", year = 2021, licensePlate = "WI" + suffix, vin = "", saveToRecords = true },
            }));
            var workId = started.GetProperty("workId").GetGuid();
            var work = await Json(await server.GetAsync($"/api/work/{workId}"));
            Assert.Equal("Walk " + suffix, work.GetProperty("clientName").GetString()!.Trim());
            Assert.Equal("555-" + suffix, work.GetProperty("clientPhone").GetString());
            Assert.Equal(JsonValueKind.String, work.GetProperty("clientId").ValueKind);
            Assert.Equal(JsonValueKind.String, work.GetProperty("vehicleId").ValueKind);
            Assert.False(work.GetProperty("isWalkIn").GetBoolean());

            // the vehicle belongs to the new client
            var clientId = work.GetProperty("clientId").GetGuid();
            var vehicles = await Json(await server.GetAsync($"/api/vehicles/client/{clientId}"));
            Assert.Equal("WI" + suffix, vehicles.EnumerateArray().Single().GetProperty("licensePlate").GetString());

            // the same plate again: the existing vehicle is used, not a copy
            var again = await Json(await server.PostAsJsonAsync("/api/work", new
            {
                description = "again", startWithOffer = false,
                walkIn = new { name = "Other " + suffix, licensePlate = "wi" + suffix.ToLowerInvariant(), saveToRecords = true },
            }));
            var work2 = await Json(await server.GetAsync($"/api/work/{again.GetProperty("workId").GetGuid()}"));
            Assert.Equal(work.GetProperty("vehicleId").GetGuid(), work2.GetProperty("vehicleId").GetGuid());
        }

        [DbFact]
        public async Task A_walk_in_customer_can_stay_on_the_work_only()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var started = await Json(await server.PostAsJsonAsync("/api/work", new
            {
                description = "walk in", startWithOffer = false, odo = 777,
                walkIn = new { name = "Only " + suffix, phone = "123", manufacturer = "Honda", model = "Fit", year = 2015, licensePlate = "", vin = "VW" + suffix, saveToRecords = false },
            }));
            var workId = started.GetProperty("workId").GetGuid();
            var work = await Json(await server.GetAsync($"/api/work/{workId}"));
            Assert.True(work.GetProperty("isWalkIn").GetBoolean());
            Assert.Equal("Only " + suffix, work.GetProperty("clientName").GetString());
            Assert.Equal(JsonValueKind.Null, work.GetProperty("clientId").ValueKind);
            Assert.Equal(JsonValueKind.Null, work.GetProperty("vehicleId").ValueKind);
            Assert.Equal("VW" + suffix, work.GetProperty("vehicleVin").GetString());
            Assert.Contains("2015_HF", work.GetProperty("code").GetString());

            // nothing was saved in the clients or vehicles
            var clients = await Json(await server.GetAsync($"/api/clients/page?limit=10&offset=0&searchText=Only%20{suffix}"));
            Assert.Empty(clients.GetProperty("items").EnumerateArray());

            // the list and its search know the walk-in customer
            var page = await Json(await server.GetAsync($"/api/work/page?limit=10&offset=0&scope=all&searchText=VW{suffix}"));
            var row = page.GetProperty("items").EnumerateArray().Single();
            Assert.Equal("Only " + suffix, row.GetProperty("clientName").GetString());
            Assert.Equal("VW" + suffix, row.GetProperty("licensePlate").GetString());

            // the invoice is made out to the walk-in customer, with the vehicle typed in
            await IssueInvoice(server, started);
            var invoice = await (await server.GetAsync($"/api/pricings/invoice/{workId}/html")).Content.ReadAsStringAsync();
            Assert.Contains("Only " + suffix, invoice);
            Assert.Contains("VW" + suffix, invoice);
        }

        [DbFact]
        public async Task A_walk_in_customer_needs_a_name_and_a_plate_or_vin()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            foreach (var save in new[] { true, false })
            {
                var noPlate = await server.PostAsJsonAsync("/api/work", new { description = "x", walkIn = new { name = "No plate", saveToRecords = save } });
                Assert.False(noPlate.IsSuccessStatusCode);
                Assert.Contains("\"isUserError\":true", await noPlate.Content.ReadAsStringAsync()); // a message for the user, not a crash
                var noName = await server.PostAsJsonAsync("/api/work", new { description = "x", walkIn = new { name = " ", licensePlate = "X1", saveToRecords = save } });
                Assert.False(noName.IsSuccessStatusCode);
                Assert.Contains("\"isUserError\":true", await noName.Content.ReadAsStringAsync());
            }
        }
    }
}
