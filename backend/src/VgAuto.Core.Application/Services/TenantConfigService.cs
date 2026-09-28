using VgAuto.Core.Application.Configuration;
using System;
using System.Threading.Tasks;

namespace VgAuto.Core.Application.Services
{
    public class TenantConfigService : ITenantConfigService
    {
        private readonly ITenantConfigRepository repository;

        public TenantConfigService(ITenantConfigRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<RequisitesOptions> GetRequisitesAsync()
        {
            var requisites = await repository.GetRequisitesAsync();

            return new RequisitesOptions(
                requisites.Name,
                requisites.Phone,
                requisites.Address,
                requisites.Email,
                requisites.BankAccount,
                requisites.RegNr,
                requisites.KMKR
            );
        }

        public async Task<PricingOptions> GetPricingAsync()
        {
            var pricing = await repository.GetPricingAsync();

            var invoiceOptions = new InvoiceOptions(
                pricing.VatRate,
                pricing.SurCharge,
                pricing.Disclaimer,
                pricing.SignatureLine,
                pricing.InvoiceEmailContent,
                pricing.ShowBankAccount,
                pricing.ShowRegNo
            );

            var estimateOptions = new EstimateOptions(
                pricing.EstimateEmailContent
            );

            var taxes = pricing.GetTaxes();
            var taxOptions = new TaxOptions(pricing.TaxCountry, pricing.TaxRegion,
                taxes.First?.Name, taxes.First?.Rate ?? 0, taxes.Second?.Name, taxes.Second?.Rate ?? 0);
            return new PricingOptions(invoiceOptions, estimateOptions, VgAuto.Core.Domain.Currencies.Normalize(pricing.Currency), taxOptions);
        }

        public async Task<AppOptions> GetAppOptionsAsync()
        {
            var requisites = await GetRequisitesAsync();
            var pricing = await GetPricingAsync();

            return new AppOptions(requisites, pricing);
        }

        public async Task SaveRequisitesAsync(RequisitesOptions requisitesOptions)
        {
            var requisites = await repository.GetRequisitesAsync();

            requisites.Update(
                requisitesOptions.Name,
                requisitesOptions.Phone,
                requisitesOptions.Address,
                requisitesOptions.Email,
                requisitesOptions.BankAccount,
                requisitesOptions.RegNr,
                requisitesOptions.KMKR
            );

            await repository.SaveRequisitesAsync(requisites);
        }

        public async Task SavePricingAsync(PricingOptions pricingOptions)
        {
            var pricing = await repository.GetPricingAsync();
            var vatRateBefore = pricing.VatRate;

            pricing.Update(
                pricingOptions.Invoice.VatRate,
                pricingOptions.Invoice.SurCharge,
                pricingOptions.Invoice.Disclaimer,
                pricingOptions.Invoice.SignatureLine,
                pricingOptions.Invoice.EmailContent,
                pricingOptions.Estimate.EmailContent,
                pricingOptions.Currency
            );
            pricing.ShowOnDocuments(pricingOptions.Invoice.ShowBankAccount, pricingOptions.Invoice.ShowRegNo);
            var t = pricingOptions.Taxes;
            if (t != null)
            {
                pricing.UseTaxes(VgAuto.Core.Domain.Taxes.Of(t.Tax1Name, t.Tax1Rate, t.Tax2Name, t.Tax2Rate), t.Country, t.Region, changeRegion: true);
            }
            else if (pricingOptions.Invoice.VatRate != vatRateBefore)
            {
                // older clients only send the VAT rate: it becomes the rate of the first tax
                var current = pricing.GetTaxes();
                pricing.UseTaxes(VgAuto.Core.Domain.Taxes.Of(current.First?.Name ?? "Tax", pricingOptions.Invoice.VatRate,
                    current.Second?.Name, current.Second?.Rate ?? 0));
            }

            await repository.SavePricingAsync(pricing);
        }

        public async Task SaveAppOptionsAsync(AppOptions appOptions)
        {
            await SaveRequisitesAsync(appOptions.Requisites);
            await SavePricingAsync(appOptions.Pricing);
        }
    }
}