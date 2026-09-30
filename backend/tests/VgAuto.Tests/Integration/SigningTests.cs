using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VgAuto.Core.Application.Signing;
using Xunit;

namespace VgAuto.Tests.Integration
{
    /// <summary>Estimate emails link to a page where the client signs without signing in; documents show the vehicle.</summary>
    [Collection("api")]
    public class SigningTests
    {
        private const string Png = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

        private readonly ApiFixture api;
        public SigningTests(ApiFixture api) { this.api = api; }

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

        private static async Task<string> Error(HttpResponseMessage response) =>
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("exceptionMessage").GetString();

        [DbFact]
        public async Task Clients_sign_estimates_online_from_the_email_link()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var email = $"sign{suffix}@example.com";
            var clientId = (await Json(await server.PostAsJsonAsync("/api/privateclients", new { firstName = "Terry", lastName = "Fox" + suffix, emailAddresses = new[] { email }, currentEmail = email, introducedAt = DateTime.UtcNow }))).GetGuid();
            var vehicleId = (await Json(await server.PostAsJsonAsync("/api/vehicles", new { licensePlate = "SG" + suffix, manufacturer = "Honda", model = "Civic", trim = "LX", year = 2019, introducedAt = DateTime.UtcNow, ownerId = clientId }))).GetGuid();
            Assert.Equal("LX", (await Json(await server.GetAsync($"/api/vehicles/{vehicleId}"))).GetProperty("trim").GetString());

            var started = await Json(await server.PostAsJsonAsync("/api/work", new { clientId, vehicleId, description = "Diagnostics", startWithOffer = true }));
            var workId = started.GetProperty("workId").GetGuid();
            var offerActivity = started.GetProperty("activityId").GetGuid();
            // lines without a product code are custom lines (a null code used to fail with an internal error)
            await Ok(await server.PutAsJsonAsync($"/api/work/offer/{offerActivity}/productsorservices", new object[]
            {
                new { id = Guid.Empty, code = (string)null, name = "Diagnostics", quantity = 1m, unit = "h", price = 95m, discount = (short?)0 },
                new { id = Guid.Empty, code = "", name = "Shop supplies", quantity = 1m, unit = (string)null, price = 12.5m, discount = (short?)0 },
            }));
            var lines = (await Json(await server.GetAsync($"/api/work/offer/{offerActivity}/productsorservices"))).EnumerateArray().ToList();
            Assert.All(lines, line => Assert.Equal("CUSTOM", line.GetProperty("code").GetString()));

            // issued and sent in one step: the email links to the signing page of this estimate
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/issue/0", new { showVehicleOnPricing = false, sendClientEmail = true, clientEmail = email }));
            var mail = api.Mailbox.LastTo(email);
            Assert.NotNull(mail);
            var match = Regex.Match(mail.TextBody, @"http://localhost:3000/sign/([A-Za-z0-9_-]{43})");
            Assert.True(match.Success, mail.TextBody);
            var token = match.Groups[1].Value;
            // the HTML version has a button to the same page
            Assert.Contains("Review and sign the estimate", mail.HtmlBody);
            Assert.Contains($"http://localhost:3000/sign/{token}", mail.HtmlBody);
            var client = api.NewClient();
            var page = $"/api/public/estimates/{token}";

            // only the Next.js server may call it, and only with a link that exists
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync($"{page}/view", new { serverSecret = "wrong" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/public/estimates/{new string('A', 43)}/view", new { serverSecret = ApiFixture.ServerSecret })).StatusCode);

            var view = await Json(await client.PostAsJsonAsync($"{page}/view", new { serverSecret = ApiFixture.ServerSecret }));
            Assert.Equal("open", view.GetProperty("status").GetString());
            Assert.Equal("2019 Honda Civic LX", view.GetProperty("vehicleTitle").GetString());
            Assert.Contains("2019 Honda Civic LX", view.GetProperty("html").GetString());
            Assert.DoesNotContain("customer-signature", view.GetProperty("html").GetString());

            // incomplete signatures are refused
            var refused = await client.PostAsJsonAsync($"{page}/sign", new { serverSecret = ApiFixture.ServerSecret, name = "Terry Fox", signature = Png, accepted = false });
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            Assert.Contains("accept", await Error(refused));
            refused = await client.PostAsJsonAsync($"{page}/sign", new { serverSecret = ApiFixture.ServerSecret, name = "Terry Fox", signature = "data:image/png;base64,SGVsbG8=", accepted = true });
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);

            var signed = await Json(await client.PostAsJsonAsync($"{page}/sign", new { serverSecret = ApiFixture.ServerSecret, name = " Terry Fox ", signature = Png, accepted = true, clientIp = "203.0.113.7" }));
            Assert.Equal("signed", signed.GetProperty("status").GetString());
            Assert.Equal("Terry Fox", signed.GetProperty("signerName").GetString());
            Assert.Contains("customer-signature", signed.GetProperty("html").GetString());
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{page}/sign", new { serverSecret = ApiFixture.ServerSecret, name = "Someone else", signature = Png, accepted = true })).StatusCode);

            var pdf = await client.PostAsJsonAsync($"{page}/pdf", new { serverSecret = ApiFixture.ServerSecret });
            Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
            Assert.EndsWith("_signed.pdf", pdf.Content.Headers.ContentDisposition?.FileNameStar ?? pdf.Content.Headers.ContentDisposition?.FileName?.Trim('"'));

            // the workshop sees who signed; the printed estimate carries the signature
            var offer = (await Json(await server.GetAsync($"/api/pricings/offers/{workId}"))).EnumerateArray().Single();
            Assert.Equal("Terry Fox", offer.GetProperty("signedBy").GetString());
            Assert.Equal(JsonValueKind.String, offer.GetProperty("signedOn").ValueKind);
            var printed = await (await server.GetAsync($"/api/pricings/offer/{offer.GetProperty("id").GetGuid()}/html")).Content.ReadAsStringAsync();
            Assert.Contains("Accepted and signed by the customer", printed);

            // the invoice shows the vehicle under its number too
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/0/accepted", "signed online"));
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/invoice/issue", new { paymentType = 2, dueDays = 10, sendClientEmail = false }));
            var invoice = await (await server.GetAsync($"/api/pricings/invoice/{workId}/html")).Content.ReadAsStringAsync();
            Assert.Contains("2019 Honda Civic LX", invoice);
        }

        [DbFact]
        public async Task Expired_links_show_the_estimate_but_cannot_sign()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var email = $"late{suffix}@example.com";
            var clientId = (await Json(await server.PostAsJsonAsync("/api/privateclients", new { firstName = "Late", lastName = "Client" + suffix, emailAddresses = new[] { email }, currentEmail = email, introducedAt = DateTime.UtcNow }))).GetGuid();
            var started = await Json(await server.PostAsJsonAsync("/api/work", new { clientId, description = "Tires", startWithOffer = true }));
            var workId = started.GetProperty("workId").GetGuid();
            await Ok(await server.PutAsJsonAsync($"/api/work/offer/{started.GetProperty("activityId").GetGuid()}/productsorservices", new[]
            {
                new { id = Guid.Empty, code = "TIRE", name = "Winter tire", quantity = 4m, unit = "pcs", price = 150m, discount = (short?)0 },
            }));
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/issue/0", new { showVehicleOnPricing = false, sendClientEmail = false }));
            var offerId = (await Json(await server.GetAsync($"/api/pricings/offers/{workId}"))).EnumerateArray().Single().GetProperty("id").GetGuid();
            // sent later, from the work page
            await Ok(await server.PutAsJsonAsync($"/api/work/estimate/send/{offerId}", new { emailAddress = email }));
            var token = Regex.Match(api.Mailbox.LastTo(email).TextBody, @"/sign/([A-Za-z0-9_-]{43})").Groups[1].Value;

            await TestDatabase.Execute(api.DatabaseName, $"UPDATE public.signature_link SET expires_at = '2020-01-01 00:00:00' WHERE token_hash = '{SignatureLinks.Hash(token)}'");

            var client = api.NewClient();
            var view = await Json(await client.PostAsJsonAsync($"/api/public/estimates/{token}/view", new { serverSecret = ApiFixture.ServerSecret }));
            Assert.Equal("expired", view.GetProperty("status").GetString());
            Assert.Null(view.GetProperty("vehicleTitle").GetString()); // no vehicle on this work
            var sign = await client.PostAsJsonAsync($"/api/public/estimates/{token}/sign", new { serverSecret = ApiFixture.ServerSecret, name = "Late Client", signature = Png, accepted = true });
            Assert.Equal((HttpStatusCode)410, sign.StatusCode);
        }
    }
}
