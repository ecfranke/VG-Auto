using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Carmasters.Core.Application.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Carmasters.Tests.Integration
{
    /// <summary>Captures outgoing mail instead of sending it.</summary>
    public class FakeMailbox : IEmailSender
    {
        public ConcurrentQueue<EmailMessage> Messages { get; } = new();

        public string Name => "Fake";

        public void Register(IServiceCollection services)
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(this);
        }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Enqueue(message);
            return Task.CompletedTask;
        }

        public EmailMessage LastTo(string address) => Messages.Where(m => m.To == address).LastOrDefault();

        /// <summary>Six digit code of the most recent code email (optionally for one recipient).</summary>
        public string LastCode(string to = null)
        {
            var mail = Messages.Where(m => to == null || m.To == to).LastOrDefault(m => m.Subject.Contains("code") || m.Subject.Contains("Confirm"));
            if (mail == null) return null;
            var match = System.Text.RegularExpressions.Regex.Match(mail.TextBody, @"\b(\d{6})\b");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
