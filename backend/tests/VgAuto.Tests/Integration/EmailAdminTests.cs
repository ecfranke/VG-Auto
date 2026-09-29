using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using VgAuto.Tests.Unit;
using Xunit;

namespace VgAuto.Tests.Integration
{
    /// <summary>
    /// Super administrators manage all companies and the built-in email; company administrators manage their own
    /// company and its email transport (own SMTP / Microsoft 365 / Gmail, or the built-in one when it is allowed).
    /// </summary>
    [Collection("api")]
    public class EmailAdminTests
    {
        private readonly ApiFixture api;
        public EmailAdminTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private static async Task Ok(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        private static async Task<string> UserError(HttpResponseMessage response)
        {
            Assert.False(response.IsSuccessStatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.GetProperty("isUserError").GetBoolean());
            return json.GetProperty("exceptionMessage").GetString();
        }

        /// <summary>A new account (in the given company) that has changed its temporary password.</summary>
        private async Task<(Guid Id, HttpClient Server)> Account(HttpClient creator, string userName, string role, Guid? companyId = null)
        {
            var created = await Json(await creator.PostAsJsonAsync("/api/admin/users", new
            {
                firstName = "Test", lastName = userName, email = $"{userName}@example.com",
                createAccount = true, userName, role, companyId,
            }));
            var temporary = created.GetProperty("temporaryPassword").GetString();
            var (server, _, _) = await api.LoginAsync(userName, temporary);
            const string password = "Werkstatt-Neu-2026!";
            await Ok(await server.PutAsJsonAsync("/api/profile/changepassword", new { currentPassword = temporary, newPassword = password, confirmPassword = password }));
            return (created.GetProperty("employeeId").GetGuid(), (await api.LoginAsync(userName, password)).Server);
        }

        private static async Task<Guid> FirstCompany(HttpClient owner) =>
            (await Json(await owner.GetAsync("/api/admin/me"))).GetProperty("companyId").GetGuid();

        [DbFact]
        public async Task Company_administrators_manage_only_their_own_company()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var companyA = await FirstCompany(owner);
            var companyB = (await Json(await owner.PostAsJsonAsync("/api/admin/companies", new { name = "Garage B " + suffix, currency = "CAD" }))).GetGuid();
            var (adminBId, adminB) = await Account(owner, "adminb" + suffix, "admin", companyB);
            var (outsiderId, _) = await Account(owner, "outsider" + suffix, "user", companyA);

            var me = await Json(await adminB.GetAsync("/api/admin/me"));
            Assert.False(me.GetProperty("isSuperAdmin").GetBoolean());
            Assert.Equal("Garage B " + suffix, me.GetProperty("companyName").GetString());

            // employees of the own company only
            var users = (await Json(await adminB.GetAsync("/api/admin/users"))).EnumerateArray().ToList();
            Assert.Contains(users, u => u.GetProperty("employeeId").GetGuid() == adminBId);
            Assert.All(users, u => Assert.Equal(companyB, u.GetProperty("companyId").GetGuid()));
            Assert.Equal(HttpStatusCode.NotFound, (await adminB.GetAsync($"/api/admin/users/{outsiderId}")).StatusCode);
            Assert.False((await adminB.PostAsync($"/api/admin/users/{outsiderId}/disable", null)).IsSuccessStatusCode);

            // new employees go to the own company; nobody is moved to another one
            Assert.Contains("own company", await UserError(await adminB.PostAsJsonAsync("/api/admin/users", new { firstName = "X", lastName = "Y", companyId = companyA })));
            var mechanic = (await Json(await adminB.PostAsJsonAsync("/api/admin/users", new { firstName = "Mechanic", lastName = suffix }))).GetProperty("employeeId").GetGuid();
            Assert.Equal(companyB, (await Json(await adminB.GetAsync($"/api/admin/users/{mechanic}"))).GetProperty("companyId").GetGuid());
            Assert.False((await adminB.PutAsJsonAsync($"/api/admin/users/{mechanic}/company", new { companyId = companyA })).IsSuccessStatusCode);

            // the own company's settings, not the others'
            Assert.Equal(HttpStatusCode.OK, (await adminB.GetAsync($"/api/admin/companies/{companyB}/options")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await adminB.GetAsync($"/api/admin/companies/{companyA}/options")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await adminB.GetAsync($"/api/admin/companies/{companyA}/email")).StatusCode);

            // the whole system belongs to super administrators
            Assert.Equal(HttpStatusCode.Forbidden, (await adminB.GetAsync("/api/admin/companies")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await adminB.PostAsJsonAsync("/api/admin/companies", new { name = "Sneaky" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await adminB.GetAsync("/api/admin/overview")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await adminB.GetAsync("/api/admin/email/system")).StatusCode);

            // the audit log of the own company
            var audit = (await Json(await adminB.GetAsync("/api/admin/audit?limit=200"))).GetProperty("items").EnumerateArray().ToList();
            Assert.Contains(audit, e => e.GetProperty("target").GetString() == "adminb" + suffix);
            Assert.DoesNotContain(audit, e => e.GetProperty("target").GetString() == "outsider" + suffix);
            Assert.All(audit, e => Assert.Equal(companyB, e.GetProperty("companyId").GetGuid()));
            // another company's filter does not widen it
            var filtered = (await Json(await adminB.GetAsync($"/api/admin/audit?limit=200&companyId={companyA}"))).GetProperty("items").EnumerateArray().ToList();
            Assert.All(filtered, e => Assert.Equal(companyB, e.GetProperty("companyId").GetGuid()));
            var all = (await Json(await owner.GetAsync("/api/admin/audit?limit=200"))).GetProperty("items").EnumerateArray().ToList();
            Assert.Contains(all, e => e.GetProperty("target").GetString() == "outsider" + suffix);
            // super administrators filter by company
            var ofB = (await Json(await owner.GetAsync($"/api/admin/audit?limit=200&companyId={companyB}"))).GetProperty("items").EnumerateArray().ToList();
            Assert.Contains(ofB, e => e.GetProperty("target").GetString() == "adminb" + suffix);
            Assert.All(ofB, e => Assert.Equal(companyB, e.GetProperty("companyId").GetGuid()));

            var overview = await Json(await owner.GetAsync("/api/admin/overview"));
            Assert.True(overview.GetProperty("companies").GetInt32() >= 2);
            Assert.True(overview.GetProperty("noEmailCompanies").GetInt32() >= 1); // company B has no email yet
        }

        [DbFact]
        public async Task Company_email_uses_its_own_transport_or_the_allowed_built_in_one()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var company = (await Json(await owner.PostAsJsonAsync("/api/admin/companies", new { name = "Mail Garage " + suffix, currency = "CAD" }))).GetGuid();
            var (_, admin) = await Account(owner, "mailadmin" + suffix, "admin", company);
            var (_, staff) = await Account(admin, "mailstaff" + suffix, "user");

            // a new company cannot send until it is set up
            var email = await Json(await admin.GetAsync($"/api/admin/companies/{company}/email"));
            Assert.Equal("system", email.GetProperty("settings").GetProperty("kind").GetString());
            Assert.False(email.GetProperty("systemAllowed").GetBoolean());
            Assert.False(email.GetProperty("canAllowSystem").GetBoolean());
            Assert.Contains("not set up", await UserError(await admin.PostAsJsonAsync($"/api/admin/companies/{company}/email/test", new { to = "x@example.com" })));
            Assert.Contains("not enabled", await UserError(await admin.PutAsJsonAsync($"/api/admin/companies/{company}/email", new { kind = "system" })));

            // a super administrator allows the built-in email; company administrators cannot
            Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync($"/api/admin/companies/{company}/email/system", new { allowed = true })).StatusCode);
            await Ok(await owner.PutAsJsonAsync($"/api/admin/companies/{company}/email/system", new { allowed = true }));
            var sent = await Json(await admin.PostAsJsonAsync($"/api/admin/companies/{company}/email/test", new { to = $"builtin{suffix}@example.com" }));
            Assert.Equal("built-in email", sent.GetProperty("transport").GetString());
            Assert.Equal("Mail Garage " + suffix, api.Mailbox.LastTo($"builtin{suffix}@example.com").FromName);

            // the company's own SMTP account: the password is stored encrypted and never returned
            using var smtp = new FakeSmtpServer();
            await Ok(await admin.PutAsJsonAsync($"/api/admin/companies/{company}/email", new
            {
                kind = "smtp", smtpHost = "127.0.0.1", smtpPort = smtp.Port, smtpSecurity = "None",
                smtpUser = "mailer", smtpPassword = "s3cret-" + suffix, fromAddress = $"office{suffix}@garage.example", fromName = "Mail Garage",
            }));
            var saved = await admin.GetAsync($"/api/admin/companies/{company}/email");
            var raw = await saved.Content.ReadAsStringAsync();
            Assert.DoesNotContain("s3cret", raw);
            var settings = (await Json(saved)).GetProperty("settings");
            Assert.True(settings.GetProperty("hasSmtpPassword").GetBoolean());
            Assert.Equal("smtp", settings.GetProperty("kind").GetString());
            var stored = (string)await TestDatabase.Scalar(api.DatabaseName, $"SELECT smtp_password FROM tenant_config.email WHERE company_id = '{company}'");
            Assert.StartsWith("v1:", stored);
            Assert.DoesNotContain("s3cret", stored);

            sent = await Json(await admin.PostAsJsonAsync($"/api/admin/companies/{company}/email/test", new { to = $"own{suffix}@example.com" }));
            Assert.Equal("SMTP (127.0.0.1)", sent.GetProperty("transport").GetString());
            var test = Assert.Single(smtp.Received);
            Assert.Equal("|mailer|s3cret-" + suffix, test.Auth);
            Assert.Contains($"office{suffix}@garage.example", test.From);
            Assert.Null(api.Mailbox.LastTo($"own{suffix}@example.com"));

            // saving again without the password keeps it; another account needs its own password
            await Ok(await admin.PutAsJsonAsync($"/api/admin/companies/{company}/email", new
            {
                kind = "smtp", smtpHost = "127.0.0.1", smtpPort = smtp.Port, smtpSecurity = "None", smtpUser = "mailer", fromAddress = $"office{suffix}@garage.example",
            }));
            Assert.Contains("password", await UserError(await admin.PutAsJsonAsync($"/api/admin/companies/{company}/email", new
            {
                kind = "smtp", smtpHost = "127.0.0.1", smtpPort = smtp.Port, smtpUser = "someone-else",
            })));
            Assert.Contains("app password", await UserError(await admin.PutAsJsonAsync($"/api/admin/companies/{company}/email", new { kind = "gmail", fromAddress = "garage@gmail.com" })));

            // the company's invoices go out through its own account
            var clientId = (await Json(await staff.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = "Mail", lastName = "Client" + suffix, emailAddresses = new[] { $"client{suffix}@example.com" },
                currentEmail = $"client{suffix}@example.com", introducedAt = DateTime.UtcNow,
            }))).GetGuid();
            var started = await Json(await staff.PostAsJsonAsync("/api/work", new { clientId, description = "Brakes", startWithOffer = false }));
            var workId = started.GetProperty("workId").GetGuid();
            await Ok(await staff.PutAsJsonAsync($"/api/work/repairjob/{started.GetProperty("activityId").GetGuid()}/productsorservices", new[]
            {
                new { id = Guid.Empty, code = "BP", name = "Brake pads", quantity = 1m, unit = "pcs", price = 80m, discount = (short?)0 },
            }));
            await Ok(await staff.PutAsJsonAsync($"/api/work/{workId}/invoice/issue", new { paymentType = 2, dueDays = 7, sendClientEmail = true, clientEmail = $"client{suffix}@example.com" }));
            Assert.Contains(smtp.Received, m => m.To.Contains($"client{suffix}@example.com") && m.Data.Contains("Subject: Invoice RP_"));
            Assert.Null(api.Mailbox.LastTo($"client{suffix}@example.com"));

            // the super administrator sees how every company sends
            var system = await Json(await owner.GetAsync("/api/admin/email/system"));
            var row = system.GetProperty("companies").EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == company);
            Assert.Equal("SMTP (127.0.0.1)", row.GetProperty("description").GetString());
            Assert.True(row.GetProperty("systemAllowed").GetBoolean());
        }

        [DbFact]
        public async Task Super_administrators_set_the_built_in_email()
        {
            var (owner, _, _) = await AdminSession.LoginAsync(api);
            var first = await FirstCompany(owner);

            var system = await Json(await owner.GetAsync("/api/admin/email/system"));
            Assert.True(system.GetProperty("editable").GetBoolean());
            Assert.Contains(system.GetProperty("companies").EnumerateArray(),
                c => c.GetProperty("id").GetGuid() == first && c.GetProperty("systemAllowed").GetBoolean()); // existing companies keep the built-in email

            try
            {
                await Ok(await owner.PutAsJsonAsync("/api/admin/email/system", new
                {
                    kind = "graph", graphTenantId = "contoso.onmicrosoft.com", graphClientId = "00000000-1111-2222-3333-444444444444",
                    graphClientSecret = "graph-secret-value", graphSender = "noreply@contoso.example",
                }));
                var response = await owner.GetAsync("/api/admin/email/system");
                Assert.DoesNotContain("graph-secret-value", await response.Content.ReadAsStringAsync());
                system = await Json(response);
                Assert.Equal("Microsoft 365 (noreply@contoso.example)", system.GetProperty("inUse").GetString());
                Assert.True(system.GetProperty("settings").GetProperty("hasGraphClientSecret").GetBoolean());

                // switched in the administration, graphically: SMTP now
                Assert.Contains("SMTP server", await UserError(await owner.PutAsJsonAsync("/api/admin/email/system", new { kind = "smtp" })));
                await Ok(await owner.PutAsJsonAsync("/api/admin/email/system", new { kind = "smtp", smtpHost = "smtp.example.org", smtpPort = 587 }));
                Assert.Equal("SMTP (smtp.example.org)", (await Json(await owner.GetAsync("/api/admin/email/system"))).GetProperty("inUse").GetString());

                var sent = await Json(await owner.PostAsJsonAsync("/api/admin/email/system/test", new { to = "system-test@example.com" }));
                Assert.Equal("SMTP (smtp.example.org)", sent.GetProperty("transport").GetString());
                Assert.NotNull(api.Mailbox.LastTo("system-test@example.com"));
            }
            finally
            {
                // back to the server configuration for the other tests
                await Ok(await owner.PutAsJsonAsync("/api/admin/email/system", new { kind = "config" }));
            }
            Assert.StartsWith("server configuration", (await Json(await owner.GetAsync("/api/admin/email/system"))).GetProperty("inUse").GetString());
            var audit = (await Json(await owner.GetAsync("/api/admin/audit?limit=50"))).GetProperty("items").EnumerateArray();
            Assert.Contains(audit, e => e.GetProperty("action").GetString() == "email.system");
        }
    }
}
