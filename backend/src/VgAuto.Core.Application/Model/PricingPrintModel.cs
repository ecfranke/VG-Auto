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
    }
}
