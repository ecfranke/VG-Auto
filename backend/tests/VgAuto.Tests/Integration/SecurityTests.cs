using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VgAuto.Tests.Integration
{
    [Collection("api")]
    public class SecurityTests
    {
        private readonly ApiFixture api;
        public SecurityTests(ApiFixture api) { this.api = api; }

        [DbFact]
        public async Task Anonymous_requests_are_rejected()
        {
            var client = api.Anonymous();
            foreach (var url in new[] { "/api/vehicles/page?limit=5", "/api/clients/page", "/api/spareparts/page", "/api/work/page", "/api/options", "/api/vehicles/client/00000000-0000-0000-0000-000000000000" })
            {
                var response = await client.GetAsync(url);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            var post = await client.PostAsJsonAsync("/api/vehicles", new { licensePlate = "X" });
            Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);
        }

        [DbFact]
        public async Task Health_is_public()
        {
            var response = await api.Anonymous().GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [DbFact]
        public async Task Stylesheets_for_the_pdf_renderer_are_public()
        {
            foreach (var css in new[] { "/tailwind.css", "/print.css" })
            {
                Assert.Equal(HttpStatusCode.OK, (await api.Anonymous().GetAsync(css)).StatusCode);
            }
        }

        [DbFact]
        public async Task Wrong_server_secret_is_rejected()
        {
            var response = await api.Anonymous().PostAsJsonAsync("/api/users/authenticate",
                new { userName = "admin", password = ApiFixture.AdminPassword, serverSecret = "wrong" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [DbFact]
        public async Task Browser_token_cannot_call_server_side_endpoints()
        {
            var (_, browser, _) = await AdminSession.LoginAsync(api);
            Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/vehicles/page?limit=5")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await browser.GetAsync("/api/work/page?limit=5")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await browser.PostAsJsonAsync("/api/vehicles", new { licensePlate = "X" })).StatusCode);
        }

        [DbTheory]
        [InlineData("x'")]
        [InlineData("x%'/**/union/**/select/**/null,version()/**/--/**/")]
        [InlineData("'||(select/**/pg_sleep(5))||'")]
        [InlineData("x'/**/or/**/sleep(5)/**/or/**/'")]
        public async Task Search_text_injection_is_harmless(string payload)
        {
            var (server, browser, _) = await AdminSession.LoginAsync(api);
            foreach (var (client, url) in new[] {
                (browser, "/api/spareparts/page"), (browser, "/api/vehicles/page"), (browser, "/api/clients/page"),
                (server, "/api/storages/page"), (server, "/api/employees/page"), (server, "/api/work/page") })
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.GetAsync($"{url}?limit=5&searchText={WebUtility.UrlEncode(payload)}&saleable={WebUtility.UrlEncode(payload)}");
                sw.Stop();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(sw.ElapsedMilliseconds < 4000, $"{url} took {sw.ElapsedMilliseconds}ms");
            }
        }

        [DbTheory]
        [InlineData("(select 1 from pg_sleep(5))")]
        [InlineData("id; drop table domain.work")]
        [InlineData("sleep(5)")]
        public async Task Order_by_injection_is_ignored(string payload)
        {
            var (_, browser, _) = await AdminSession.LoginAsync(api);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await browser.GetAsync($"/api/spareparts/page?limit=5&orderby={WebUtility.UrlEncode(payload)}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(sw.ElapsedMilliseconds < 4000);
        }

        [DbFact]
        public async Task Filter_ids_must_be_guids()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var response = await server.GetAsync("/api/work/page?limit=5&clientiId%5Bvalue%5D=" + WebUtility.UrlEncode("x' or '1'='1"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [DbFact]
        public async Task Internal_errors_do_not_leak_details()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var response = await server.GetAsync("/api/work/00000000-0000-0000-0000-000000000000");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Null, json.GetProperty("exceptionDetails").ValueKind);
            Assert.DoesNotContain("   at ", json.GetProperty("exceptionMessage").GetString());
        }
    }

    public sealed class DbTheoryAttribute : TheoryAttribute
    {
        public DbTheoryAttribute()
        {
            if (!TestDatabase.IsConfigured) Skip = "Set VGAUTO_TEST_DB_HOST to run database integration tests.";
        }
    }
}
