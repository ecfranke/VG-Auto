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
    }
}
