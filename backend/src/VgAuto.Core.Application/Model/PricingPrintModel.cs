using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Domain;

namespace VgAuto.Core.Application.Model
{
    public class PricingPrintModel
    {
        public Pricing Pricing { get; set; }
        public RequisitesOptions RequisitesOptions { get; set; }
        public PricingOptions PricingOptions { get; set; }
        /// <summary>"GST/HST No." in Canada, otherwise "Tax ID".</summary>
        public string TaxIdLabel { get; set; } = "Tax ID";
        /// <summary>The client's signature of an estimate signed online; null otherwise.</summary>
        public VgAuto.Core.Application.Signing.EstimateSignature Signature { get; set; }
    }
}
