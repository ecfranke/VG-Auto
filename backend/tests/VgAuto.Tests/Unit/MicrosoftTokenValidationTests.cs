using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using VgAuto.Core.Application.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace VgAuto.Tests.Unit
{
    public class MicrosoftTokenValidationTests
    {
        private static readonly RsaSecurityKey Key = new(RSA.Create(2048)) { KeyId = "k1" };
        private const string Tid = "72f988bf-86f1-41af-91ab-2d7cd011db47";

        private static OpenIdConnectConfiguration Config()
        {
            var c = new OpenIdConnectConfiguration();
            c.SigningKeys.Add(Key);
            return c;
        }

        private static AuthenticationOptions.MicrosoftOptions Options(string tenant = "common") =>
            new() { ClientId = "client-1", TenantId = tenant };

        private static string Token(string issuerTid = Tid, string tid = Tid, string audience = "client-1", string nonce = "n1",
            SecurityKey key = null, DateTime? expires = null, string email = "user@contoso.com")
        {
            var claims = new List<Claim> { new("tid", tid), new("oid", "oid-1"), new("nonce", nonce), new("name", "User") };
            if (email != null) claims.Add(new Claim("email", email));
            var handler = new JwtSecurityTokenHandler();
            var exp = expires ?? DateTime.UtcNow.AddMinutes(30);
            var token = handler.CreateJwtSecurityToken(
                issuer: $"https://login.microsoftonline.com/{issuerTid}/v2.0",
                audience: audience,
                subject: new ClaimsIdentity(claims),
                notBefore: exp.AddHours(-1),
                expires: exp,
                issuedAt: exp.AddHours(-1),
                signingCredentials: new SigningCredentials(key ?? Key, SecurityAlgorithms.RsaSha256));
            return handler.WriteToken(token);
        }

        [Fact]
        public void Valid_token_gives_tenant_scoped_subject()
        {
            var principal = MicrosoftIdentityClient.ValidateIdToken(Token(), Config(), Options(), "n1");
            var identity = MicrosoftIdentityClient.ToIdentity(principal);
            Assert.Equal($"{Tid}:oid-1", identity.Subject);
            Assert.Equal("user@contoso.com", identity.Email);
        }

        [Fact]
        public void Personal_account_tenant_is_accepted_with_common()
        {
            const string consumers = "9188040d-6c67-4c5b-b112-36a304b66dad";
            MicrosoftIdentityClient.ValidateIdToken(Token(consumers, consumers), Config(), Options(), "n1");
        }

        [Fact]
        public void Wrong_audience_is_rejected() =>
            Assert.ThrowsAny<SecurityTokenException>(() => MicrosoftIdentityClient.ValidateIdToken(Token(audience: "other-app"), Config(), Options(), "n1"));

        [Fact]
        public void Issuer_must_match_tid() =>
            Assert.ThrowsAny<SecurityTokenException>(() => MicrosoftIdentityClient.ValidateIdToken(Token(issuerTid: "11111111-1111-1111-1111-111111111111"), Config(), Options(), "n1"));

        [Fact]
        public void Wrong_nonce_is_rejected() =>
            Assert.ThrowsAny<SecurityTokenException>(() => MicrosoftIdentityClient.ValidateIdToken(Token(nonce: "other"), Config(), Options(), "n1"));

        [Fact]
        public void Foreign_signature_is_rejected() =>
            Assert.ThrowsAny<SecurityTokenException>(() => MicrosoftIdentityClient.ValidateIdToken(Token(key: new RsaSecurityKey(RSA.Create(2048)) { KeyId = "k1" }), Config(), Options(), "n1"));

        [Fact]
        public void Expired_token_is_rejected() =>
            Assert.ThrowsAny<SecurityTokenException>(() => MicrosoftIdentityClient.ValidateIdToken(Token(expires: DateTime.UtcNow.AddMinutes(-10)), Config(), Options(), "n1"));

        [Fact]
        public void Single_tenant_configuration_rejects_other_tenants()
        {
            var other = "22222222-2222-2222-2222-222222222222";
            Assert.ThrowsAny<SecurityTokenException>(() => MicrosoftIdentityClient.ValidateIdToken(Token(other, other), Config(), Options(Tid), "n1"));
            MicrosoftIdentityClient.ValidateIdToken(Token(), Config(), Options(Tid), "n1");
        }

        [Fact]
        public void Account_without_email_is_rejected()
        {
            var principal = MicrosoftIdentityClient.ValidateIdToken(Token(email: null), Config(), Options(), "n1");
            Assert.ThrowsAny<SecurityTokenException>(() => MicrosoftIdentityClient.ToIdentity(principal));
        }

        [Theory]
        [InlineData("john@example.com", "jo***@example.com")]
        [InlineData("a@b.c", "a***@b.c")]
        public void Email_is_masked(string email, string masked) => Assert.Equal(masked, AuthService.MaskEmail(email));
    }
}
