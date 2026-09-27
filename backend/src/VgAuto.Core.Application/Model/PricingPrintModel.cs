using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Domain;

namespace VgAuto.Core.Application.Model
{
    public class PricingPrintModel
    {
        public Pricing Pricing { get; set; }
        public RequisitesOptions RequisitesOptions { get; set; }
        public PricingOptions PricingOptions { get; set; }
    }
}
