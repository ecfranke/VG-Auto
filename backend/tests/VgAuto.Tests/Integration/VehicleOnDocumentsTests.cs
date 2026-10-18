using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace VgAuto.Tests.Integration
{
    [Collection("api")]
    public class VehicleOnDocumentsTests
    {
        private readonly ApiFixture api;
        public VehicleOnDocumentsTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            var text = await response.Content.ReadAsStringAsync();
            return string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
        }

        private static async Task SetVehicleOptions(HttpClient server, JsonObject invoice, JsonObject estimate)
        {
            var options = JsonNode.Parse((await Json(await server.GetAsync("/api/options"))).GetRawText())!;
            options["pricing"]!["invoice"]!["vehicle"] = invoice;
            options["pricing"]!["estimate"]!["vehicle"] = estimate;
            (await server.PutAsJsonAsync("/api/options", options)).EnsureSuccessStatusCode();
        }

        private static JsonObject Vehicle(bool show, bool title, bool plate, bool odometer, bool vin) =>
            new() { ["showByDefault"] = show, ["title"] = title, ["licensePlate"] = plate, ["odometer"] = odometer, ["vin"] = vin };

        [DbFact]
        public async Task The_company_chooses_the_vehicle_details_of_invoices_and_estimates()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var before = JsonNode.Parse((await Json(await server.GetAsync("/api/options"))).GetRawText())!;
            try
            {
                var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
                var vehicleId = (await Json(await server.PostAsJsonAsync("/api/vehicles", new { licensePlate = "VD" + suffix, vin = "VIN" + suffix, manufacturer = "Kia", model = "Niro", year = 2025, introducedAt = DateTime.UtcNow }))).GetGuid();

                // invoices: plate and odometer shown by default; estimates: only the VIN, off by default
                await SetVehicleOptions(server, Vehicle(true, false, true, true, false), Vehicle(false, false, false, false, true));
                var saved = await Json(await server.GetAsync("/api/options"));
                Assert.True(saved.GetProperty("pricing").GetProperty("invoice").GetProperty("vehicle").GetProperty("showByDefault").GetBoolean());
                Assert.False(saved.GetProperty("pricing").GetProperty("estimate").GetProperty("vehicle").GetProperty("licensePlate").GetBoolean());

                async Task<(Guid Work, Guid Activity)> Start(bool offer)
                {
                    var started = await Json(await server.PostAsJsonAsync("/api/work", new { vehicleId, description = "d", startWithOffer = offer, odo = 123456 }));
                    var workId = started.GetProperty("workId").GetGuid();
                    var activityId = started.GetProperty("activityId").GetGuid();
                    await Json(await server.PutAsJsonAsync($"/api/work/{(offer ? "offer" : "repairjob")}/{activityId}/productsorservices", new[]
                    {
                        new { id = Guid.Empty, code = "OC", name = "Oil change", quantity = 1m, unit = "pcs", price = 90m, discount = (short?)0 },
                    }));
                    return (workId, activityId);
                }

                // the invoice dialog starts with the company default
                var (work1, _) = await Start(false);
                Assert.True((await Json(await server.GetAsync($"/api/work/{work1}"))).GetProperty("vehicleOnInvoiceByDefault").GetBoolean());
                await Json(await server.PutAsJsonAsync($"/api/work/{work1}/invoice/issue", new { paymentType = 2, dueDays = 14, sendClientEmail = false }));
                var invoice = await (await server.GetAsync($"/api/pricings/invoice/{work1}/html")).Content.ReadAsStringAsync();
                Assert.Contains("License plate: VD" + suffix, invoice);
                Assert.Contains("Odometer: 123456", invoice);
                Assert.DoesNotContain("VIN: VIN" + suffix, invoice);

                // switched off for this invoice
                var (work2, _) = await Start(false);
                await Json(await server.PutAsJsonAsync($"/api/work/{work2}/invoice/issue", new { paymentType = 2, dueDays = 14, sendClientEmail = false, showVehicleOnPricing = false }));
                Assert.DoesNotContain("License plate:", await (await server.GetAsync($"/api/pricings/invoice/{work2}/html")).Content.ReadAsStringAsync());

                // a new offer starts with the estimate default (off); issued with the vehicle it shows the VIN only
                var (work3, offerId) = await Start(true);
                var activities = await Json(await server.GetAsync($"/api/work/{work3}/activities"));
                Assert.False(activities.GetProperty("current").GetProperty("isVehicleLinesOnPricing").GetBoolean());
                await Json(await server.PutAsJsonAsync($"/api/work/{work3}/estimate/issue/0", new { showVehicleOnPricing = true, sendClientEmail = false }));
                var estimate = await (await server.GetAsync($"/api/pricings/offer/{offerId}/html")).Content.ReadAsStringAsync();
                Assert.Contains("VIN: VIN" + suffix, estimate);
                Assert.DoesNotContain("License plate:", estimate);
            }
            finally
            {
                (await server.PutAsJsonAsync("/api/options", before)).EnsureSuccessStatusCode();
            }
        }
    }
}
