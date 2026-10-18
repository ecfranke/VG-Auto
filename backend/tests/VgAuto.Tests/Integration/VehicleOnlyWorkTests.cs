using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VgAuto.Tests.Integration
{
    [Collection("api")]
    public class VehicleOnlyWorkTests
    {
        private readonly ApiFixture api;
        public VehicleOnlyWorkTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            var text = await response.Content.ReadAsStringAsync();
            return string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
        }

        /// <summary>A work with a vehicle but no client (walk-in): its offer and its repair job can be issued.</summary>
        [DbTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Work_with_a_vehicle_and_no_client_can_be_issued(bool startWithOffer)
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var vehicleId = (await Json(await server.PostAsJsonAsync("/api/vehicles", new { licensePlate = "VO" + suffix, manufacturer = "Kia", model = "Niro", year = 2025, introducedAt = DateTime.UtcNow }))).GetGuid();
            var started = await Json(await server.PostAsJsonAsync("/api/work", new { vehicleId, description = "walk-in", startWithOffer }));
            var workId = started.GetProperty("workId").GetGuid();
            var activityId = started.GetProperty("activityId").GetGuid();
            var kind = startWithOffer ? "offer" : "repairjob";
            await Json(await server.PutAsJsonAsync($"/api/work/{kind}/{activityId}/productsorservices", new[]
            {
                new { id = Guid.Empty, code = "OC", name = "Oil change", quantity = 1m, unit = "pcs", price = 90m, discount = (short?)0 },
            }));
            var work = await Json(await server.GetAsync($"/api/work/{workId}"));
            Assert.Equal(vehicleId, work.GetProperty("vehicleId").GetGuid());

            if (startWithOffer)
            {
                await Json(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/issue/0", new { showVehicleOnPricing = true, sendClientEmail = false }));
                var html = await (await server.GetAsync($"/api/pricings/offer/{activityId}/html")).Content.ReadAsStringAsync();
                Assert.Contains("VO" + suffix, html);
                await Json(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/0/accepted", "ok"));
            }
            await Json(await server.PutAsJsonAsync($"/api/work/{workId}/invoice/issue", new { paymentType = 2, dueDays = 14, sendClientEmail = false, showVehicleOnPricing = true }));
            var invoice = await (await server.GetAsync($"/api/pricings/invoice/{workId}/html")).Content.ReadAsStringAsync();
            Assert.Contains("Walk-in customer", invoice);
        }
    }
}
