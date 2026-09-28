namespace VgAuto.Core.Application.Authentication
{
    /// <summary>Configuration section "Authentication".</summary>
    public class AuthenticationOptions
    {
        public EmailCodeOptions EmailCode { get; set; } = new();
        public PasswordResetOptions PasswordReset { get; set; } = new();
        public MicrosoftOptions Microsoft { get; set; } = new();

        public class EmailCodeOptions
        {
            /// <summary>Password logins need a one time code sent by email (two step login). Default on.</summary>
            public bool RequireForPasswordLogin { get; set; } = true;
            /// <summary>When false, users without an email address cannot log in with a password while codes are required.</summary>
            public bool AllowUsersWithoutEmail { get; set; } = false;
            public int CodeLifetimeMinutes { get; set; } = 10;
            public int MaxAttempts { get; set; } = 5;
            public int MaxSends { get; set; } = 3;
            /// <summary>After a code was entered, the same browser skips the code for this many days (0: ask every time).
            /// Changing the password ends it.</summary>
            public int RememberDeviceDays { get; set; } = 7;
        }

        public class PasswordResetOptions
        {
            public bool Enabled { get; set; } = true;
        }

        /// <summary>
        /// "Sign in with Microsoft" (Entra ID / personal Microsoft accounts) using an app registration
        /// with a web redirect URI of {app}/auth/microsoft/callback.
        /// </summary>
        public class MicrosoftOptions
        {
            public bool Enabled { get; set; }
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            /// <summary>"common" = any work, school or personal Microsoft account; or a tenant id / "organizations" / "consumers".</summary>
            public string TenantId { get; set; } = "common";
            public string Instance { get; set; } = "https://login.microsoftonline.com";

            public string AuthorityUrl => $"{Instance.TrimEnd('/')}/{TenantId}/v2.0";
        }
    }
}
