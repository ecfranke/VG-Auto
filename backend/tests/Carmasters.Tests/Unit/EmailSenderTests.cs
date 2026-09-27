using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Carmasters.Core.Application.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Carmasters.Tests.Unit
{
    public class EmailSenderTests
    {
        private static EmailMessage Sample()
        {
            var m = new EmailMessage("client@example.com", "Invoice 42", "Please find your invoice attached.")
            {
                FromName = "Best Garage",
                ReplyTo = "office@garage.example",
                FallbackFromAddress = "office@garage.example",
            };
            m.Attachments.Add(new EmailAttachment("invoice-42.pdf", "application/pdf", Encoding.ASCII.GetBytes("%PDF-1.4 test")));
            return m;
        }

        [Fact]
        public async Task Smtp_sender_delivers_message_with_attachment()
        {
            using var server = new FakeSmtpServer();
            var options = Options.Create(new EmailOptions
            {
                FromAddress = "noreply@garage.example",
                Smtp = { Host = "127.0.0.1", Port = server.Port, Security = SmtpSecurity.None, User = "mailer", Password = "secret" }
            });
            var sender = new SmtpEmailSender(options, NullLogger<SmtpEmailSender>.Instance);

            await sender.SendAsync(Sample());

            var received = Assert.Single(server.Received);
            Assert.Contains("noreply@garage.example", received.From);
            Assert.Contains("client@example.com", received.To);
            Assert.Equal("|mailer|secret", received.Auth);
            Assert.Contains("Subject: Invoice 42", received.Data);
            Assert.Contains("Reply-To: office@garage.example", received.Data);
            Assert.Contains("invoice-42.pdf", received.Data);
            Assert.Contains("Best Garage", received.Data);
        }

        [Fact]
        public async Task Smtp_sender_uses_workshop_address_when_no_sender_is_configured()
        {
            using var server = new FakeSmtpServer();
            var options = Options.Create(new EmailOptions { Smtp = { Host = "127.0.0.1", Port = server.Port, Security = SmtpSecurity.None } });
            await new SmtpEmailSender(options, NullLogger<SmtpEmailSender>.Instance).SendAsync(Sample());
            Assert.Contains("office@garage.example", Assert.Single(server.Received).From);
        }

        [Fact]
        public async Task Smtp_failure_is_reported_as_delivery_exception()
        {
            var options = Options.Create(new EmailOptions { FromAddress = "a@b.c", Smtp = { Host = "127.0.0.1", Port = 1, Security = SmtpSecurity.None, TimeoutSeconds = 2 } });
            await Assert.ThrowsAsync<EmailDeliveryException>(() => new SmtpEmailSender(options, NullLogger<SmtpEmailSender>.Instance).SendAsync(Sample()));
        }

        private class GraphHandler : HttpMessageHandler
        {
            public List<(HttpRequestMessage Request, string Body)> Requests { get; } = new();
            public Queue<HttpStatusCode> SendMailResults { get; } = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                Requests.Add((request, body));
                if (request.RequestUri.AbsolutePath.EndsWith("/oauth2/v2.0/token"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent($"{{\"access_token\":\"token-{Requests.Count}\",\"expires_in\":3600}}", Encoding.UTF8, "application/json")
                    };
                }
                var status = SendMailResults.Count > 0 ? SendMailResults.Dequeue() : HttpStatusCode.Accepted;
                var response = new HttpResponseMessage(status) { Content = new StringContent(status == HttpStatusCode.Accepted ? "" : "{\"error\":{\"message\":\"nope\"}}") };
                if ((int)status == 429) response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(10));
                return response;
            }
        }

        private class Factory : IHttpClientFactory
        {
            private readonly HttpMessageHandler handler;
            public Factory(HttpMessageHandler handler) { this.handler = handler; }
            public HttpClient CreateClient(string name) => new HttpClient(handler, false);
        }

        private static (GraphEmailSender Sender, GraphHandler Handler) Graph()
        {
            var handler = new GraphHandler();
            var options = Options.Create(new EmailOptions
            {
                Provider = EmailProvider.Graph,
                Graph = { TenantId = "contoso.onmicrosoft.com", ClientId = "app-id", ClientSecret = "app-secret", Sender = "workshop@contoso.com" }
            });
            return (new GraphEmailSender(options, new Factory(handler), new GraphTokenCache(), NullLogger<GraphEmailSender>.Instance), handler);
        }

        [Fact]
        public async Task Graph_sender_gets_app_token_and_posts_sendMail()
        {
            var (sender, handler) = Graph();
            await sender.SendAsync(Sample());

            Assert.Equal(2, handler.Requests.Count);
            var token = handler.Requests[0];
            Assert.Equal("/contoso.onmicrosoft.com/oauth2/v2.0/token", token.Request.RequestUri.AbsolutePath);
            Assert.Contains("grant_type=client_credentials", token.Body);
            Assert.Contains("scope=https%3A%2F%2Fgraph.microsoft.com%2F.default", token.Body);

            var send = handler.Requests[1];
            Assert.Equal("https://graph.microsoft.com/v1.0/users/workshop%40contoso.com/sendMail", send.Request.RequestUri.AbsoluteUri);
            Assert.Equal("Bearer", send.Request.Headers.Authorization.Scheme);
            using var json = JsonDocument.Parse(send.Body);
            var message = json.RootElement.GetProperty("message");
            Assert.Equal("Invoice 42", message.GetProperty("subject").GetString());
            Assert.Equal("client@example.com", message.GetProperty("toRecipients")[0].GetProperty("emailAddress").GetProperty("address").GetString());
            Assert.Equal("office@garage.example", message.GetProperty("replyTo")[0].GetProperty("emailAddress").GetProperty("address").GetString());
            var attachment = message.GetProperty("attachments")[0];
            Assert.Equal("#microsoft.graph.fileAttachment", attachment.GetProperty("@odata.type").GetString());
            Assert.Equal("%PDF-1.4 test", Encoding.ASCII.GetString(Convert.FromBase64String(attachment.GetProperty("contentBytes").GetString())));
        }

        [Fact]
        public async Task Graph_sender_reuses_token_and_retries_throttling()
        {
            var (sender, handler) = Graph();
            handler.SendMailResults.Enqueue((HttpStatusCode)429);
            await sender.SendAsync(Sample());
            await sender.SendAsync(Sample());
            Assert.Single(handler.Requests, r => r.Request.RequestUri.AbsolutePath.EndsWith("/token"));
            Assert.Equal(3, handler.Requests.Count(r => r.Request.RequestUri.AbsolutePath.EndsWith("/sendMail")));
        }

        [Fact]
        public async Task Graph_error_is_reported()
        {
            var (sender, handler) = Graph();
            handler.SendMailResults.Enqueue(HttpStatusCode.Forbidden);
            var ex = await Assert.ThrowsAsync<EmailDeliveryException>(() => sender.SendAsync(Sample()));
            Assert.Contains("nope", ex.Message);
        }

        [Fact]
        public void Provider_is_selected_from_configuration_and_legacy_smtp_section_still_works()
        {
            IServiceProvider Build(Dictionary<string, string> values) =>
                new ServiceCollection().AddLogging()
                    .AddEmail(new ConfigurationBuilder().AddInMemoryCollection(values).Build())
                    .BuildServiceProvider();

            var graph = Build(new() { ["Email:Provider"] = "Graph" }).GetRequiredService<IEmailSender>();
            Assert.Equal("Graph", graph.Name);

            var legacy = Build(new() { ["SmtpOptions:Host"] = "mail.example", ["SmtpOptions:Port"] = "465" });
            Assert.Equal("Smtp", legacy.GetRequiredService<IEmailSender>().Name);
            var options = legacy.GetRequiredService<IOptions<EmailOptions>>().Value;
            Assert.Equal("mail.example", options.Smtp.Host);
            Assert.Equal(465, options.Smtp.Port);
        }
    }
}
