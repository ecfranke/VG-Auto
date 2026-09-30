using VgAuto.Core.Application.Extensions;
﻿using VgAuto.Core.Domain;
using VgAuto.Http.Api.Models;
using Microsoft.AspNetCore.Mvc;
using VgAuto.Core.Application.Services;
using Microsoft.AspNetCore.Authorization;
using System;
using NHibernate;
using System.Linq;
using NHibernate.Mapping;
using System.Collections.Generic;
using Dapper;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Database;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace VgAuto.Http.Api.Controllers
{
    /// <summary>
    /// todo refract not http methods out
    /// </summary>
    [TenantRateLimit]
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController : BaseController<VehicleDto, Vehicle>
    {
        private readonly ISession session;
        private readonly IAdminAuditLog audit;

        public VehiclesController(IRepository repository,ISession session, IAdminAuditLog audit) : base(repository)
        {
            this.session = session;
            this.audit = audit;
        }

        /// <summary>
        /// Only administrators delete vehicles, and only vehicles no work order uses; their ownership history goes with them.
        /// Each deleted vehicle is written to the audit log of the company.
        /// </summary>
        [Authorize(Policy = "ServerSidePolicy")]
        [RequireAdmin]
        public override async Task<IActionResult> Delete([FromBody] Guid[] ids)
        {
            var deleted = new List<(string Target, string Details)>();
            foreach (var id in ids)
            {
                var vehicle = repository.Get<Vehicle>(id);
                var work = Convert.ToInt64(session.CreateSQLQuery(SqlDialect.Current.Sql("select count(*) from domain.work where vehicleid = :id"))
                    .SetParameter("id", vehicle.Id).UniqueResult());
                var name = string.Join(" ", new[] { vehicle.LicensePlate, vehicle.Vin }.Where(x => !string.IsNullOrWhiteSpace(x)));
                if (work > 0)
                    throw new UserException($"Cannot delete the vehicle {name}: {(work == 1 ? "1 work order uses" : $"{work} work orders use")} it. Delete those first, or keep the vehicle.");
                deleted.Add((name, string.Join("; ", new[] { vehicle.Title, vehicle.Owner?.Name }.Where(x => !string.IsNullOrWhiteSpace(x)))));
                repository.Delete(vehicle);
            }
            session.Flush();
            foreach (var (target, details) in deleted)
                await audit.WriteAsync(this.TenantName(), this.CurrentAccount()?.UserName ?? this.UserName(), "vehicle.delete", target, details, this.CompanyId());
            return Ok();
        }

        [HttpGet("client/{clientId}")]
        public ClientVehicleDto[] ClientVehicles(Guid clientId)
        {
            var vehicles = repository.GetConnection()
               .Query<ClientVehicleDto>(SqlDialect.Current.Sql(@" select v.producer as manufacturer,v.model,v.year,v.series as trim,v.regnr as licenseplate,v.vin,v.id,r.ownerid from domain.vehicleregistration r
										    inner join domain.vehicle v on v.id = r.vehicleid
											  where r.ownerid = @ownerid  
												  and r.datetimeto is null and v.company_id = @companyId"), new { ownerid = clientId, companyId = this.CompanyId() })
               .ToArray();


            return vehicles;
        }
       
        protected override VehicleDto Map(Vehicle entity)
        {
            return new VehicleDto
            {
                Body = entity.Body,
                Description = entity.Description,
                DrivingSide = entity.DrivingSide,
                Engine = entity.Engine,
                Id = entity.Id,
                IntroducedAt = entity.IntroducedAt,
                Model = entity.Model,
                Odo = entity.Odo.GetValueOrDefault(),
                OwnerId = entity.Owner?.Id,
                OwnerName = entity.Owner?.Name,
                Manufacturer = entity.Manufacturer,
                Year = entity.Year,
                ProductionDate = entity.ProductionDate,
                Region = entity.Region,
                LicensePlate = entity.LicensePlate,
                Trim = entity.Trim,
                Transmission = entity.Transmission,
                Vin = entity.Vin
            };
        }


        protected override Vehicle CreateFrom(VehicleDto model)
        {
            var vehicle = new Vehicle(model.LicensePlate,
                        model.IntroducedAt,
                        model.Manufacturer,
                        model.Model,
                        model.Vin,
                        model.Odo,
                        model.Body,
                        model.DrivingSide,
                        model.Engine,
                        model.ProductionDate,
                        model.Region,
                        model.Trim,
                        model.Transmission,
                        model.Description,
                        year: model.Year);
            if (model.OwnerId != null)
            {
                vehicle.RegisterTo(repository.Get<Client>(model.OwnerId.GetValueOrDefault()));
            }
            return vehicle;
        }

        protected override void Edit(Vehicle entity, VehicleDto model)
        {
            entity.Edit(model.LicensePlate,
                        model.Manufacturer,
                        model.Model,
                        model.Vin,
                        model.Odo,
                        model.Body,
                        model.DrivingSide,
                        model.Engine,
                        model.ProductionDate,
                        model.Region,
                        model.Trim,
                        model.Transmission,
                        model.Description,
                        model.Year);
            if (model.OwnerId != null)
            {
                if (entity.Owner?.Id != model.OwnerId.GetValueOrDefault()) 
                {
                    entity.RegisterTo(repository.Get<Client>(model.OwnerId.GetValueOrDefault()));
                }
                
            }
            else entity.EndRegistration();
        }

        private static readonly Dictionary<string, string> SortColumns = new()
        {
            ["id"] = "v.id",
            ["regnr"] = "v.regnr",
            ["licenseplate"] = "v.regnr",
            ["manufacturer"] = "v.producer",
            ["year"] = "v.year",
            ["vin"] = "v.vin",
            ["producer"] = "v.producer",
            ["model"] = "v.model",
            ["ownername"] = "ownername",
        };

        [HttpGet("page")]
        public PagedResult<VehiclePageDto> GetPage(string searchText, string orderby, int limit, int offset, bool desc)
        {
            return
                repository
                  .PageQuery<VehiclePageDto>(orderby, limit, offset, desc)
                  .FilterBy(searchText)
                  .SearchFields("v.regnr", "v.vin", "p.firstname", "p.lastname", "l.name", "v.producer", "v.model")
                  .ForCompany("v.company_id", this.CompanyId())
                  .Sortable(SortColumns, "v.id")
                  .SelectSql($@"SELECT 
                        v.id, 
                        v.regnr as licenseplate,vin, producer as manufacturer,model,v.year,body,drivingside,engine,{SqlDialect.Current.FormatMonthYear("productiondate")} as productiondate,region,series as trim,transmission,
                        concat_ws(' ',p.firstname,p.lastname,l.name)  as ownername,
                        v0.ownerid as ownerid
                        FROM domain.vehicle AS v
                               left join domain.vehicleregistration v0 on v.id = v0.vehicleid AND v0.datetimeto IS NULL
	                           left join domain.legalclient l on l.id = v0.ownerid
                               left join domain.privateclient p on p.id = v0.ownerid")
                  .ToResult();

        }
    }
}
