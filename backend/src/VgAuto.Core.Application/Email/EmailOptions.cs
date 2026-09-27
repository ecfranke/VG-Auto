namespace VgAuto.Core.Application.Email
{
    public enum EmailProvider
    {
        Smtp,
        Graph
    }

    public enum SmtpSecurity
    {
        /// <summary>SSL on connect for port 465, STARTTLS when offered otherwise.</summary>
        Auto,
        SslOnConnect,
        StartTls,
        None
    }

    /// <summary>Configuration section "Email".</summary>
    public class EmailOptions
    {
        public EmailProvider Provider { get; set; } = EmailProvider.Smtp;

        /// <summary>Sender address. With Graph this defaults to the mailbox given in Graph:Sender.</summary>
        public string FromAddress { get; set; }
        public string FromName { get; set; }

        public SmtpSettings Smtp { get; set; } = new SmtpSettings();
        public GraphSettings Graph { get; set; } = new GraphSettings();

        public class SmtpSettings
        {
            public string Host { get; set; }
            public int Port { get; set; } = 587;
            public string User { get; set; }
            public string Password { get; set; }
            public SmtpSecurity Security { get; set; } = SmtpSecurity.Auto;
            /// <summary>Only for test servers with self signed certificates.</summary>
            public bool AllowInvalidCertificate { get; set; }
            public int TimeoutSeconds { get; set; } = 30;
        }

        /// <summary>
        /// Microsoft Graph with an Entra ID app registration (client credentials) that has the
        /// application permission Mail.Send. Limit it to the sender mailbox with an application access policy.
        /// </summary>
        public class GraphSettings
        {
            public string TenantId { get; set; }
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            /// <summary>Mailbox that sends the mail (user principal name or object id).</summary>
            public string Sender { get; set; }
            public bool SaveToSentItems { get; set; } = true;
            public string Authority { get; set; } = "https://login.microsoftonline.com";
            public string GraphEndpoint { get; set; } = "https://graph.microsoft.com/v1.0";
        }
    }
}
