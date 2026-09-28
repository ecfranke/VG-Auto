using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace VgAuto.Core.Domain
{
    public class Vehicle: GuidIdentityEntity
    {
        IList<VehicleRegistration> registrations = new List<VehicleRegistration>();
        protected Vehicle() { }
        public    Vehicle(
            string licensePlate,
            DateTime introducedAt,
            string manufacturer = null,
            string model = null, 
            string vin = null,
            int? odo = null,
            string body = null,
            string drivingSide = null,
            string engine = null,
            DateTime? productionDate = null,
            string region = null,
            string series = null,
            string transmission = null,
            string description = null,
            Guid? id = null,
            int? year = null)
        {  
            IntroducedAt = introducedAt; 
            SetValues(licensePlate, manufacturer, model, vin, odo, body, drivingSide, engine, productionDate, region, series, transmission,description, year);
            this.Id = id.GetValueOrDefault();
        }


        private void SetValues(string licensePlate,
                               string manufacturer,
                               string model,
                               string vin,
                               int? odo,
                               string body,
                               string drivingSide,
                               string engine,
                               DateTime? productionDate,
                               string region,
                               string series,
                               string transmission,
                               string description,
                               int? year)
        {
            if (string.IsNullOrWhiteSpace(licensePlate)&& string.IsNullOrWhiteSpace(vin))
            {
                throw new UserException("License plate or VIN is required.");
            }
            if (year != null && (year < 1900 || year > DateTime.UtcNow.Year + 1))
            {
                throw new UserException($"Year must be between 1900 and {DateTime.UtcNow.Year + 1}.");
            }
            Description = description;
            Manufacturer = manufacturer;
            Model = model;
            Year = year;
            LicensePlate = licensePlate;
            Vin = vin;
            Odo = odo;
            Body = body;
            DrivingSide = drivingSide;
            Engine = engine;
            ProductionDate = productionDate;
            Region = region;
            Series = series;
            Transmission = transmission;
        }
         
        public  virtual string Manufacturer { get; protected set; }
        public  virtual string Model { get; protected set; }
        /// <summary>Model year, e.g. 2019 (optional).</summary>
        public  virtual int? Year { get; protected set; }
        public  virtual string LicensePlate { get; protected set; }

        /// <summary>"2019 Honda Civic"</summary>
        public virtual string Title => string.Join(" ", new[] { Year?.ToString(), Manufacturer, Model }.Where(x => !string.IsNullOrWhiteSpace(x)));
        public  virtual string Vin { get; protected set; }
        public  virtual int? Odo { get; protected set; }
        public  virtual string Body { get; protected set; }
        public  virtual string DrivingSide { get; protected set; }
        public  virtual string Engine { get; protected set; }
        public  virtual DateTime? ProductionDate { get; protected set; }
        public  virtual string Region { get; protected set; }
        public  virtual string Series { get; protected set; }
        public  virtual string Transmission { get; protected set; }

        public  virtual Client Owner { get 
            { 
                return registrations.SingleOrDefault(x=>x.IsEffective)?.Owner; 
            } 
        }

        public  virtual IReadOnlyCollection<VehicleRegistration> Registrations { get => registrations.ToList().AsReadOnly(); }

        public  virtual void Edit(string licensePlate,
            string manufacturer = null,
            string model = null,
            string vin = null,
            int odo = 0,
            string body = null,
            string drivingSide = null,
            string engine = null,
            DateTime? productionDate = null,
            string region = null,
            string series = null,
            string transmission = null,
            string description = null,
            int? year = null)
        {
            SetValues(licensePlate, manufacturer, model, vin, odo, body, drivingSide, engine, productionDate, region, series, transmission,description, year);
        }


        public  virtual void RegisterTo(Client owner)
        {
            if (this.Owner!=null && this.Owner.Equals(owner)) throw new Exception("already registered to this owner");
            //end previous reg 
            this.EndRegistration();

            this.registrations.Add(VehicleRegistration.New(this,owner));
        }

        public virtual void EndRegistration() 
        {
            this.registrations
               .SingleOrDefault(x => x.IsEffective)?.End();
        }


        public  virtual DateTime IntroducedAt { get; }
        public  virtual string Description { get; protected set; }
    }

}