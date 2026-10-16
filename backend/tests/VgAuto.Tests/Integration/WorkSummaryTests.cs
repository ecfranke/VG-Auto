using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VgAuto.Tests.Integration
{
    [Collection("api")]
    public class WorkSummaryTests
    {
        private readonly ApiFixture api;
        public WorkSummaryTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        /// <summary>The figures of the home page count the work of the own company.</summary>
        [DbFact]
        public async Task Summary_counts_open_work_started_this_month()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            long Open(JsonElement s) => s.GetProperty("openWork").GetInt64();
            long Month(JsonElement s) => s.GetProperty("startedThisMonth").GetInt64();

            var before = await Json(await server.GetAsync("/api/work/summary"));
            var clientId = (await Json(await server.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "Sum", lastName = Guid.NewGuid().ToString("N")[..6], phone = "1", emailAddresses = Array.Empty<string>(), introducedAt = DateTime.UtcNow,
            }))).GetGuid();
            await Json(await server.PostAsJsonAsync("/api/work", new { clientId, description = "d", startWithOffer = false }));

            var after = await Json(await server.GetAsync("/api/work/summary"));
            Assert.Equal(Open(before) + 1, Open(after));
            Assert.Equal(Month(before) + 1, Month(after));
            Assert.True(after.GetProperty("unpaidInvoices").GetInt64() >= after.GetProperty("overdueInvoices").GetInt64());
            Assert.True(after.GetProperty("offersAwaiting").GetInt64() >= 0);
        }
    }
}
