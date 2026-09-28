using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VgAuto.Core.Application.Configuration
{
    public record JwtOptions(string Secret, string ConsumerSecret,TimeSpan SessionTimeout) { public JwtOptions() : this(default,default, default) { } }
    public record RequisitesOptions(string Name, string Phone, string Address, string Email, string BankAccount, string RegNr, string KMKR) { public RequisitesOptions() : this(default, default, default, default, default, default, default) { } }
    public record InvoiceOptions(int VatRate,string SurCharge, string Disclaimer, bool SignatureLine, string EmailContent) { public InvoiceOptions() : this(default,default, default, default, default) { } }
    public record EstimateOptions(string EmailContent) { public EstimateOptions() : this(default(string)) { } }
    /// <param name="Country">country of registration (CA, US, OTHER)</param>
    /// <param name="Region">province or state of registration (BC, ON, QC ...)</param>
    /// <param name="Tax2Name">second tax (PST, QST, RST), empty when there is none</param>
    public record TaxOptions(string Country, string Region, string Tax1Name, decimal Tax1Rate, string Tax2Name, decimal Tax2Rate) { public TaxOptions() : this(default, default, default, default, default, default) { } }
    /// <param name="Currency">ISO code of the company currency; null when saving keeps the current one.</param>
    /// <param name="Taxes">sales taxes; null when saving keeps them (then Invoice.VatRate sets the rate of the first tax)</param>
    public record PricingOptions(InvoiceOptions Invoice, EstimateOptions Estimate, string Currency = null, TaxOptions Taxes = null) { public PricingOptions() : this(default, default, default, default) { } }

    public record AppOptions(RequisitesOptions Requisites, PricingOptions Pricing) { public AppOptions() : this(default, default) { } }


    public enum DbKind
    {
        Tenancy, Template
    }
    public class DbOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public string Name { get; set; } 
        /// <summary>PostgreSql (default) or MySql.</summary>
        public VgAuto.Core.Application.Database.DatabaseProvider Provider { get; set; } = VgAuto.Core.Application.Database.DatabaseProvider.PostgreSql;
        public MultiTenancyOptions MultiTenancy { get; set; }
        public class MultiTenancyOptions
        {
            public bool Enabled { get; set; }
            public SuffixOptions Suffix { get; set; }


            public class SuffixOptions
            {
                public string Tenancy { get; set; }
                public string Template { get; set; }
            }
        }

    }
}
