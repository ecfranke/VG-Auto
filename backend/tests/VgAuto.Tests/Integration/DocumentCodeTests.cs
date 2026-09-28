using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace VgAuto.Tests.Integration
{
    /// <summary>Estimates and invoices are named like their work (RP_TF_2019_HC_2026_09_28_15) instead of "nr. 12".</summary>
    [Collection("api")]
    public class DocumentCodeTests
    {
        private readonly ApiFixture api;
        public DocumentCodeTests(ApiFixture api) { this.api = api; }

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

        [DbFact]
        public async Task Documents_issued_before_codes_get_the_code_of_their_work()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var clientId = (await Json(await server.PostAsJsonAsync("/api/privateclients", new { firstName = "Terry", lastName = "Fox" + suffix, emailAddresses = new[] { $"terry{suffix}@example.com" }, currentEmail = $"terry{suffix}@example.com", introducedAt = DateTime.UtcNow }))).GetGuid();
            var vehicleId = (await Json(await server.PostAsJsonAsync("/api/vehicles", new { licensePlate = "TF" + suffix, manufacturer = "Honda", model = "Civic", year = 2019, introducedAt = DateTime.UtcNow, ownerId = clientId }))).GetGuid();

            // an estimate issued twice (the second one is a new offer), accepted and invoiced
            var started = await Json(await server.PostAsJsonAsync("/api/work", new { clientId, vehicleId, description = "Brakes", startWithOffer = true }));
            var workId = started.GetProperty("workId").GetGuid();
            await Ok(await server.PutAsJsonAsync($"/api/work/offer/{started.GetProperty("activityId").GetGuid()}/productsorservices", new[]
            {
                new { id = Guid.Empty, code = "BP", name = "Brake pads", quantity = 1m, unit = "pcs", price = 80m, discount = (short?)0 },
            }));
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/issue/0", new { showVehicleOnPricing = false, sendClientEmail = false }));
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/issue/0", new { showVehicleOnPricing = false, sendClientEmail = false }));
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/1/accepted", "by phone"));
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/invoice/issue", new { paymentType = 2, dueDays = 10, sendClientEmail = false }));

            var work = await Json(await server.GetAsync($"/api/work/{workId}"));
            var workCode = work.GetProperty("code").GetString()!;
            Assert.Matches(@"^RP_TF_2019_HC_\d{4}_\d{2}_\d{2}_\d+$", workCode);
            var expectedEstimates = new[] { "OF" + workCode[2..], "OF" + workCode[2..] + "-1" };
            Assert.Equal(workCode, work.GetProperty("issuance").GetProperty("code").GetString());
            Assert.Equal(expectedEstimates, await EstimateCodes(server, workId));

            // as issued before codes existed: the old numbers are still shown, not an empty name
            await TestDatabase.Execute(api.DatabaseName, $@"UPDATE domain.pricing SET code = NULL
                WHERE id IN (SELECT invoiceid FROM domain.work WHERE id = '{workId}') OR id IN (SELECT estimateid FROM domain.offer WHERE workid = '{workId}')");
            work = await Json(await server.GetAsync($"/api/work/{workId}"));
            Assert.Equal(work.GetProperty("issuance").GetProperty("invoiceNumber").GetInt32().ToString(), work.GetProperty("issuance").GetProperty("code").GetString());
            var number = work.GetProperty("number").GetString();
            Assert.Equal(new[] { number + "-0", number + "-1" }, await EstimateCodes(server, workId));

            // the migration names them like their work
            await TestDatabase.Execute(api.DatabaseName, "DELETE FROM schemaversions WHERE scriptname LIKE '%Script0014%'");
            var migration = DatabaseMigrator.Run(new ConfigurationBuilder().AddEnvironmentVariables().Build(), logToConsole: false);
            Assert.True(migration.Successful, migration.Error?.ToString());
            work = await Json(await server.GetAsync($"/api/work/{workId}"));
            Assert.Equal(workCode, work.GetProperty("issuance").GetProperty("code").GetString());
            Assert.Equal(expectedEstimates, await EstimateCodes(server, workId));
        }

        private static async Task<string[]> EstimateCodes(HttpClient server, Guid workId) =>
            (await Json(await server.GetAsync($"/api/pricings/offers/{workId}"))).EnumerateArray()
                .Select(x => x.GetProperty("code").GetString()).OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }
}
