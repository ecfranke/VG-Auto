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

        /// <summary>
        /// An issued offer is deleted and the offer is issued again: the new offer gets the deleted one's number.
        /// Issuing it used to fail with a duplicate estimate number (500): the deleted offer's estimate was left behind.
        /// </summary>
        [DbFact]
        public async Task An_offer_can_be_issued_again_after_an_issued_offer_was_deleted()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var email = $"again{suffix}@example.com";
            var clientId = (await Json(await server.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "Deleted", lastName = "Offer" + suffix, emailAddresses = new[] { email }, currentEmail = email, introducedAt = DateTime.UtcNow,
            }))).GetGuid();
            var started = await Json(await server.PostAsJsonAsync("/api/work", new { clientId, description = "d", startWithOffer = true }));
            var workId = started.GetProperty("workId").GetGuid();
            var firstOffer = started.GetProperty("activityId").GetGuid();
            (await server.PutAsJsonAsync($"/api/work/offer/{firstOffer}/productsorservices", new[]
            {
                new { id = Guid.Empty, code = "OC0004", name = "Oil Change", quantity = 1m, unit = "pcs", price = 90m, discount = (short?)0 },
            })).EnsureSuccessStatusCode();
            async Task<Guid> Issue(bool send = false) =>
                (await Json(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/issue/0", new { showVehicleOnPricing = false, sendClientEmail = send, clientEmail = email }))).GetGuid();

            Assert.Equal(firstOffer, await Issue());
            var reissued = await Issue();
            // the page opens the new offer: its id, not an empty one
            Assert.NotEqual(Guid.Empty, reissued);
            Assert.NotEqual(firstOffer, reissued);

            (await server.DeleteAsync($"/api/work/{workId}/offer/1")).EnsureSuccessStatusCode();
            var again = await Issue(send: true);
            Assert.NotEqual(Guid.Empty, again);

            // an offer sent to the client is not deleted
            var refused = await server.DeleteAsync($"/api/work/{workId}/offer/1");
            Assert.False(refused.IsSuccessStatusCode);
            Assert.Contains("sent to the client", (await refused.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("exceptionMessage").GetString());
            var offers = (await Json(await server.GetAsync($"/api/pricings/offers/{workId}"))).GetArrayLength();
            Assert.Equal(2, offers);
        }
    }
}
