using VgAuto.Core.Domain;
using System;
using System.Linq;

namespace VgAuto.Core.Domain
{
    public class Estimate : Pricing
    {
        protected Estimate() : base() { }
        public Estimate( string number,
                       Employee issuer = null, 
                        DateTime issuedOn = default, 
                        string partyName = null,
                        DateTime? sentOn = null,
                        DateTime? printedOn = null,
                        string email = null, 
                        string partyAddress = null,
                        string partyCode = null,
                        string vehicleLine1 = null,
                        string vehicleLine2 = null,
                        string vehicleLine3 = null,
                        string vehicleLine4 = null,
                        Guid? id = null) : base( issuer, sentOn, printedOn, email, partyName, partyAddress, partyCode, vehicleLine1, vehicleLine2, vehicleLine3,vehicleLine4, issuedOn,id)
        {
            this.Number = number;
        }

        public  override string GetFileName()
        {
            return $"estimate_nr_{Number}.pdf";
        }
        public override string GetDisplayName()
        {
            return $"Estimate nr. {Number}";
        }
        public virtual string Number { get; }

        /// <summary>Copy of <see cref="GuidIdentityEntity.CompanyId"/> in the estimate table: numbers are unique per company.</summary>
        public virtual Guid NumberCompanyId { get; protected internal set; }
        public virtual Estimate CreateFor(Taxes taxes,Offer offer, Employee issuer, string currency = null)
        {
            UseCurrency(currency);
            UseTaxes(taxes);
            var newSet = offer.Products.ToArray();
            ApplyClientInformation(offer.Work.Client);
            if (offer.IsVehicleLinesOnEstimate)
            {
                ApplyVehicleInformation(offer.Work.Vehicle);
            }
            else ApplyVehicleInformation(null);
            IssuedNowBy(issuer);
            SentOn = default;
            Email = default;
             PricingLine.Synchronize(newSet.Select((x, i) => ToLine(taxes,x, Convert.ToInt16(i + 1))).ToArray(), lines);
            return this;
        }

        public override string GetNumber()
        {
            return Number.ToString();
        }
    }
}