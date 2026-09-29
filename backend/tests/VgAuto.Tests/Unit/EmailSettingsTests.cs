using VgAuto.Core.Application.Email;
using VgAuto.Core.Domain;
using Xunit;

namespace VgAuto.Tests.Unit
{
    public class EmailSettingsTests
    {
        [Fact]
        public void Secrets_are_encrypted_and_need_the_same_server_secret()
        {
            var protector = new SecretProtector("server-secret-0123456789abcdef0123456789");
            var stored = protector.Protect("my SMTP password");
            Assert.StartsWith("v1:", stored);
            Assert.DoesNotContain("password", stored);
            Assert.NotEqual(stored, protector.Protect("my SMTP password")); // random nonce

            Assert.True(protector.TryUnprotect(stored, out var plain));
            Assert.Equal("my SMTP password", plain);
            Assert.True(protector.TryUnprotect(null, out var none));
            Assert.Null(none);
            Assert.Null(protector.Protect(""));

            // another server secret, or a damaged value, cannot read it
            Assert.False(new SecretProtector("another-server-secret-0123456789abcdef").TryUnprotect(stored, out _));
            Assert.False(protector.TryUnprotect("v1:" + stored[5..], out _));
            Assert.False(protector.TryUnprotect("plain text", out _));
        }

        [Fact]
        public void Own_transports_are_checked_before_they_are_saved()
        {
            Assert.Throws<UserException>(() => new EmailTransportSettings { Kind = EmailTransportKind.Smtp }.Validate());
            Assert.Throws<UserException>(() => new EmailTransportSettings { Kind = EmailTransportKind.Smtp, SmtpHost = "mail.example", SmtpPort = 70000 }.Validate());
            Assert.Throws<UserException>(() => new EmailTransportSettings { Kind = EmailTransportKind.Smtp, SmtpHost = "mail.example", SmtpUser = "me" }.Validate());
            new EmailTransportSettings { Kind = EmailTransportKind.Smtp, SmtpHost = "mail.example" }.Validate(); // relay without login

            Assert.Throws<UserException>(() => new EmailTransportSettings { Kind = EmailTransportKind.Gmail, FromAddress = "garage@gmail.com" }.Validate());
            new EmailTransportSettings { Kind = EmailTransportKind.Gmail, FromAddress = "garage@gmail.com", SmtpPassword = "abcd efgh ijkl mnop" }.Validate();

            var graph = new EmailTransportSettings { Kind = EmailTransportKind.Graph, GraphTenantId = "t", GraphClientId = "c", GraphSender = "office@garage.example" };
            Assert.Throws<UserException>(() => graph.Validate());
            graph.GraphClientSecret = "secret";
            graph.Validate();
            Assert.Equal(EmailProvider.Graph, graph.ToOptions().Provider);
            Assert.Equal("office@garage.example", graph.ToOptions().Graph.Sender);
            Assert.Equal("Microsoft 365 (office@garage.example)", graph.Describe());

            Assert.Throws<UserException>(() => new EmailTransportSettings { Kind = EmailTransportKind.Smtp, SmtpHost = "mail.example", FromAddress = "not an address" }.Validate());
            Assert.Throws<UserException>(() => new EmailTransportSettings { Kind = EmailTransportKind.Smtp, SmtpHost = "mail.example", UnreadableSecret = true }.Validate());
        }

        [Fact]
        public void Unknown_kinds_mean_the_built_in_email()
        {
            Assert.Equal(EmailTransportKind.System, EmailTransportKind.Normalize(null));
            Assert.Equal(EmailTransportKind.System, EmailTransportKind.Normalize("whatever"));
            Assert.Equal(EmailTransportKind.Gmail, EmailTransportKind.Normalize(" Gmail "));
            Assert.False(new EmailTransportSettings().IsOwnTransport);
            Assert.False(new EmailTransportSettings().SystemAllowed);
        }
    }
}
