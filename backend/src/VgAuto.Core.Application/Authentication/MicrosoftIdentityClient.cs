using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace VgAuto.Core.Application.Authentication
{
    /// <summary>
    /// Redeems an authorization code at the Microsoft identity platform (v2.0 endpoint) and validates the id_token:
    /// signature (published keys), audience, lifetime, nonce and the multi-tenant issuer (issuer must match the tid claim).
    /// </summary>
    public class MicrosoftIdentityClient : IMicrosoftIdentityClient
    {
        public const string HttpClientName = "microsoft-identity";

        private readonly AuthenticationOptions.MicrosoftOptions options;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfigurationManager<OpenIdConnectConfiguration> metadata;
        private readonly ILogger<MicrosoftIdentityClient> logger;

        public MicrosoftIdentityClient(IOptions<AuthenticationOptions> options, IHttpClientFactory httpClientFactory,
            MicrosoftMetadata metadata, ILogger<MicrosoftIdentityClient> logger)
        {
            this.options = options.Value.Microsoft;
            this.httpClientFactory = httpClientFactory;
            this.metadata = metadata.Manager;
            this.logger = logger;
        }

        public async Task<ExternalIdentity> RedeemCodeAsync(string code, string codeVerifier, string redirectUri, string nonce)
        {
            if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
                throw new InvalidOperationException("Authentication:Microsoft ClientId/ClientSecret are not configured.");
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Missing authorization code.");

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsync($"{options.Instance.TrimEnd('/')}/{options.TenantId}/oauth2/v2.0/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = options.ClientId,
                    ["client_secret"] = options.ClientSecret,
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["code_verifier"] = codeVerifier ?? string.Empty,
                    ["redirect_uri"] = redirectUri,
                    ["scope"] = "openid profile email",
                }));
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Microsoft token endpoint returned {status}: {body}", (int)response.StatusCode, body.Length > 500 ? body[..500] : body);
                throw new SecurityTokenException("Authorization code could not be redeemed.");
            }

            using var doc = JsonDocument.Parse(body);
            var idToken = doc.RootElement.GetProperty("id_token").GetString();
            var configuration = await metadata.GetConfigurationAsync(CancellationToken.None);
            var principal = ValidateIdToken(idToken, configuration, options, nonce);
            return ToIdentity(principal);
        }

        /// <summary>Validates an id_token issued by the Microsoft identity platform. Public for tests.</summary>
        public static ClaimsPrincipal ValidateIdToken(string idToken, OpenIdConnectConfiguration configuration,
            AuthenticationOptions.MicrosoftOptions options, string expectedNonce)
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var parameters = new TokenValidationParameters
            {
                ValidAudience = options.ClientId,
                IssuerSigningKeys = configuration.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                RequireSignedTokens = true,
                // multi-tenant: the issuer contains the tenant id of the signed in user
                IssuerValidator = (issuer, token, _) =>
                {
                    var tid = ((JwtSecurityToken)token).Claims.FirstOrDefault(c => c.Type == "tid")?.Value;
                    if (string.IsNullOrEmpty(tid)) throw new SecurityTokenInvalidIssuerException("Missing tid claim.");
                    var expected = $"{options.Instance.TrimEnd('/')}/{tid}/v2.0";
                    if (!string.Equals(issuer, expected, StringComparison.OrdinalIgnoreCase))
                        throw new SecurityTokenInvalidIssuerException($"Unexpected issuer {issuer}.");
                    if (Guid.TryParse(options.TenantId, out var onlyTenant) && !string.Equals(tid, onlyTenant.ToString(), StringComparison.OrdinalIgnoreCase))
                        throw new SecurityTokenInvalidIssuerException("Sign in from this Microsoft tenant is not allowed.");
                    return issuer;
                },
            };

            var principal = handler.ValidateToken(idToken, parameters, out _);
            var nonce = principal.FindFirst("nonce")?.Value;
            if (string.IsNullOrEmpty(expectedNonce) || !string.Equals(nonce, expectedNonce, StringComparison.Ordinal))
                throw new SecurityTokenValidationException("Invalid nonce.");
            return principal;
        }

        public static ExternalIdentity ToIdentity(ClaimsPrincipal principal)
        {
            var tid = principal.FindFirst("tid")?.Value;
            var oid = principal.FindFirst("oid")?.Value ?? principal.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(tid) || string.IsNullOrEmpty(oid)) throw new SecurityTokenValidationException("Token has no tid/oid.");
            var email = principal.FindFirst("email")?.Value ?? principal.FindFirst("preferred_username")?.Value;
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                throw new SecurityTokenValidationException("The Microsoft account has no email address.");
            return new ExternalIdentity(AuthService.MicrosoftProvider, $"{tid}:{oid}", email.Trim(), principal.FindFirst("name")?.Value);
        }
    }

    /// <summary>Cached OpenID Connect metadata (signing keys) of the Microsoft identity platform.</summary>
    public class MicrosoftMetadata
    {
        public MicrosoftMetadata(IOptions<AuthenticationOptions> options, IHttpClientFactory httpClientFactory)
        {
            var o = options.Value.Microsoft;
            Manager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{o.AuthorityUrl}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(httpClientFactory.CreateClient(MicrosoftIdentityClient.HttpClientName)) { RequireHttps = true });
        }

        public IConfigurationManager<OpenIdConnectConfiguration> Manager { get; }
    }
}
