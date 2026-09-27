using System;

namespace VgAuto.Core.Domain
{

    public class TenantPricing : GuidIdentityEntity
    {
        public virtual int VatRate { get; protected set; }
        public virtual string SurCharge { get; protected set; }
        public virtual string Disclaimer { get; protected set; }
        public virtual bool SignatureLine { get; protected set; }
        public virtual string InvoiceEmailContent { get; protected set; }
        public virtual string EstimateEmailContent { get; protected set; }
        /// <summary>ISO code of the company currency (see <see cref="Currencies"/>).</summary>
        public virtual string Currency { get; protected set; } = Currencies.Default;
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
    }
}