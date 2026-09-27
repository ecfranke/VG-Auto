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
    /// <summary>End to end walk through the main workshop flow: client, vehicle, work, offer, repair job, invoice.</summary>
    [Collection("api")]
    public class WorkflowTests
    {
        private readonly ApiFixture api;
        public WorkflowTests(ApiFixture api) { this.api = api; }

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
        public async Task Full_workshop_flow()
        {
            var (server, browser, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];

            // master data
            var clientId = (await Json(await server.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "Jane",
                lastName = "Doe" + suffix,
                phone = "555-" + suffix,
                address = new { street = "Main 1", city = "Tallinn", country = "EE" },
                emailAddresses = new[] { $"jane{suffix}@example.com" },
                currentEmail = $"jane{suffix}@example.com",
                introducedAt = DateTime.UtcNow,
            }))).GetGuid();

            var companyId = (await Json(await server.PostAsJsonAsync("/api/legalclients", new
            {
                name = "Acme " + suffix,
                regNr = "R" + suffix,
                emailAddresses = new[] { $"acme{suffix}@example.com" },
                currentEmail = $"acme{suffix}@example.com",
                introducedAt = DateTime.UtcNow,
            }))).GetGuid();

            var vehicleId = (await Json(await server.PostAsJsonAsync("/api/vehicles", new
            {
                regNr = "REG" + suffix,
                producer = "Toyota",
                model = "Corolla",
                vin = "VIN" + suffix,
                odo = 12000,
                productionDate = new DateTime(2020, 5, 1),
                introducedAt = DateTime.UtcNow,
                ownerId = clientId,
            }))).GetGuid();

            var storageId = (await Json(await server.PostAsJsonAsync("/api/storages", new { name = "Shelf " + suffix, address = "Back room", introducedAt = DateTime.UtcNow }))).GetGuid();
            var partId = (await Json(await server.PostAsJsonAsync("/api/spareparts", new
            {
                code = "OF-" + suffix,
                name = "Oil filter " + suffix,
                price = 12.5m,
                quantity = 4m,
                storageId,
            }))).GetGuid();

            // read back master data
            var vehicle = await Json(await server.GetAsync($"/api/vehicles/{vehicleId}"));
            Assert.Equal("REG" + suffix, vehicle.GetProperty("regNr").GetString());
            var client = await Json(await server.GetAsync($"/api/privateclients/{clientId}"));
            Assert.Equal("Doe" + suffix, client.GetProperty("lastName").GetString());
            var part = await Json(await server.GetAsync($"/api/spareparts/{partId}"));
            Assert.Equal(storageId, part.GetProperty("storageId").GetGuid());

            // browser side lookups
            var vehicles = await Json(await browser.GetAsync($"/api/vehicles/page?limit=10&searchText=corolla%20REG{suffix}"));
            var vehicleRow = vehicles.GetProperty("items").EnumerateArray().Single();
            Assert.StartsWith("2020-05", vehicleRow.GetProperty("productionDate").GetString());
            var clientVehicles = await Json(await browser.GetAsync($"/api/vehicles/client/{clientId}"));
            Assert.Single(clientVehicles.EnumerateArray());
            var parts = await Json(await browser.GetAsync($"/api/spareparts/page?limit=10&searchText=OF-{suffix}&orderby=name&desc=true"));
            Assert.Single(parts.GetProperty("items").EnumerateArray());
            var clients = await Json(await browser.GetAsync($"/api/clients/page?limit=10&searchText=Doe{suffix}"));
            Assert.Single(clients.GetProperty("items").EnumerateArray());
            var companies = await Json(await browser.GetAsync($"/api/clients/page?limit=10&searchText=acme%20{suffix}"));
            Assert.True(companies.GetProperty("items")[0].GetProperty("isCompany").GetBoolean());

            // work with an offer
            var started = await Json(await server.PostAsJsonAsync("/api/work", new { clientId, vehicleId, description = "Service " + suffix, startWithOffer = true, odo = 12100 }));
            var workId = started.GetProperty("workId").GetGuid();
            var offerId = started.GetProperty("activityId").GetGuid();

            await Ok(await server.PutAsJsonAsync($"/api/work/offer/{offerId}/productsorservices", new[]
            {
                new { id = Guid.Empty, code = "OF-" + suffix, name = "Oil filter", quantity = 1m, unit = "pcs", price = 12.5m, discount = (short?)0 },
                new { id = Guid.Empty, code = "LAB", name = "Labour", quantity = 1.5m, unit = "h", price = 40m, discount = (short?)10 },
            }));
            var offerProducts = await Json(await server.GetAsync($"/api/work/offer/{offerId}/productsorservices"));
            Assert.Equal(2, offerProducts.GetArrayLength());

            var activities = await Json(await server.GetAsync($"/api/work/{workId}/activities"));
            Assert.Equal("offer", activities.GetProperty("items")[0].GetProperty("name").GetString());
            var offerNumber = activities.GetProperty("items")[0].GetProperty("number").GetString();

            // timestamps are UTC and identical whether they come from the ORM or from SQL queries
            var workDetails = await Json(await server.GetAsync($"/api/work/{workId}"));
            var startedByOrm = workDetails.GetProperty("startedOn").GetDateTimeOffset();
            var startedBySql = activities.GetProperty("items")[0].GetProperty("startedOn").GetDateTimeOffset();
            Assert.True(Math.Abs((startedByOrm - DateTimeOffset.UtcNow).TotalMinutes) < 5, $"work started {startedByOrm}");
            Assert.True(Math.Abs((startedBySql - DateTimeOffset.UtcNow).TotalMinutes) < 5, $"offer started {startedBySql}");
            Assert.True(activities.GetProperty("current").GetProperty("priceSummary").GetProperty("totalWithVat").GetDecimal() > 0);

            // company currency: CAD by default, changed by an administrator
            Assert.Equal("CAD", activities.GetProperty("current").GetProperty("currency").GetString());
            await SetCurrency(server, "USD");
            Assert.Equal("USD", (await Json(await server.GetAsync($"/api/work/{workId}/activities"))).GetProperty("current").GetProperty("currency").GetString());
            await Ok(await server.PutAsJsonAsync("/api/options", WithCurrency(await Json(await server.GetAsync("/api/options")), "XYZ"))
                .ContinueWith(t => { Assert.False(t.Result.IsSuccessStatusCode); return new HttpResponseMessage(System.Net.HttpStatusCode.OK); }));

            // issue the estimate and send it
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/issue/{offerNumber}", new { showVehicleOnPricing = true, sendClientEmail = true, clientEmail = $"jane{suffix}@example.com" }));
            var estimateMail = api.Mailbox.LastTo($"jane{suffix}@example.com");
            Assert.NotNull(estimateMail);
            Assert.Equal("application/pdf", Assert.Single(estimateMail.Attachments).ContentType);
            Assert.Equal("Default Company", estimateMail.FromName);
            // the issued estimate keeps its currency when the company currency changes
            await SetCurrency(server, "JPY");
            Assert.Equal("USD", (await Json(await server.GetAsync($"/api/work/{workId}/activities"))).GetProperty("current").GetProperty("currency").GetString());
            await SetCurrency(server, "CAD");
            var offers = await Json(await server.GetAsync($"/api/pricings/offers/{workId}"));
            Assert.Equal("System Administrator", offers[0].GetProperty("issuedBy").GetString());

            var workList = await Json(await server.GetAsync($"/api/work/page?limit=10&searchText=REG{suffix}"));
            var row = workList.GetProperty("items").EnumerateArray().Single();
            Assert.Equal(workId, row.GetProperty("id").GetGuid());
            Assert.Equal(JsonValueKind.Object, row.GetProperty("offerIssuance").ValueKind);
            Assert.Equal(1, row.GetProperty("numberOfOffers").GetInt32());
            var bySaleable = await Json(await server.GetAsync($"/api/work/page?limit=10&saleable=OF-{suffix}"));
            Assert.Single(bySaleable.GetProperty("items").EnumerateArray());
            var byClient = await Json(await server.GetAsync($"/api/work/page?limit=10&clientiId%5Bvalue%5D={clientId}&vehicleId%5Bvalue%5D={vehicleId}&workForm=2000-01-01&workTo=2100-01-01"));
            Assert.Single(byClient.GetProperty("items").EnumerateArray());

            // accept the estimate -> repair job with the same products
            var jobId = (await Json(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/{offerNumber}/accepted", "accepted by phone"))).GetGuid();
            var jobProducts = await Json(await server.GetAsync($"/api/work/repairjob/{jobId}/productsorservices"));
            Assert.Equal(2, jobProducts.GetArrayLength());

            // assign a mechanic, change status
            var mechanicId = (await Json(await server.PostAsJsonAsync("/api/employees", new { firstName = "Mech", lastName = suffix, email = $"m{suffix}@example.com" }))).GetGuid();
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}", new { clientId, vehicleId, description = "Service", assignedTo = new[] { mechanicId } }));
            await Ok(await server.PutAsync($"/api/work/{workId}/status/InProgress", null));
            var inProgress = await Json(await server.GetAsync($"/api/work/page?limit=10&status=inprogress&searchText=REG{suffix}"));
            var inProgressRow = inProgress.GetProperty("items").EnumerateArray().Single();
            Assert.Contains(suffix, inProgressRow.GetProperty("mechanicNames").GetString());
            Assert.True(inProgressRow.GetProperty("hasRepairs").GetBoolean());

            // invoice
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/invoice/issue", new { paymentType = 2, dueDays = 0, sendClientEmail = false }));
            var work = await Json(await server.GetAsync($"/api/work/{workId}"));
            Assert.Equal("completed", work.GetProperty("status").GetString());
            var invoiceNumber = work.GetProperty("issuance").GetProperty("invoiceNumber").GetInt32();

            var issued = await Json(await server.GetAsync($"/api/work/page?limit=10&issued=on&searchText={invoiceNumber}%20REG{suffix}&invoiceFrom=2000-01-01"));
            var issuedRow = issued.GetProperty("items").EnumerateArray().Single();
            Assert.Equal(invoiceNumber, issuedRow.GetProperty("issuance").GetProperty("invoiceNumber").GetInt32());
            var overdue = await Json(await server.GetAsync($"/api/work/page?limit=10&issued=on&status=overdue&searchText=REG{suffix}"));
            Assert.Single(overdue.GetProperty("items").EnumerateArray());

            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/invoice/send", new { emailAddress = $"acme{suffix}@example.com" }));
            Assert.NotNull(api.Mailbox.LastTo($"acme{suffix}@example.com"));

            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/invoice/paid", true));
            overdue = await Json(await server.GetAsync($"/api/work/page?limit=10&issued=on&status=overdue&searchText=REG{suffix}"));
            Assert.Empty(overdue.GetProperty("items").EnumerateArray());

            // copy, quick search
            var copyId = (await Json(await server.PostAsync($"/api/work/{workId}/makecopy", null))).GetGuid();
            Assert.NotEqual(workId, copyId);
            var quick = await Json(await server.GetAsync($"/api/query/Doe{suffix}"));
            Assert.Contains(quick.EnumerateArray(), x => x.GetProperty("resourcename").GetString() == "Client");

            // the last invoice can be deleted again
            await Ok(await server.PutAsync($"/api/work/{workId}/invoice/delete", null));
            work = await Json(await server.GetAsync($"/api/work/{workId}"));
            Assert.Equal(JsonValueKind.Null, work.GetProperty("issuance").ValueKind);

            // settings
            var options = await Json(await server.GetAsync("/api/options"));
            Assert.Equal(20, options.GetProperty("pricing").GetProperty("invoice").GetProperty("vatRate").GetInt32());
        }

        private static object WithCurrency(JsonElement options, string currency)
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(options.GetRawText())!;
            node["pricing"]!["currency"] = currency;
            return node;
        }

        private static async Task SetCurrency(HttpClient server, string currency)
        {
            var options = await Json(await server.GetAsync("/api/options"));
            await Ok(await server.PutAsJsonAsync("/api/options", WithCurrency(options, currency)));
            Assert.Equal(currency, (await Json(await server.GetAsync("/api/options"))).GetProperty("pricing").GetProperty("currency").GetString());
        }

        [DbFact]
        public async Task Deleting_referenced_data_returns_a_user_error()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var storageId = (await Json(await server.PostAsJsonAsync("/api/storages", new { name = "Del", introducedAt = DateTime.UtcNow }))).GetGuid();
            await Ok(await server.PostAsJsonAsync("/api/spareparts", new { code = "D1", name = "Del part", price = 1m, quantity = 1m, storageId }));

            var request = new HttpRequestMessage(HttpMethod.Delete, "/api/storages") { Content = JsonContent.Create(new[] { storageId }) };
            var response = await server.SendAsync(request);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(error.GetProperty("isUserError").GetBoolean());
        }
    }
}
