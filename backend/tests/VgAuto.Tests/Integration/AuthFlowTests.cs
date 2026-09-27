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
    public class AuthFlowTests
    {
        private readonly ApiFixture api;
        public AuthFlowTests(ApiFixture api) { this.api = api; }

        private async Task<string> CreateUser(string userName, string email, string password)
        {
            var (server, _, _) = await AdminSession.LoginAsync(api);
            (await server.PostAsJsonAsync("/api/employees", new { firstName = "F", lastName = userName, email, userName, password })).EnsureSuccessStatusCode();
            return userName;
        }

        private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

        [DbFact]
        public async Task Password_login_requires_the_emailed_code()
        {
            var user = await CreateUser("codeuser", "codeuser@example.com", "Code-User-Pass-1");
            var client = api.Anonymous();

            var step1 = await Json(await client.PostAsJsonAsync("/api/auth/login", new { userName = user, password = "Code-User-Pass-1", serverSecret = ApiFixture.ServerSecret }));
            Assert.True(step1.GetProperty("codeRequired").GetBoolean());
            Assert.False(step1.TryGetProperty("jwt", out _));
            Assert.Equal("co***@example.com", step1.GetProperty("emailHint").GetString());
            var challengeId = step1.GetProperty("challengeId").GetGuid();
            var code = api.Mailbox.LastCode("codeuser@example.com");
            Assert.Matches("^[0-9]{6}$", code);

            var wrong = await client.PostAsJsonAsync("/api/auth/verify", new { challengeId, code = code == "000000" ? "111111" : "000000", serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

            var ok = await Json(await client.PostAsJsonAsync("/api/auth/verify", new { challengeId, code, serverSecret = ApiFixture.ServerSecret }));
            Assert.False(string.IsNullOrEmpty(ok.GetProperty("jwt").GetString()));

            // a code can be used only once
            var reused = await client.PostAsJsonAsync("/api/auth/verify", new { challengeId, code, serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
        }

        [DbFact]
        public async Task Code_is_invalidated_after_too_many_wrong_attempts()
        {
            var user = await CreateUser("guessuser", "guess@example.com", "Guess-User-Pass-1");
            var step1 = await Json(await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { userName = user, password = "Guess-User-Pass-1", serverSecret = ApiFixture.ServerSecret }));
            var challengeId = step1.GetProperty("challengeId").GetGuid();
            var code = api.Mailbox.LastCode("guess@example.com");
            for (int i = 0; i < 5; i++)
            {
                var wrongCode = ((int.Parse(code) + 1 + i) % 1_000_000).ToString("D6");
                await api.Anonymous().PostAsJsonAsync("/api/auth/verify", new { challengeId, code = wrongCode, serverSecret = ApiFixture.ServerSecret });
            }
            var right = await api.Anonymous().PostAsJsonAsync("/api/auth/verify", new { challengeId, code, serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.Unauthorized, right.StatusCode);
        }

        [DbFact]
        public async Task Code_can_be_resent_a_limited_number_of_times()
        {
            var user = await CreateUser("resenduser", "resend@example.com", "Resend-User-Pass-1");
            var step1 = await Json(await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { userName = user, password = "Resend-User-Pass-1", serverSecret = ApiFixture.ServerSecret }));
            var challengeId = step1.GetProperty("challengeId").GetGuid();
            var first = api.Mailbox.LastCode("resend@example.com");

            Assert.Equal(HttpStatusCode.OK, (await api.Anonymous().PostAsJsonAsync("/api/auth/resend", new { challengeId, serverSecret = ApiFixture.ServerSecret })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await api.Anonymous().PostAsJsonAsync("/api/auth/resend", new { challengeId, serverSecret = ApiFixture.ServerSecret })).StatusCode);
            Assert.Equal((HttpStatusCode)429, (await api.Anonymous().PostAsJsonAsync("/api/auth/resend", new { challengeId, serverSecret = ApiFixture.ServerSecret })).StatusCode);

            var latest = api.Mailbox.LastCode("resend@example.com");
            if (latest != first)
            {
                var old = await api.Anonymous().PostAsJsonAsync("/api/auth/verify", new { challengeId, code = first, serverSecret = ApiFixture.ServerSecret });
                Assert.Equal(HttpStatusCode.Unauthorized, old.StatusCode);
            }
            var ok = await api.Anonymous().PostAsJsonAsync("/api/auth/verify", new { challengeId, code = latest, serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        [DbFact]
        public async Task User_without_email_cannot_use_password_login_while_codes_are_required()
        {
            await CreateUser("noemail", null, "No-Email-Pass-12");
            var response = await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { userName = "noemail", password = "No-Email-Pass-12", serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("noEmail", (await Json(response)).GetProperty("error").GetString());
        }

        [DbFact]
        public async Task Forgot_password_does_not_reveal_accounts_and_resets_with_code()
        {
            var unknown = await Json(await api.Anonymous().PostAsJsonAsync("/api/auth/password/forgot", new { login = "does-not-exist", serverSecret = ApiFixture.ServerSecret }));
            Assert.True(unknown.GetProperty("codeRequired").GetBoolean());
            Assert.Equal(JsonValueKind.Null, unknown.GetProperty("emailHint").ValueKind);

            var user = await CreateUser("resetuser", "reset@example.com", "Reset-User-Pass-1");
            var known = await Json(await api.Anonymous().PostAsJsonAsync("/api/auth/password/forgot", new { login = "RESET@example.com", serverSecret = ApiFixture.ServerSecret }));
            Assert.Equal(JsonValueKind.Null, known.GetProperty("emailHint").ValueKind);
            var challengeId = known.GetProperty("challengeId").GetGuid();
            var code = api.Mailbox.LastCode("reset@example.com");
            Assert.NotNull(code);

            var weak = await api.Anonymous().PostAsJsonAsync("/api/auth/password/reset", new { challengeId, code, newPassword = "short", serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);

            var reset = await api.Anonymous().PostAsJsonAsync("/api/auth/password/reset", new { challengeId, code, newPassword = "Brand-New-Reset-Pw-9", serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

            var oldPassword = await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { userName = user, password = "Reset-User-Pass-1", serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
            var (_, _, login) = await api.LoginAsync(user, "Brand-New-Reset-Pw-9");
            Assert.False(string.IsNullOrEmpty(login.Jwt));

            var fakeChallenge = unknown.GetProperty("challengeId").GetGuid();
            var fake = await api.Anonymous().PostAsJsonAsync("/api/auth/password/reset", new { challengeId = fakeChallenge, code = "123456", newPassword = "Brand-New-Reset-Pw-9", serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.Unauthorized, fake.StatusCode);
        }

        [DbFact]
        public async Task Microsoft_sign_in_links_after_email_confirmation()
        {
            await CreateUser("msuser", "ms.user@contoso.com", "Ms-User-Password-1");
            var client = api.Anonymous();

            var unknown = await client.PostAsJsonAsync("/api/auth/microsoft", new { code = "t1|o1|nobody@contoso.com", codeVerifier = "v", redirectUri = "http://localhost:3000/auth/microsoft/callback", nonce = "nonce-1", serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
            Assert.Equal("noAccount", (await Json(unknown)).GetProperty("error").GetString());

            var badNonce = await client.PostAsJsonAsync("/api/auth/microsoft", new { code = "t1|o2|MS.User@contoso.com", codeVerifier = "v", redirectUri = "x", nonce = "other", serverSecret = ApiFixture.ServerSecret });
            Assert.Equal(HttpStatusCode.Unauthorized, badNonce.StatusCode);

            // first sign in: email matches (case insensitive), the code goes to the user's stored address
            var first = await Json(await client.PostAsJsonAsync("/api/auth/microsoft", new { code = "t1|o2|MS.User@contoso.com", codeVerifier = "v", redirectUri = "x", nonce = "nonce-1", serverSecret = ApiFixture.ServerSecret }));
            Assert.True(first.GetProperty("codeRequired").GetBoolean());
            var code = api.Mailbox.LastCode("ms.user@contoso.com");
            var linked = await Json(await client.PostAsJsonAsync("/api/auth/verify", new { challengeId = first.GetProperty("challengeId").GetGuid(), code, serverSecret = ApiFixture.ServerSecret }));
            var jwt = linked.GetProperty("jwt").GetString();

            // second sign in with the same Microsoft account: no code needed
            var second = await Json(await client.PostAsJsonAsync("/api/auth/microsoft", new { code = "t1|o2|ms.user@contoso.com", codeVerifier = "v", redirectUri = "x", nonce = "nonce-1", serverSecret = ApiFixture.ServerSecret }));
            Assert.False(string.IsNullOrEmpty(second.GetProperty("jwt").GetString()));

            // a different Microsoft account (e.g. another tenant) with the same email must prove the mailbox again
            var other = await Json(await client.PostAsJsonAsync("/api/auth/microsoft", new { code = "evil-tenant|o9|ms.user@contoso.com", codeVerifier = "v", redirectUri = "x", nonce = "nonce-1", serverSecret = ApiFixture.ServerSecret }));
            Assert.True(other.GetProperty("codeRequired").GetBoolean());

            // the user sees and can remove the link
            var profile = api.WithToken(jwt);
            var logins = await Json(await profile.GetAsync("/api/profile/externallogins"));
            Assert.Equal("microsoft", logins[0].GetProperty("provider").GetString());
            (await profile.DeleteAsync("/api/profile/externallogins/microsoft")).EnsureSuccessStatusCode();
            var afterUnlink = await Json(await client.PostAsJsonAsync("/api/auth/microsoft", new { code = "t1|o2|ms.user@contoso.com", codeVerifier = "v", redirectUri = "x", nonce = "nonce-1", serverSecret = ApiFixture.ServerSecret }));
            Assert.True(afterUnlink.GetProperty("codeRequired").GetBoolean());
        }

        [DbFact]
        public async Task Login_endpoints_require_the_server_secret()
        {
            foreach (var url in new[] { "/api/auth/login", "/api/auth/verify", "/api/auth/password/forgot", "/api/auth/password/reset", "/api/auth/microsoft", "/api/auth/resend" })
            {
                var response = await api.Anonymous().PostAsJsonAsync(url, new { userName = "admin", login = "admin", challengeId = Guid.NewGuid(), serverSecret = "nope" });
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [DbFact]
        public async Task Providers_are_public()
        {
            var providers = await Json(await api.Anonymous().GetAsync("/api/auth/providers"));
            Assert.True(providers.GetProperty("microsoft").GetProperty("enabled").GetBoolean());
            Assert.Equal("test-client-id", providers.GetProperty("microsoft").GetProperty("clientId").GetString());
            Assert.DoesNotContain("test-client-secret", providers.GetRawText());
        }
    }
}
