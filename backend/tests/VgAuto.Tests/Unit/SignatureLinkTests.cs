using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using VgAuto.Core.Application.Signing;
using Xunit;

namespace VgAuto.Tests.Unit
{
    public class SignatureLinkTests
    {
        private static SignatureLinks Links(Dictionary<string, string> settings, string appOrigin = null)
        {
            var context = new DefaultHttpContext();
            if (appOrigin != null) context.Request.Headers[SignatureLinks.AppOriginHeader] = appOrigin;
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            return new SignatureLinks(null, configuration, new HttpContextAccessor { HttpContext = context });
        }

        [Fact]
        public void The_app_url_comes_from_the_configuration_then_the_cors_origin_then_the_web_server()
        {
            Assert.Equal("https://shop.example.com", Links(new() { ["App:Url"] = "https://shop.example.com/", ["Cors:AllowedOrigins:0"] = "https://other.example.com" }).AppUrl);
            Assert.Equal("https://other.example.com", Links(new() { ["Cors:AllowedOrigins:0"] = "https://other.example.com" }, "https://web.example.com").AppUrl);
            Assert.Equal("https://web.example.com", Links(new(), "https://web.example.com").AppUrl);
            Assert.Null(Links(new() { ["Cors:AllowedOrigins:0"] = "*" }).AppUrl);
        }

        [Theory]
        [InlineData("https://shop.example.com/sign/x?y=1", "https://shop.example.com")]
        [InlineData("http://workshop-pc:3000", "http://workshop-pc:3000")]
        [InlineData("javascript:alert(1)", null)]
        [InlineData("shop.example.com", null)]
        [InlineData("", null)]
        public void Only_http_origins_are_taken_from_the_web_server(string value, string origin)
        {
            Assert.Equal(origin, SignatureLinks.Origin(value));
        }

        [Fact]
        public void Tokens_are_random_and_only_their_hash_is_kept()
        {
            var token = SignatureLinks.NewToken();
            Assert.Matches("^[A-Za-z0-9_-]{43}$", token);
            Assert.NotEqual(token, SignatureLinks.NewToken());
            Assert.Equal(64, SignatureLinks.Hash(token).Length);
        }
    }
}
