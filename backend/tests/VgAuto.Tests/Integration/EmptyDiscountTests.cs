using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VgAuto.Tests.Integration
{
    [Collection("api")]
    public class EmptyDiscountTests
    {
        private readonly ApiFixture api;
        public EmptyDiscountTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        /// <summary>A row whose discount was never filled in used to end in an internal error when issuing.</summary>
        [DbFact]
        public async Task Rows_without_a_discount_can_be_issued()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var clientId = (await Json(await server.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "No", lastName = "Discount" + suffix, phone = "1", emailAddresses = Array.Empty<string>(), introducedAt = DateTime.UtcNow,
            }))).GetGuid();
            var started = await Json(await server.PostAsJsonAsync("/api/work", new { clientId, description = "d", startWithOffer = true }));
            var workId = started.GetProperty("workId").GetGuid();
            var offerId = started.GetProperty("activityId").GetGuid();
            (await server.PutAsJsonAsync($"/api/work/offer/{offerId}/productsorservices", new[]
            {
                new { id = Guid.Empty, code = "OC0004", name = "Oil Change", quantity = 1m, unit = "pcs", price = 90m, discount = (short?)null },
            })).EnsureSuccessStatusCode();

            await Json(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/issue/0", new { showVehicleOnPricing = true, sendClientEmail = false }));
            var jobId = (await Json(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/0/accepted", "ok"))).GetGuid();
            Assert.NotEqual(Guid.Empty, jobId);
            (await server.PutAsJsonAsync($"/api/work/{workId}/invoice/issue", new { paymentType = 2, dueDays = 14, sendClientEmail = false })).EnsureSuccessStatusCode();
        }
    }
}
