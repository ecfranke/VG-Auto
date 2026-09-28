using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VgAuto.Core.Application.Email
{
    public record EmailAttachment(string FileName, string ContentType, byte[] Content);

    public class EmailMessage
    {
        public EmailMessage(string to, string subject, string textBody)
        {
            if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("Recipient is required", nameof(to));
            To = to.Trim();
            Subject = subject ?? string.Empty;
            TextBody = textBody ?? string.Empty;
        }

        /// <summary>One or more addresses separated by comma or semicolon.</summary>
        public string To { get; }

        /// <summary>The recipients; throws <see cref="EmailDeliveryException"/> for an invalid address.</summary>
        public IReadOnlyList<string> Recipients => EmailAddresses.Parse(To);
        public string Subject { get; }
        public string TextBody { get; }
        public string HtmlBody { get; init; }
        /// <summary>Display name of the sender, e.g. the workshop name.</summary>
        public string FromName { get; init; }
        /// <summary>Address replies should go to, e.g. the workshop's own address.</summary>
        public string ReplyTo { get; init; }
        /// <summary>Only used when Email:FromAddress is not configured (legacy behaviour).</summary>
        public string FallbackFromAddress { get; init; }
        public IList<EmailAttachment> Attachments { get; } = new List<EmailAttachment>();
    }

    public static class EmailAddresses
    {
        private static readonly System.Text.RegularExpressions.Regex Simple =
            new(@"^[^@\s,;<>""]+@[^@\s,;<>""]+\.[^@\s,;<>""]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

        public static bool IsValid(string address) =>
            !string.IsNullOrWhiteSpace(address) && Simple.IsMatch(address.Trim()) && MimeKit.MailboxAddress.TryParse(address.Trim(), out _);

        /// <summary>Splits "a@x.com; b@y.com" into addresses and checks each one.</summary>
        public static IReadOnlyList<string> Parse(string addresses)
        {
            var list = new List<string>();
            foreach (var part in (addresses ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!IsValid(part)) throw new EmailDeliveryException($"\"{part}\" is not a valid email address.");
                list.Add(part);
            }
            if (list.Count == 0) throw new EmailDeliveryException("No recipient email address.");
            return list;
        }
    }

    public interface IEmailSender
    {
        /// <summary>Name of the transport, for logs ("Smtp" / "Graph").</summary>
        string Name { get; }
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }

    public class EmailDeliveryException : Exception
    {
        public EmailDeliveryException(string message, Exception inner = null) : base(message, inner) { }
    }
}
