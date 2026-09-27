using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Carmasters.Core.Application.Services;
using Carmasters.Core.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Carmasters.Tests.Integration
{
    /// <summary>
    /// Starts the API against a fresh, migrated database.
    /// Configure with environment variables:
    ///   CARCARE_TEST_DB_PROVIDER = PostgreSql | MySql
    ///   CARCARE_TEST_DB_HOST, CARCARE_TEST_DB_PORT, CARCARE_TEST_DB_USER, CARCARE_TEST_DB_PASSWORD
    /// Integration tests are skipped when CARCARE_TEST_DB_HOST is not set.
    /// </summary>
    public class ApiFixture : IAsyncLifetime
    {
        public const string AdminPassword = "Initial-Admin-Pw-1";
        public const string ServerSecret = "test-server-secret-0123456789abcdef";

        public WebApplicationFactory<Program> Factory { get; private set; }
        public FakeMailbox Mailbox { get; } = new FakeMailbox();
        public string DatabaseName { get; private set; }
        public bool Enabled => TestDatabase.IsConfigured;

        private readonly Dictionary<string, string> previousEnvironment = new();

        public async Task InitializeAsync()
        {
            if (!Enabled) return;

            DatabaseName = "carcare_test_" + Guid.NewGuid().ToString("N")[..10];
            var settings = new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["DbOptions__Provider"] = TestDatabase.Provider,
                ["DbOptions__Host"] = TestDatabase.Host,
                ["DbOptions__Port"] = TestDatabase.Port.ToString(),
                ["DbOptions__UserId"] = TestDatabase.User,
                ["DbOptions__Password"] = TestDatabase.Password,
                ["DbOptions__Name"] = DatabaseName,
                ["DbOptions__MultiTenancy__Enabled"] = "false",
                ["JwtOptions__Secret"] = "integration-test-jwt-secret-0123456789abcdef0123456789",
                ["JwtOptions__ConsumerSecret"] = ServerSecret,
                ["JwtOptions__SessionTimeout"] = "01:00:00",
                ["Cors__Mode"] = "restricted",
                ["Cors__AllowedOrigins__0"] = "http://localhost:3000",
                ["DefaultAdmin__UserName"] = "admin",
                ["DefaultAdmin__Password"] = AdminPassword,
                ["DefaultAdmin__Email"] = "admin@example.com",
                ["PdfDirectory"] = System.IO.Path.GetTempPath(),
                ["Authentication__Microsoft__Enabled"] = "true",
                ["Authentication__Microsoft__ClientId"] = "test-client-id",
                ["Authentication__Microsoft__ClientSecret"] = "test-client-secret",
            };
            foreach (var (key, value) in settings)
            {
                previousEnvironment[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }

            var migrationConfig = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            var result = DatabaseMigrator.Run(migrationConfig, logToConsole: false);
            if (!result.Successful) throw new Exception("Migration failed", result.Error);

            Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IPdfGenerator>();
                    services.AddScoped<IPdfGenerator, FakePdfGenerator>();
                    Mailbox.Register(services);
                    services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, TestClientIpStartupFilter>();
                    services.RemoveAll<Carmasters.Core.Application.Authentication.IMicrosoftIdentityClient>();
                    services.AddSingleton<Carmasters.Core.Application.Authentication.IMicrosoftIdentityClient, FakeMicrosoftIdentityClient>();
                });
            });
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (!Enabled) return;
            Factory?.Dispose();
            await TestDatabase.Drop(DatabaseName);
            foreach (var (key, value) in previousEnvironment) Environment.SetEnvironmentVariable(key, value);
        }

        public HttpClient Anonymous() => NewClient();

        private int clientCounter;

        /// <summary>Client with its own (simulated) IP address.</summary>
        public HttpClient NewClient()
        {
            var client = Factory.CreateClient();
            var n = System.Threading.Interlocked.Increment(ref clientCounter);
            client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.Header, $"10.{n / 65536 % 256}.{n / 256 % 256}.{n % 256}");
            return client;
        }

        /// <summary>Password login including the emailed code step.</summary>
        public async Task<(HttpClient Server, HttpClient Browser, LoginResult Login)> LoginAsync(string userName, string password)
        {
            var client = NewClient();
            var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password, serverSecret = ServerSecret });
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            if (json.TryGetProperty("codeRequired", out var required) && required.GetBoolean())
            {
                var challengeId = json.GetProperty("challengeId").GetGuid();
                var verify = await client.PostAsJsonAsync("/api/auth/verify", new { challengeId, code = Mailbox.LastCode(), serverSecret = ServerSecret });
                verify.EnsureSuccessStatusCode();
                json = await verify.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            }
            var login = System.Text.Json.JsonSerializer.Deserialize<LoginResult>(json, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            return (WithToken(login.Jwt), WithToken(login.PublicJwt), login);
        }

        public HttpClient WithToken(string jwt)
        {
            var client = NewClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            return client;
        }

        public record LoginResult(string Jwt, string PublicJwt, int Timeout, bool MustChangePassword);
    }

    public class FakePdfGenerator : IPdfGenerator
    {
        public Task<byte[]> Generate(Pricing pricing) => Task.FromResult(System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 fake"));
        public IPricingHtmlGenerator GetBodyGenerator() => throw new NotSupportedException();
        public IPricingHtmlGenerator GetFooterGenerator() => throw new NotSupportedException();
    }

    [CollectionDefinition("api")]
    public class ApiCollection : ICollectionFixture<ApiFixture> { }

    /// <summary>Fact that is skipped when no test database is configured.</summary>
    public sealed class DbFactAttribute : FactAttribute
    {
        public DbFactAttribute()
        {
            if (!TestDatabase.IsConfigured) Skip = "Set CARCARE_TEST_DB_HOST to run database integration tests.";
        }
    }
}
