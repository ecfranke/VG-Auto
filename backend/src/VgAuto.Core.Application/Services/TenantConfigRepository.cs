using VgAuto.Core.Application.Services;
using VgAuto.Core.Domain;
using NHibernate;
using NHibernate.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace VgAuto.Core.Persistence.Repositories
{
    public class TenantConfigRepository : ITenantConfigRepository
    {
        private readonly ISession session;

        public TenantConfigRepository(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task<TenantRequisites> GetRequisitesAsync()
        {
            // one record per company (the session is limited to the current company)
            var requisites = session.QueryOver<TenantRequisites>().List<TenantRequisites>().SingleOrDefault();
               

            if (requisites == null)
            {
                // Create default if none exists
                requisites = new TenantRequisites("New company", "", "", "", "", "", "");
                await session.SaveAsync(requisites);
                await session.FlushAsync();
            }
            await Task.CompletedTask;
            return requisites;
        }

        public async Task<TenantPricing> GetPricingAsync()
        {
            // Get the first record (should only be one per tenant)
            var pricing = session.QueryOver<TenantPricing>().List<TenantPricing>().SingleOrDefault();
                

            if (pricing == null)
            {
                // Create default if none exists
                pricing = new TenantPricing(
                    5,
                    "",
                    "",
                    true,
                    "Thank you for your business. Please find your invoice attached.",
                    "Thank you for your interest. Please find your estimate attached."
                );
                // new companies start in Canada with GST; the administrator chooses the province
                pricing.UseTaxes(VgAuto.Core.Domain.Taxes.Of("GST", 5m), "CA", null, changeRegion: true);
                await session.SaveAsync(pricing);
                await session.FlushAsync();
            }
            await Task.CompletedTask;
            return pricing;
        }

        public async Task SaveRequisitesAsync(TenantRequisites requisites)
        {
            if (requisites == null)
                throw new ArgumentNullException(nameof(requisites));

            await session.SaveOrUpdateAsync(requisites);
            await session.FlushAsync();
        }

        public async Task SavePricingAsync(TenantPricing pricing)
        {
            if (pricing == null)
                throw new ArgumentNullException(nameof(pricing));

            await session.SaveOrUpdateAsync(pricing);
            await session.FlushAsync();
        }
    }
}