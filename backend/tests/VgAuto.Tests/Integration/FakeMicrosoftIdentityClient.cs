using System;
using System.Threading.Tasks;
using VgAuto.Core.Application.Authentication;

namespace VgAuto.Tests.Integration
{
    /// <summary>Test double: the authorization "code" is "tenantId|objectId|email".</summary>
    public class FakeMicrosoftIdentityClient : IMicrosoftIdentityClient
    {
        public Task<ExternalIdentity> RedeemCodeAsync(string code, string codeVerifier, string redirectUri, string nonce)
        {
            var parts = code.Split('|');
            if (parts.Length != 3 || nonce != "nonce-1") throw new UnauthorizedAccessException("bad code");
            return Task.FromResult(new ExternalIdentity(AuthService.MicrosoftProvider, $"{parts[0]}:{parts[1]}", parts[2], "Test User"));
        }
    }
}
