using System;
using VgAuto.Core.Domain;

namespace VgAuto.Core.Application.Email
{
    /// <summary>Kinds of email transport that can be chosen in the administration.</summary>
    public static class EmailTransportKind
    {
        /// <summary>A company uses the built-in transport of the system (a super administrator allows it per company).</summary>
        public const string System = "system";
        /// <summary>The system uses the Email section of the server configuration (appsettings).</summary>
        public const string Config = "config";
        public const string Smtp = "smtp";
        /// <summary>Microsoft 365 / Exchange Online through Microsoft Graph.</summary>
        public const string Graph = "graph";
        /// <summary>Gmail or Google Workspace through smtp.gmail.com with an app password.</summary>
        public const string Gmail = "gmail";

        public static bool IsOwnTransport(string kind) => kind is Smtp or Graph or Gmail;

        public static string Normalize(string kind) => kind?.Trim().ToLowerInvariant() switch
        {
            Smtp => Smtp,
            Graph => Graph,
            Gmail => Gmail,
            Config => Config,
            _ => System,
        };
    }

    /// <summary>
    /// An email transport set in the administration: the built-in one of the system or the own one of a company.
    /// Secrets are plain text here; the repository stores them encrypted.
    /// </summary>
    public class EmailTransportSettings
    {
        public const string GmailHost = "smtp.gmail.com";
        public const int GmailPort = 587;

        /// <summary>See <see cref="EmailTransportKind"/>.</summary>
        public string Kind { get; set; } = EmailTransportKind.System;
        /// <summary>Companies only: a super administrator allows the company to use the built-in transport.</summary>
        public bool SystemAllowed { get; set; }
        public string FromAddress { get; set; }
        public string FromName { get; set; }
        public string SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPassword { get; set; }
        public SmtpSecurity SmtpSecurity { get; set; } = SmtpSecurity.Auto;
        public string GraphTenantId { get; set; }
        public string GraphClientId { get; set; }
        public string GraphClientSecret { get; set; }
        public string GraphSender { get; set; }
        public DateTime? UpdatedAt { get; set; }
        /// <summary>A stored secret could not be decrypted (the server secret changed) and has to be entered again.</summary>
        public bool UnreadableSecret { get; set; }

        public bool IsOwnTransport => EmailTransportKind.IsOwnTransport(Kind);

        /// <summary>Options for <see cref="SmtpEmailSender"/> / <see cref="GraphEmailSender"/> (own transports only).</summary>
        public EmailOptions ToOptions()
        {
            var options = new EmailOptions { FromAddress = Clean(FromAddress), FromName = Clean(FromName) };
            switch (Kind)
            {
                case EmailTransportKind.Graph:
                    options.Provider = EmailProvider.Graph;
                    options.Graph.TenantId = Clean(GraphTenantId);
                    options.Graph.ClientId = Clean(GraphClientId);
                    options.Graph.ClientSecret = GraphClientSecret;
                    options.Graph.Sender = Clean(GraphSender);
                    break;
                case EmailTransportKind.Gmail:
                    // the Gmail address is the sender and the SMTP user; the password is an app password
                    options.Provider = EmailProvider.Smtp;
                    options.FromAddress = Clean(FromAddress) ?? Clean(SmtpUser);
                    options.Smtp.Host = GmailHost;
                    options.Smtp.Port = GmailPort;
                    options.Smtp.Security = SmtpSecurity.StartTls;
                    options.Smtp.User = Clean(SmtpUser) ?? Clean(FromAddress);
                    options.Smtp.Password = SmtpPassword;
                    break;
                case EmailTransportKind.Smtp:
                    options.Provider = EmailProvider.Smtp;
                    options.Smtp.Host = Clean(SmtpHost);
                    options.Smtp.Port = SmtpPort ?? 587;
                    options.Smtp.Security = SmtpSecurity;
                    options.Smtp.User = Clean(SmtpUser);
                    options.Smtp.Password = SmtpPassword;
                    break;
                default:
                    throw new InvalidOperationException($"'{Kind}' is not an email transport of its own.");
            }
            return options;
        }

        /// <summary>"SMTP (smtp.example.com)", "Microsoft 365 (office@example.com)", "Gmail (garage@gmail.com)".</summary>
        public string Describe() => Kind switch
        {
            EmailTransportKind.Smtp => $"SMTP ({SmtpHost})",
            EmailTransportKind.Graph => $"Microsoft 365 ({GraphSender})",
            EmailTransportKind.Gmail => $"Gmail ({Clean(FromAddress) ?? SmtpUser})",
            EmailTransportKind.Config => "server configuration",
            _ => "built-in email",
        };

        /// <summary>Checks that an own transport has everything it needs to send.</summary>
        public void Validate()
        {
            if (UnreadableSecret)
                throw new UserException("The saved password or client secret can no longer be read (the server secret changed). Enter it again.");
            if (!string.IsNullOrWhiteSpace(FromAddress) && !EmailAddresses.IsValid(FromAddress))
                throw new UserException($"\"{FromAddress}\" is not a valid sender address.");
            switch (Kind)
            {
                case EmailTransportKind.Smtp:
                    if (string.IsNullOrWhiteSpace(SmtpHost)) throw new UserException("The SMTP server is required.");
                    if (SmtpPort is < 1 or > 65535) throw new UserException("The SMTP port must be between 1 and 65535.");
                    if (!string.IsNullOrWhiteSpace(SmtpUser) && string.IsNullOrEmpty(SmtpPassword)) throw new UserException("The SMTP password is required.");
                    break;
                case EmailTransportKind.Gmail:
                    if (!EmailAddresses.IsValid(FromAddress)) throw new UserException("The Gmail address is required.");
                    if (string.IsNullOrEmpty(SmtpPassword)) throw new UserException("The app password of the Gmail account is required.");
                    break;
                case EmailTransportKind.Graph:
                    if (string.IsNullOrWhiteSpace(GraphTenantId) || string.IsNullOrWhiteSpace(GraphClientId))
                        throw new UserException("The tenant ID and the application (client) ID are required.");
                    if (string.IsNullOrEmpty(GraphClientSecret)) throw new UserException("The client secret is required.");
                    if (!EmailAddresses.IsValid(GraphSender)) throw new UserException("The sender mailbox is required.");
                    break;
            }
        }

        private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
