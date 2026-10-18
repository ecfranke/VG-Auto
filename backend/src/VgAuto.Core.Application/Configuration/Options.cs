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
    /// <param name="ShowBankAccount">print the bank account on estimates and invoices (null when saving keeps the current value)</param>
    /// <param name="ShowRegNo">print the Reg No on estimates and invoices (null keeps the current value)</param>
    /// <param name="Vehicle">vehicle details on invoices (null when saving keeps the current values)</param>
    public record InvoiceOptions(int VatRate,string SurCharge, string Disclaimer, bool SignatureLine, string EmailContent, bool? ShowBankAccount = null, bool? ShowRegNo = null, VehicleInfoOptions Vehicle = null) { public InvoiceOptions() : this(default,default, default, default, default) { } }
    /// <param name="Vehicle">vehicle details on estimates (null when saving keeps the current values)</param>
    public record EstimateOptions(string EmailContent, VehicleInfoOptions Vehicle = null) { public EstimateOptions() : this(default(string)) { } }
    /// <summary>Vehicle details on a kind of document.</summary>
    /// <param name="ShowByDefault">the switch "show vehicle information" is on when such a document is issued</param>
    /// <param name="Title">year, make and model</param>
    public record VehicleInfoOptions(bool ShowByDefault, bool Title, bool LicensePlate, bool Odometer, bool Vin)
    {
        public VehicleInfoOptions() : this(default, true, true, true, true) { }

        public static VehicleInfoOptions From(bool show, VgAuto.Core.Domain.VehicleFields f) => new(show,
            f.HasFlag(VgAuto.Core.Domain.VehicleFields.Title), f.HasFlag(VgAuto.Core.Domain.VehicleFields.LicensePlate),
            f.HasFlag(VgAuto.Core.Domain.VehicleFields.Odometer), f.HasFlag(VgAuto.Core.Domain.VehicleFields.Vin));

        public VgAuto.Core.Domain.VehicleFields Fields =>
            (Title ? VgAuto.Core.Domain.VehicleFields.Title : 0) | (LicensePlate ? VgAuto.Core.Domain.VehicleFields.LicensePlate : 0) |
            (Odometer ? VgAuto.Core.Domain.VehicleFields.Odometer : 0) | (Vin ? VgAuto.Core.Domain.VehicleFields.Vin : 0);
    }
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
