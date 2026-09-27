using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Carmasters.Tests.Integration
{
    /// <summary>
    /// The in-memory test server has no remote address. This sets one from the X-Test-Client-Ip header
    /// (default 127.0.0.1) so per-client rate limits behave like in production.
    /// </summary>
    public class TestClientIpStartupFilter : IStartupFilter
    {
        public const string Header = "X-Test-Client-Ip";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                var header = context.Request.Headers[Header].ToString();
                context.Connection.RemoteIpAddress = IPAddress.TryParse(header, out var ip) ? ip : IPAddress.Parse("192.0.2.1");
                await nextMiddleware();
            });
            next(app);
        };
    }
}
