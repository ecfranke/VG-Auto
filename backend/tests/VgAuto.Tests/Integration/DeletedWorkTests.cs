using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VgAuto.Tests.Integration
{
    [Collection("api")]
    public class DeletedWorkTests
    {
        private readonly ApiFixture api;
        public DeletedWorkTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        /// <summary>
        /// The newest work is deleted after its offer was issued (not sent); the next work gets the same number.
        /// Its offer used to fail with a duplicate estimate number (the old estimate was left behind).
        /// </summary>
        [DbFact]
        public async Task Offer_of_a_work_that_reuses_a_deleted_work_number_can_be_issued()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var clientId = (await Json(await server.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "Deleted", lastName = "Work" + suffix, phone = "1", emailAddresses = Array.Empty<string>(), introducedAt = DateTime.UtcNow,
            }))).GetGuid();

            async Task<Guid> StartAndIssueOffer()
            {
                var started = await Json(await server.PostAsJsonAsync("/api/work", new { clientId, description = "d", startWithOffer = true }));
                var workId = started.GetProperty("workId").GetGuid();
                var offerId = started.GetProperty("activityId").GetGuid();
                (await server.PutAsJsonAsync($"/api/work/offer/{offerId}/productsorservices", new[]
                {
                    new { id = Guid.Empty, code = "OC0004", name = "Oil Change", quantity = 1m, unit = "pcs", price = 90m, discount = (short?)0 },
                })).EnsureSuccessStatusCode();
                await Json(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/issue/0", new { showVehicleOnPricing = false, sendClientEmail = false }));
                return workId;
            }

            var first = await StartAndIssueOffer();
            (await server.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/work") { Content = JsonContent.Create(new[] { first }) })).EnsureSuccessStatusCode();

            var second = await StartAndIssueOffer();
            Assert.NotEqual(first, second);
        }
    }
}
