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
    /// <summary>Super administrators see the work of every company and can delete any work, also sent, signed and invoiced.</summary>
    [Collection("api")]
    public class AdminWorkTests
    {
        private const string Png = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

        private readonly ApiFixture api;
        public AdminWorkTests(ApiFixture api) { this.api = api; }

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

        private async Task<HttpClient> Account(HttpClient creator, string userName, string role, Guid companyId)
        {
            var created = await Json(await creator.PostAsJsonAsync("/api/admin/users", new
            {
                firstName = "Test", lastName = userName, email = $"{userName}@example.com", createAccount = true, userName, role, companyId,
            }));
            var temporary = created.GetProperty("temporaryPassword").GetString();
            var (server, _, _) = await api.LoginAsync(userName, temporary);
            const string password = "Werkstatt-Neu-2026!";
            await Ok(await server.PutAsJsonAsync("/api/profile/changepassword", new { currentPassword = temporary, newPassword = password, confirmPassword = password }));
            return (await api.LoginAsync(userName, password)).Server;
        }

        /// <summary>A work with an offer (issued and sent when an email is given, then signed online), optionally invoiced.</summary>
        private async Task<Guid> Work(HttpClient server, string lastName, string email = null, bool invoice = false)
        {
            var clientId = (await Json(await server.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "Admin", lastName, emailAddresses = email == null ? Array.Empty<string>() : new[] { email }, currentEmail = email, introducedAt = DateTime.UtcNow,
            }))).GetGuid();
            var started = await Json(await server.PostAsJsonAsync("/api/work", new { clientId, description = "d", startWithOffer = true }));
            var workId = started.GetProperty("workId").GetGuid();
            await Ok(await server.PutAsJsonAsync($"/api/work/offer/{started.GetProperty("activityId").GetGuid()}/productsorservices", new[]
            {
                new { id = Guid.Empty, code = "OC", name = "Oil change", quantity = 1m, unit = "pcs", price = 90m, discount = (short?)0 },
            }));
            await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/issue/0", new { showVehicleOnPricing = false, sendClientEmail = email != null, clientEmail = email }));
            if (email != null)
            {
                var token = Regex.Match(api.Mailbox.LastTo(email).TextBody, @"/sign/([A-Za-z0-9_-]{43})").Groups[1].Value;
                await Ok(await api.NewClient().PostAsJsonAsync($"/api/public/estimates/{token}/sign", new { serverSecret = ApiFixture.ServerSecret, name = "Admin " + lastName, signature = Png, accepted = true }));
            }
            if (invoice)
            {
                await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/estimate/0/accepted", "signed"));
                await Ok(await server.PutAsJsonAsync($"/api/work/{workId}/invoice/issue", new { paymentType = 2, dueDays = 10, sendClientEmail = true, clientEmail = email }));
            }
            return workId;
        }

        [DbFact]
        public async Task Super_administrators_see_and_delete_the_work_of_every_company()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var companyB = (await Json(await owner.PostAsJsonAsync("/api/admin/companies", new { name = "Works B " + suffix, currency = "CAD" }))).GetGuid();
            var adminB = await Account(owner, "worksb" + suffix, "admin", companyB);

            // sent, signed and invoiced in the owner's company; an issued offer in company B
            var signedWork = await Work(owner, "Signed" + suffix, $"signed{suffix}@example.com", invoice: true);
            var otherWork = await Work(adminB, "Other" + suffix);

            // only super administrators
            Assert.Equal(HttpStatusCode.Forbidden, (await adminB.GetAsync("/api/admin/works")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await adminB.DeleteAsync($"/api/admin/works/{otherWork}")).StatusCode);

            var rows = (await Json(await owner.GetAsync($"/api/admin/works?searchText={suffix}"))).GetProperty("items").EnumerateArray().ToList();
            var signedRow = rows.Single(r => r.GetProperty("id").GetGuid() == signedWork);
            Assert.Equal(1, signedRow.GetProperty("signedOffers").GetInt32());
            Assert.Equal(1, signedRow.GetProperty("sentOffers").GetInt32());
            Assert.Equal(JsonValueKind.Number, signedRow.GetProperty("invoiceNumber").ValueKind);
            Assert.Equal("completed", signedRow.GetProperty("status").GetString());
            Assert.StartsWith("RP_", signedRow.GetProperty("code").GetString());
            var otherRow = rows.Single(r => r.GetProperty("id").GetGuid() == otherWork);
            Assert.Equal("Works B " + suffix, otherRow.GetProperty("companyName").GetString());
            // the company filter
            var onlyB = (await Json(await owner.GetAsync($"/api/admin/works?companyId={companyB}"))).GetProperty("items").EnumerateArray().ToList();
            Assert.All(onlyB, r => Assert.Equal(companyB, r.GetProperty("companyId").GetGuid()));
            Assert.Contains(onlyB, r => r.GetProperty("id").GetGuid() == otherWork);

            // details and PDFs, also of another company's work
            var detail = await Json(await owner.GetAsync($"/api/admin/works/{signedWork}"));
            var documents = detail.GetProperty("documents").EnumerateArray().ToList();
            var estimate = documents.Single(x => x.GetProperty("kind").GetString() == "estimate");
            Assert.Equal("Admin Signed" + suffix, estimate.GetProperty("signedBy").GetString());
            Assert.Equal(JsonValueKind.String, estimate.GetProperty("sentOn").ValueKind);
            var invoiceDocument = documents.Single(x => x.GetProperty("kind").GetString() == "invoice");
            Assert.False(invoiceDocument.GetProperty("paid").GetBoolean());
            var otherEstimate = (await Json(await owner.GetAsync($"/api/admin/works/{otherWork}"))).GetProperty("documents").EnumerateArray().Single();
            var pdf = await owner.GetAsync($"/api/admin/works/{otherWork}/pdf/{otherEstimate.GetProperty("id").GetGuid()}");
            Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
            Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/admin/works/{otherWork}/pdf/{Guid.NewGuid()}")).StatusCode);

            // the workshop cannot delete it (sent to the client, invoiced); the super administrator can
            var refused = await owner.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/work") { Content = JsonContent.Create(new[] { signedWork }) });
            Assert.False(refused.IsSuccessStatusCode);
            var estimateId = estimate.GetProperty("id").GetGuid();
            var invoiceId = invoiceDocument.GetProperty("id").GetGuid();
            await Ok(await owner.DeleteAsync($"/api/admin/works/{signedWork}"));
            await Ok(await owner.DeleteAsync($"/api/admin/works/{otherWork}"));

            Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/admin/works/{signedWork}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/admin/works/{otherWork}")).StatusCode);
            Assert.Equal(0L, Convert.ToInt64(await TestDatabase.Scalar(api.DatabaseName, $"SELECT COUNT(*) FROM domain.pricing WHERE id IN ('{estimateId}', '{invoiceId}')")));
            Assert.Equal(0L, Convert.ToInt64(await TestDatabase.Scalar(api.DatabaseName, $"SELECT COUNT(*) FROM domain.estimate_signature WHERE estimate_id = '{estimateId}'")));
            Assert.Equal(0L, Convert.ToInt64(await TestDatabase.Scalar(api.DatabaseName, $"SELECT COUNT(*) FROM public.signature_link WHERE estimate_id = '{estimateId}'")));
            Assert.DoesNotContain((await Json(await owner.GetAsync($"/api/admin/works?searchText={suffix}"))).GetProperty("items").EnumerateArray(),
                r => r.GetProperty("id").GetGuid() == signedWork || r.GetProperty("id").GetGuid() == otherWork);

            // who deleted what is in the audit log of the company
            var entries = (await Json(await owner.GetAsync("/api/admin/audit?limit=50"))).GetProperty("items").EnumerateArray().ToList();
            var deleted = entries.First(e => e.GetProperty("action").GetString() == "work.delete" && e.GetProperty("companyId").GetGuid() == companyB);
            Assert.Contains("Works B " + suffix, deleted.GetProperty("details").GetString());
            Assert.Contains(entries, e => e.GetProperty("action").GetString() == "work.delete" && (e.GetProperty("details").GetString() ?? "").Contains("signed:"));
        }
    }
}
