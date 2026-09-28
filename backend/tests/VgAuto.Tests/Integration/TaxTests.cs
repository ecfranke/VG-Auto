using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace VgAuto.Tests.Integration
{
    [Collection("api")]
    public class TaxTests
    {
        private readonly ApiFixture api;
        public TaxTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        [DbFact]
        public async Task Prices_are_before_tax_and_each_tax_of_the_region_is_shown()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];

            // a company registered in British Columbia: GST 5 % + PST 7 %
            var companyId = (await Json(await owner.PostAsJsonAsync("/api/admin/companies", new { name = "Tax " + suffix, currency = "CAD" }))).GetGuid();
            var options = JsonNode.Parse((await Json(await owner.GetAsync($"/api/admin/companies/{companyId}/options"))).GetRawText())!;
            Assert.Equal("GST", options["pricing"]!["taxes"]!["tax1Name"]!.GetValue<string>()); // new companies start with GST
            options["pricing"]!["taxes"] = new JsonObject
            {
                ["country"] = "CA", ["region"] = "BC",
                ["tax1Name"] = "GST", ["tax1Rate"] = 5m, ["tax2Name"] = "PST", ["tax2Rate"] = 7m,
            };
            options["requisites"]!["kmkr"] = "123456789RT0001";
            (await owner.PutAsJsonAsync($"/api/admin/companies/{companyId}/options", options)).EnsureSuccessStatusCode();

            var regions = await Json(await owner.GetAsync("/api/options/taxregions"));
            var bc = regions.EnumerateArray().Single(c => c.GetProperty("code").GetString() == "CA")
                .GetProperty("regions").EnumerateArray().Single(r => r.GetProperty("code").GetString() == "BC");
            Assert.Equal(new[] { "GST", "PST" }, bc.GetProperty("taxes").EnumerateArray().Select(t => t.GetProperty("name").GetString()));

            // a user of that company invoices 2 x 100.00 before tax
            var created = await Json(await owner.PostAsJsonAsync("/api/admin/users", new
            {
                firstName = "Tax", lastName = suffix, email = $"tax{suffix}@example.com",
                createAccount = true, userName = "tax" + suffix, role = "user", companyId,
            }));
            var temporary = created.GetProperty("temporaryPassword").GetString();
            var (first, _, _) = await api.LoginAsync("tax" + suffix, temporary);
            const string password = "Werkstatt-Neu-2026!";
            (await first.PutAsJsonAsync("/api/profile/changepassword", new { currentPassword = temporary, newPassword = password, confirmPassword = password })).EnsureSuccessStatusCode();
            var user = (await api.LoginAsync("tax" + suffix, password)).Server;

            var own = await Json(await user.GetAsync("/api/options"));
            Assert.Equal("BC", own.GetProperty("pricing").GetProperty("taxes").GetProperty("region").GetString());

            // a normal user can change tax names and rates, but not the place of registration
            var mine = JsonNode.Parse(own.GetRawText())!;
            mine["pricing"]!["taxes"]!["region"] = "ON";
            (await user.PutAsJsonAsync("/api/options", mine)).EnsureSuccessStatusCode();
            Assert.Equal("BC", (await Json(await user.GetAsync("/api/options"))).GetProperty("pricing").GetProperty("taxes").GetProperty("region").GetString());

            var clientId = (await Json(await user.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "Client", lastName = suffix, phone = "1",
                emailAddresses = new[] { $"c{suffix}@example.com" }, currentEmail = $"c{suffix}@example.com", introducedAt = DateTime.UtcNow,
            }))).GetGuid();
            var started = await Json(await user.PostAsJsonAsync("/api/work", new { clientId, description = "Brakes " + suffix, startWithOffer = false }));
            var workId = started.GetProperty("workId").GetGuid();
            var jobId = started.GetProperty("activityId").GetGuid();
            (await user.PutAsJsonAsync($"/api/work/repairjob/{jobId}/productsorservices", new[]
            {
                new { id = Guid.Empty, code = "BP", name = "Brake pads", quantity = 2m, unit = "pcs", price = 100m, discount = (short?)0 },
            })).EnsureSuccessStatusCode();

            var activities = await Json(await user.GetAsync($"/api/work/{workId}/activities"));
            var summary = activities.GetProperty("current").GetProperty("priceSummary");
            Assert.Equal(200m, summary.GetProperty("totalWithoutVat").GetDecimal());
            Assert.Equal(224m, summary.GetProperty("totalWithVat").GetDecimal());
            Assert.Equal(new[] { 10m, 14m }, summary.GetProperty("taxes").EnumerateArray().Select(t => t.GetProperty("amount").GetDecimal()));

            (await user.PutAsJsonAsync($"/api/work/{workId}/invoice/issue", new { paymentType = 2, dueDays = 14, sendClientEmail = false })).EnsureSuccessStatusCode();
            var html = await (await user.GetAsync($"/api/pricings/invoice/{workId}/html")).Content.ReadAsStringAsync();
            Assert.Contains("GST (5%)", html);
            Assert.Contains("PST (7%)", html);
            Assert.Contains("$10.00", html);
            Assert.Contains("$14.00", html);
            Assert.Contains("$224.00 CAD", html);
            Assert.Contains("100.00", html); // the unit price is printed as entered (before tax)
            Assert.Contains("GST/HST No.: 123456789RT0001", html);
            Assert.DoesNotContain("Reg No:", html);        // hidden unless switched on
            Assert.DoesNotContain("Bank account:", html);
            var shown = JsonNode.Parse((await Json(await owner.GetAsync($"/api/admin/companies/{companyId}/options"))).GetRawText())!;
            shown["requisites"]!["regNr"] = "R-" + suffix;
            shown["requisites"]!["bankAccount"] = "B-" + suffix;
            shown["pricing"]!["invoice"]!["showRegNo"] = true;
            shown["pricing"]!["invoice"]!["showBankAccount"] = true;
            (await owner.PutAsJsonAsync($"/api/admin/companies/{companyId}/options", shown)).EnsureSuccessStatusCode();
            var withNumbers = await (await user.GetAsync($"/api/pricings/invoice/{workId}/html")).Content.ReadAsStringAsync();
            Assert.Contains("Reg No: R-" + suffix, withNumbers);
            Assert.Contains("Bank account: B-" + suffix, withNumbers);

            // the invoice keeps its taxes when the settings change later
            var later = JsonNode.Parse((await Json(await owner.GetAsync($"/api/admin/companies/{companyId}/options"))).GetRawText())!;
            later["pricing"]!["taxes"] = new JsonObject { ["country"] = "CA", ["region"] = "ON", ["tax1Name"] = "HST", ["tax1Rate"] = 13m, ["tax2Name"] = null, ["tax2Rate"] = 0m };
            (await owner.PutAsJsonAsync($"/api/admin/companies/{companyId}/options", later)).EnsureSuccessStatusCode();
            var again = await (await user.GetAsync($"/api/pricings/invoice/{workId}/html")).Content.ReadAsStringAsync();
            Assert.Contains("PST (7%)", again);
            Assert.DoesNotContain("HST (13%)", again);
        }
    }
}
