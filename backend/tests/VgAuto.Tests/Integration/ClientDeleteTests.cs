using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VgAuto.Tests.Integration
{
    [Collection("api")]
    public class ClientDeleteTests
    {
        private readonly ApiFixture api;
        public ClientDeleteTests(ApiFixture api) { this.api = api; }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private static HttpRequestMessage Delete(Guid id) =>
            new(HttpMethod.Delete, "/api/clients") { Content = JsonContent.Create(new[] { id }) };

        [DbFact]
        public async Task Unused_client_is_deleted_and_a_client_with_work_gives_a_clear_message()
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            var suffix = Guid.NewGuid().ToString("N")[..6];
            async Task<Guid> NewClient(string name) => (await Json(await server.PostAsJsonAsync("/api/privateclients", new
            {
                firstName = name, lastName = suffix, phone = "1",
                emailAddresses = new[] { $"{name}{suffix}@example.com" }, currentEmail = $"{name}{suffix}@example.com", introducedAt = DateTime.UtcNow,
            }))).GetGuid();

            // nothing refers to it (its e-mail addresses go with it)
            var unused = await NewClient("unused");
            var deleted = await server.SendAsync(Delete(unused)); Assert.True(deleted.IsSuccessStatusCode, await deleted.Content.ReadAsStringAsync());
            var gone = await server.GetAsync($"/api/privateclients/{unused}");
            Assert.True(gone.StatusCode == HttpStatusCode.NotFound || (await gone.Content.ReadAsStringAsync()).Trim() is "" or "null");

            // it has work: not deleted, and the user is told why
            var withWork = await NewClient("busy");
            await Json(await server.PostAsJsonAsync("/api/work", new { clientId = withWork, description = "d", startWithOffer = false }));
            var response = await server.SendAsync(Delete(withWork));
            Assert.False(response.IsSuccessStatusCode);
            var error = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(error.GetProperty("isUserError").GetBoolean());
            Assert.Contains("1 work order", error.GetProperty("exceptionMessage").GetString());
            Assert.Contains("busy", await (await server.GetAsync($"/api/privateclients/{withWork}")).Content.ReadAsStringAsync());
        }
    }
}
