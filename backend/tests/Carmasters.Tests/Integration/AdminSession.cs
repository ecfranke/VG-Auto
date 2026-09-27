using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Carmasters.Tests.Integration
{
    /// <summary>Logs in as the initial admin, performing the forced password change once per test run.</summary>
    public static class AdminSession
    {
        public const string NewPassword = "Werkstatt-2026!secure";
        private static readonly SemaphoreSlim gate = new(1, 1);
        private static bool passwordChanged;
        private static ApiFixture.LoginResult cached;
        private static ApiFixture cachedFor;

        public static async Task<(HttpClient Server, HttpClient Browser, ApiFixture.LoginResult Login)> LoginAsync(ApiFixture api)
        {
            await gate.WaitAsync();
            try
            {
                if (!passwordChanged)
                {
                    var (server, _, login) = await api.LoginAsync("admin", ApiFixture.AdminPassword);
                    if (login.MustChangePassword)
                    {
                        var response = await server.PutAsJsonAsync("/api/profile/changepassword",
                            new { currentPassword = ApiFixture.AdminPassword, newPassword = NewPassword, confirmPassword = NewPassword });
                        response.EnsureSuccessStatusCode();
                    }
                    passwordChanged = true;
                }
                if (cached == null || !ReferenceEquals(cachedFor, api))
                {
                    cached = (await api.LoginAsync("admin", NewPassword)).Login;
                    cachedFor = api;
                }
                return (api.WithToken(cached.Jwt), api.WithToken(cached.PublicJwt), cached);
            }
            finally
            {
                gate.Release();
            }
        }
    }
}
