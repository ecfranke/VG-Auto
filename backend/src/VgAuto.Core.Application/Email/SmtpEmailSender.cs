using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace VgAuto.Core.Application.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions options;
        private readonly ILogger<SmtpEmailSender> logger;

        public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
        {
            this.options = options.Value;
            this.logger = logger;
        }

        public string Name => "Smtp";

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            var smtp = options.Smtp;
            if (string.IsNullOrWhiteSpace(smtp.Host)) throw new EmailDeliveryException("Email:Smtp:Host is not configured.");

            var mime = BuildMime(message);

            using var client = new MailKit.Net.Smtp.SmtpClient { Timeout = smtp.TimeoutSeconds * 1000 };
            if (smtp.AllowInvalidCertificate)
            {
                client.ServerCertificateValidationCallback = (_, _, _, _) => true;
            }

            try
            {
                await client.ConnectAsync(smtp.Host, smtp.Port, ToSocketOptions(smtp), cancellationToken);
                if (!string.IsNullOrWhiteSpace(smtp.User))
                {
                    await client.AuthenticateAsync(smtp.User, smtp.Password ?? string.Empty, cancellationToken);
                }
                await client.SendAsync(mime, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "SMTP delivery to {host}:{port} failed", smtp.Host, smtp.Port);
                throw new EmailDeliveryException($"Sending email failed: {ex.Message}", ex);
            }
            logger.LogInformation("Email sent via SMTP {host}:{port}: {subject}", smtp.Host, smtp.Port, message.Subject);
        }

        internal MimeMessage BuildMime(EmailMessage message)
        {
            var fromAddress = !string.IsNullOrWhiteSpace(options.FromAddress) ? options.FromAddress : message.FallbackFromAddress;
            if (string.IsNullOrWhiteSpace(fromAddress))
                throw new EmailDeliveryException("No sender address. Configure Email:FromAddress.");

            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(message.FromName ?? options.FromName ?? string.Empty, fromAddress));
            mime.To.Add(MailboxAddress.Parse(message.To));
            if (!string.IsNullOrWhiteSpace(message.ReplyTo) &&
                !string.Equals(message.ReplyTo, fromAddress, StringComparison.OrdinalIgnoreCase))
            {
                mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));
            }
            mime.Subject = message.Subject;

            var body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody };
            foreach (var attachment in message.Attachments)
            {
                body.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }
            mime.Body = body.ToMessageBody();
            return mime;
        }

        private static SecureSocketOptions ToSocketOptions(EmailOptions.SmtpSettings smtp) => smtp.Security switch
        {
            SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
            SmtpSecurity.None => SecureSocketOptions.None,
            _ => smtp.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable
        };
    }
}
