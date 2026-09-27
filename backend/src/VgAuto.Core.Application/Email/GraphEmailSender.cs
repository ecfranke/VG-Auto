using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VgAuto.Core.Application.Email
{
    /// <summary>
    /// Sends mail through Microsoft Graph (POST /users/{sender}/sendMail) using the OAuth2 client credentials flow.
    /// </summary>
    public class GraphEmailSender : IEmailSender
    {
        public const string HttpClientName = "graph-mail";
        private const int MaxInlineAttachmentBytes = 3 * 1024 * 1024; // Graph limit for sendMail with inline attachments

        private readonly EmailOptions options;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly GraphTokenCache tokenCache;
        private readonly ILogger<GraphEmailSender> logger;

        public GraphEmailSender(IOptions<EmailOptions> options, IHttpClientFactory httpClientFactory, GraphTokenCache tokenCache, ILogger<GraphEmailSender> logger)
        {
            this.options = options.Value;
            this.httpClientFactory = httpClientFactory;
            this.tokenCache = tokenCache;
            this.logger = logger;
        }

        public string Name => "Graph";

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            var graph = options.Graph;
            if (string.IsNullOrWhiteSpace(graph.TenantId) || string.IsNullOrWhiteSpace(graph.ClientId) ||
                string.IsNullOrWhiteSpace(graph.ClientSecret) || string.IsNullOrWhiteSpace(graph.Sender))
            {
                throw new EmailDeliveryException("Email:Graph (TenantId, ClientId, ClientSecret, Sender) is not fully configured.");
            }
            if (message.Attachments.Sum(a => (long)a.Content.Length) > MaxInlineAttachmentBytes)
            {
                throw new EmailDeliveryException("Attachments are too large for Microsoft Graph sendMail (3 MB).");
            }

            var payload = BuildPayload(message);
            var client = httpClientFactory.CreateClient(HttpClientName);
            var url = $"{graph.GraphEndpoint.TrimEnd('/')}/users/{Uri.EscapeDataString(graph.Sender)}/sendMail";

            for (var attempt = 1; ; attempt++)
            {
                var token = await tokenCache.GetTokenAsync(client, graph, cancellationToken);
                using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var response = await client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Email sent via Microsoft Graph as {sender}: {subject}", graph.Sender, message.Subject);
                    return;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 1)
                {
                    tokenCache.Invalidate();
                    continue;
                }
                if ((response.StatusCode == (HttpStatusCode)429 || (int)response.StatusCode >= 500) && attempt < 3)
                {
                    var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2 * attempt);
                    await Task.Delay(delay > TimeSpan.FromSeconds(30) ? TimeSpan.FromSeconds(30) : delay, cancellationToken);
                    continue;
                }

                logger.LogError("Microsoft Graph sendMail failed with {status}: {body}", (int)response.StatusCode, body);
                throw new EmailDeliveryException($"Sending email through Microsoft Graph failed ({(int)response.StatusCode}): {GraphError(body)}");
            }
        }

        internal object BuildPayload(EmailMessage message)
        {
            var from = !string.IsNullOrWhiteSpace(options.FromAddress) ? options.FromAddress : options.Graph.Sender;
            var replyTo = !string.IsNullOrWhiteSpace(message.ReplyTo) && !string.Equals(message.ReplyTo, from, StringComparison.OrdinalIgnoreCase)
                ? new[] { new { emailAddress = new { address = message.ReplyTo } } }
                : Array.Empty<object>();

            return new
            {
                message = new
                {
                    subject = message.Subject,
                    body = string.IsNullOrEmpty(message.HtmlBody)
                        ? new { contentType = "Text", content = message.TextBody }
                        : new { contentType = "HTML", content = message.HtmlBody },
                    from = new { emailAddress = new { address = from, name = message.FromName ?? options.FromName } },
                    toRecipients = new[] { new { emailAddress = new { address = message.To } } },
                    replyTo,
                    attachments = message.Attachments.Select(a => new Dictionary<string, object>
                    {
                        ["@odata.type"] = "#microsoft.graph.fileAttachment",
                        ["name"] = a.FileName,
                        ["contentType"] = a.ContentType,
                        ["contentBytes"] = Convert.ToBase64String(a.Content),
                    }).ToArray(),
                },
                saveToSentItems = options.Graph.SaveToSentItems
            };
        }

        private static string GraphError(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var message))
                    return message.GetString();
            }
            catch (JsonException) { }
            return body.Length > 200 ? body[..200] : body;
        }
    }

    /// <summary>Caches the app-only access token until shortly before it expires.</summary>
    public class GraphTokenCache
    {
        private readonly SemaphoreSlim gate = new(1, 1);
        private string token;
        private DateTimeOffset expiresAt;

        public void Invalidate() => token = null;

        public async Task<string> GetTokenAsync(HttpClient client, EmailOptions.GraphSettings graph, CancellationToken cancellationToken)
        {
            if (token != null && DateTimeOffset.UtcNow < expiresAt) return token;
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (token != null && DateTimeOffset.UtcNow < expiresAt) return token;

                var tokenUrl = $"{graph.Authority.TrimEnd('/')}/{Uri.EscapeDataString(graph.TenantId)}/oauth2/v2.0/token";
                using var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = graph.ClientId,
                    ["client_secret"] = graph.ClientSecret,
                    ["scope"] = "https://graph.microsoft.com/.default",
                    ["grant_type"] = "client_credentials",
                }), cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw new EmailDeliveryException($"Could not get a Microsoft Graph token ({(int)response.StatusCode}). Check Email:Graph TenantId/ClientId/ClientSecret.");
                }
                using var doc = JsonDocument.Parse(body);
                token = doc.RootElement.GetProperty("access_token").GetString();
                var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;
                expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn - 300));
                return token;
            }
            finally
            {
                gate.Release();
            }
        }
    }
}
