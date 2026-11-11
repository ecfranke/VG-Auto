using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VgAuto.Http.Api.Models
{
    public record SendPricingDto(string EmailAddress, string DisplayName);
    public record PutActivityDto(string Notes, bool IsVehicelLinesOnPricing);
    public class PutProductOrService
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal? Quantity { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public short? Discount { get; set; }
    }
    public record PostOrPutWork(Guid? ClientId, string Description, Guid? VehicleId, Guid[] AssignedTo, int? Odo, bool StartWithOffer, WalkInCustomer WalkIn = null);

    /// <summary>
    /// A walk-in customer typed in when the work is started. <see cref="SaveToRecords"/>: kept as a client and a vehicle
    /// (an existing vehicle with the same license plate or VIN is used), otherwise only on the work.
    /// </summary>
    public record WalkInCustomer(string Name, string Phone, string Manufacturer, string Model, int? Year, string LicensePlate, string Vin, bool SaveToRecords = true);

    
    public record WorkPage(
        Guid id, 
        string WorkNr, 
        DateTime StartedOn,
        DateTime? SentOn,
        string Status,
        string Issued, 
        Guid ClientId, 
        string ClientName, 
        Guid VehicleId, 
        string LicensePlate, 
        string MechanicNames, 
        string Notes,  
        bool hasRepairs,
        int numberOfOffers,
        JsonNode OfferIssuance, 
        JsonNode Issuance,
        string VehicleManufacturer = null,
        string VehicleModel = null,
        int? VehicleYear = null) 
    {
        public WorkPage() : this(default,default,default,default,default,default,default,default,default,default,default,default,default,default,default,default) { }

        /// <summary>Readable work number, e.g. RP_TF_2019_HC_2026_09_28_15 (see <see cref="VgAuto.Core.Domain.WorkCode"/>).</summary>
        public string Code => VgAuto.Core.Domain.WorkCode.Format(hasRepairs || Issuance != null, ClientName, VehicleYear, VehicleManufacturer, VehicleModel, StartedOn, WorkNr);
    }

}