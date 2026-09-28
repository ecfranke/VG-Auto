using System;

namespace VgAuto.Core.Domain
{

    public class TenantPricing : GuidIdentityEntity
    {
        public virtual int VatRate { get; protected set; }
        public virtual string SurCharge { get; protected set; }
        public virtual string Disclaimer { get; protected set; }
        public virtual bool SignatureLine { get; protected set; }
        /// <summary>Print the bank account / Reg No of the company on estimates and invoices (off by default).</summary>
        public virtual bool ShowBankAccount { get; protected set; }
        public virtual bool ShowRegNo { get; protected set; }

        public virtual void ShowOnDocuments(bool? bankAccount, bool? regNo)
        {
            if (bankAccount != null) ShowBankAccount = bankAccount.Value;
            if (regNo != null) ShowRegNo = regNo.Value;
        }
        public virtual string InvoiceEmailContent { get; protected set; }
        public virtual string EstimateEmailContent { get; protected set; }
        /// <summary>ISO code of the company currency (see <see cref="Currencies"/>).</summary>
        public virtual string Currency { get; protected set; } = Currencies.Default;
        /// <summary>Country (ISO code, e.g. CA) and province/state (e.g. BC) where the company is registered; decides the taxes.</summary>
        public virtual string TaxCountry { get; protected set; }
        public virtual string TaxRegion { get; protected set; }
        public virtual string Tax1Name { get; protected set; }
        public virtual decimal Tax1Rate { get; protected set; }
        public virtual string Tax2Name { get; protected set; }
        public virtual decimal Tax2Rate { get; protected set; }
        public virtual DateTime CreatedAt { get; protected set; }
        public virtual DateTime UpdatedAt { get; protected set; }

        protected TenantPricing() { }

        public TenantPricing(
            int vatRate,
            string surCharge,
            string disclaimer,
            bool signatureLine,
            string invoiceEmailContent,
            string estimateEmailContent,
            Guid? id = null)
        {
            Id = id.GetValueOrDefault();
            VatRate = vatRate;
            SurCharge = surCharge;
            Disclaimer = disclaimer;
            SignatureLine = signatureLine;
            InvoiceEmailContent = invoiceEmailContent;
            EstimateEmailContent = estimateEmailContent;
            Currency = Currencies.Default;
            Tax1Name = "Tax";
            Tax1Rate = vatRate;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public virtual void Update(
            int vatRate,
            string surCharge,
            string disclaimer,
            bool signatureLine,
            string invoiceEmailContent,
            string estimateEmailContent,
            string currency = null)
        {
            VatRate = vatRate;
            if (currency != null)
            {
                if (!Currencies.IsSupported(currency.Trim().ToUpperInvariant())) throw new UserException("Unsupported currency.");
                Currency = currency.Trim().ToUpperInvariant();
            }
            SurCharge = surCharge;
            Disclaimer = disclaimer;
            SignatureLine = signatureLine;
            InvoiceEmailContent = invoiceEmailContent;
            EstimateEmailContent = estimateEmailContent;
            UpdatedAt = DateTime.UtcNow;
        }

        public virtual Taxes GetTaxes() =>
            string.IsNullOrWhiteSpace(Tax1Name) ? Taxes.Of("Tax", VatRate) : Taxes.Of(Tax1Name, Tax1Rate, Tax2Name, Tax2Rate);

        /// <summary>Sets the taxes (up to two); the region is only changed when given.</summary>
        public virtual void UseTaxes(Taxes taxes, string country = null, string region = null, bool changeRegion = false)
        {
            if (taxes.First == null) throw new UserException("At least one tax name is required (use a rate of 0 when no tax is charged).");
            Tax1Name = taxes.First.Name;
            Tax1Rate = taxes.First.Rate;
            Tax2Name = taxes.Second?.Name;
            Tax2Rate = taxes.Second?.Rate ?? 0;
            VatRate = (int)Math.Round(taxes.TotalRate, MidpointRounding.AwayFromZero);
            if (changeRegion)
            {
                TaxCountry = string.IsNullOrWhiteSpace(country) ? null : country.Trim().ToUpperInvariant();
                TaxRegion = string.IsNullOrWhiteSpace(region) ? null : region.Trim().ToUpperInvariant();
            }
            UpdatedAt = DateTime.UtcNow;
        }
    }
}