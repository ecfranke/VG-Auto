using System.Collections.Concurrent;
using System.Threading.Tasks;
using Carmasters.Core.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Carmasters.Tests.Integration
{
    /// <summary>Captures outgoing mail instead of sending it.</summary>
    public class FakeMailbox
    {
        public ConcurrentQueue<SentPricing> Pricings { get; } = new();

        public void Register(IServiceCollection services)
        {
            services.RemoveAll<IPricingSender>();
            services.AddSingleton<IPricingSender>(new CapturingPricingSender(this));
        }

        public record SentPricing(string To, string Subject);

        private class CapturingPricingSender : IPricingSender
        {
            private readonly FakeMailbox box;
            public CapturingPricingSender(FakeMailbox box) { this.box = box; }
            public Task Send(Pricing pricing)
            {
                box.Pricings.Enqueue(new SentPricing(pricing.Email, pricing.GetDisplayName()));
                return Task.CompletedTask;
            }
        }
    }
}
