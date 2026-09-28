
using VgAuto.Core.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VgAuto.Core.Domain
{
    public abstract partial class Pricing : GuidIdentityEntity
    {
        protected Pricing() { }

         
        protected Pricing ApplyVehicleInformation(Vehicle vehicle) 
        {
            if (vehicle == null) 
            {
                this.VehicleLine1 = this.VehicleLine2 = this.VehicleLine3 = this.VehicleLine4 = String.Empty;
                return this;
            }
            this.VehicleLine1 = "Vehicle: " + vehicle.Producer + " " + vehicle.Model;
            this.VehicleLine2 = "Plate: " + vehicle.RegNr;
            this.VehicleLine3 = "Odometer: " + vehicle.Odo;
            this.VehicleLine4 = "VIN: " + vehicle.Vin;
            return this;
        }

        protected Pricing ApplyClientInformation(Client client) 
        {
            this.Email = client?.CurrentEmail;
            this.PartyName = client == null ? "Walk-in customer" : client.Name;
            this.PartyAddress = client?.Address?.ToString();
             this.PartyCode = client?.RegCode;
            return this;
        }
        protected Pricing( Employee issuer,DateTime? sentOn, DateTime? printedOn, string email, string partyName, string partyAddress, string partyCode, string vehicleLine1, string vehicleLine2, string vehicleLine3,string vehicleLine4, DateTime issuedOn, Guid? id = null)
        {
             
            this.Issuer = issuer;
            Id = id.GetValueOrDefault();
            SentOn = sentOn;
            PrintedOn = printedOn;
            Email = email;
            PartyName = partyName;
            PartyAddress = partyAddress;
            PartyCode = partyCode;
            VehicleLine1 = vehicleLine1;
            VehicleLine2 = vehicleLine2;
            VehicleLine3 = vehicleLine3;
            VehicleLine4 = vehicleLine4;
            IssuedOn = issuedOn;
           
        }

        public abstract string GetFileName();
        public abstract string GetDisplayName();


        public virtual async Task Send(IPricingSender sender, string receipient)
        { 
            this.Email = receipient;
            this.SentOn = DateTime.UtcNow;
            await sender.Send(this);
        }

        /// <summary>Subtotal before tax, or the total with the taxes.</summary>
        public virtual decimal GetTotal(bool withVat)
        {
            var subtotal = this.Lines.Sum(x => x.Total);
            if (!withVat) return subtotal;
            if (!HasTaxes) return this.Lines.Sum(x => x.TotalWithVat); // issued before the taxes were stored
            return subtotal + GetTaxes().Sum(t => t.Amount);
        }

        /// <summary>The taxes of the document with their amounts (one "VAT" line for documents issued before taxes were stored).</summary>
        public virtual IReadOnlyList<TaxAmount> GetTaxes()
        {
            if (!HasTaxes)
            {
                var vat = this.Lines.Sum(x => x.TotalWithVat) - this.Lines.Sum(x => x.Total);
                return new[] { new TaxAmount("VAT", 0, vat) };
            }
            return GetTaxRates().On(this.Lines.Sum(x => x.Total), Currencies.Decimals(Currency));
        }

        public virtual Taxes GetTaxRates() => new(new TaxRate(Tax1Name, Tax1Rate ?? 0), new TaxRate(Tax2Name, Tax2Rate ?? 0));

        protected virtual bool HasTaxes => !string.IsNullOrWhiteSpace(Tax1Name);

        protected void UseTaxes(Taxes taxes)
        {
            Tax1Name = taxes?.First?.Name;
            Tax1Rate = taxes?.First?.Rate;
            Tax2Name = taxes?.Second?.Name;
            Tax2Rate = taxes?.Second?.Rate;
        }

        public virtual string Tax1Name { get; protected set; }
        public virtual decimal? Tax1Rate { get; protected set; }
        public virtual string Tax2Name { get; protected set; }
        public virtual decimal? Tax2Rate { get; protected set; }
        protected IList<PricingLine> lines = new List<PricingLine>();
        public  virtual IEnumerable<PricingLine> Lines => lines.ToList();
         
        public  virtual void AddLine(Taxes taxes,Saleable saleable)
        {
            lines.Add(ToLine(taxes,saleable, Convert.ToInt16(lines.Count() + 1)));
        }

        /// <summary>Prices are before tax: the unit price is printed as entered and the taxes are added.</summary>
        protected PricingLine ToLine(Taxes taxes,Saleable saleable,short jnr)
        {
            var priceSummary = new PriceSummary(saleable, taxes ?? Taxes.None);
            return new PricingLine(this, jnr, saleable.Name, saleable.Quantity, saleable.Price, saleable.Unit, saleable.Discount, priceSummary.TotalWithoutVat, priceSummary.TotalWithVat);
        }


        protected void IssuedNowBy(Employee issuer)
        {
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
            IssuedOn = DateTime.UtcNow;
        }

        public abstract string GetNumber();

        /// <summary>Currency of the document, fixed when it is issued (null for documents issued before currencies existed: the company currency applies).</summary>
        public virtual string Currency { get; protected set; }

        protected void UseCurrency(string currency) => Currency = Currencies.Normalize(currency);

        public  virtual DateTime? SentOn { get; protected set; }
        public  virtual DateTime? PrintedOn { get; }
        public  virtual string Email { get; protected set; }
        public  virtual string PartyName { get; protected set; }
        public  virtual string PartyAddress { get; protected set; }
        public  virtual string PartyCode { get; protected set; }
        public  virtual string VehicleLine1 { get; protected set; }
        public  virtual string VehicleLine2 { get; protected set; }
        public  virtual string VehicleLine3 { get; protected set; }
        public virtual string VehicleLine4 { get; protected set; }
        public  virtual DateTime IssuedOn { get; protected set; }
        public  virtual Employee Issuer { get; protected set; }
    }
}