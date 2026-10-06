using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Extensions;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Domain;
using VgAuto.Http.Api.Controllers.Clients;
using VgAuto.Http.Api.Models;

namespace VgAuto.Http.Api.Controllers
{
    /// <summary>
    /// Clients and vehicles in the administration (/admin/clients, /admin/vehicles): list and delete.
    /// Super administrators see every company; administrators only their own company.
    /// Nothing that is still in use is deleted: the administrator is told what refers to it.
    /// </summary>
    [TenantRateLimit]
    [Authorize(Policy = "ServerSidePolicy")]
    [RequireAdmin]
    [Route("api/admin")]
    [ApiController]
    public class AdminDataController : ControllerBase
    {
        private readonly ISession session;
        private readonly IRepository repository;
        private readonly ICompanyScope companies;
        private readonly IAdminAuditLog audit;

        public AdminDataController(ISession session, IRepository repository, ICompanyScope companies, IAdminAuditLog audit)
        {
            this.session = session;
            this.repository = repository;
            this.companies = companies;
            this.audit = audit;
        }

        // classes with setters: column types differ between PostgreSQL and MySQL (smallint/int, bigint/decimal)
        public class ClientRow
        {
            public Guid Id { get; set; }
            public Guid CompanyId { get; set; }
            public string CompanyName { get; set; }
            public string Name { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public long WorkCount { get; set; }
            public long VehicleCount { get; set; }
        }

        public class VehicleRow
        {
            public Guid Id { get; set; }
            public Guid CompanyId { get; set; }
            public string CompanyName { get; set; }
            public string LicensePlate { get; set; }
            public string Manufacturer { get; set; }
            public string Model { get; set; }
            public int? Year { get; set; }
            public string Vin { get; set; }
            public string Owner { get; set; }
            public long WorkCount { get; set; }
        }

        private bool IsSuperAdmin => this.CurrentAccount()?.Role == UserRoles.SuperAdmin;

        /// <summary>The company a list shows: any (or all) for super administrators, always the own one for administrators.</summary>
        private Guid? ListScope(Guid? requested) => IsSuperAdmin ? requested : this.CurrentAccount().CompanyId;

        [HttpGet("clients")]
        public PagedResult<ClientRow> Clients(string searchText, Guid? companyId, int limit = 50, int offset = 0)
        {
            var query = repository.PageQuery<ClientRow>(null, limit, offset, false).Sortable(new Dictionary<string, string>(), "name");
            if (ListScope(companyId) is Guid company) query.ForCompany("c.company_id", company);
            return query
                .FilterBy(searchText)
                .SearchFields("p.firstname", "p.lastname", "l.name", "c.phone", "ce.address", "coalesce(r.name, co.name)")
                .SelectSql(@"select
                        c.id, c.company_id as companyid, coalesce(r.name, co.name) as companyname,
                        concat_ws(' ', p.firstname, p.lastname, l.name) as name, c.phone, ce.address as email,
                        concat_ws(' ', c.address, c.city, c.region, c.postalcode, c.country) as address,
                        (select count(*) from domain.work w where w.clientid = c.id) as workcount,
                        (select count(distinct vr.vehicleid) from domain.vehicleregistration vr where vr.ownerid = c.id) as vehiclecount
                      from domain.client c
                      inner join domain.company co on co.id = c.company_id
                      left join tenant_config.requisites r on r.company_id = c.company_id
                      left join domain.privateclient p on p.id = c.id
                      left join domain.legalclient l on l.id = c.id
                      left join domain.clientemail ce on ce.clientid = c.id and ce.isactive = true")
                .ToResult();
        }

        [HttpGet("vehicles")]
        public PagedResult<VehicleRow> Vehicles(string searchText, Guid? companyId, int limit = 50, int offset = 0)
        {
            var query = repository.PageQuery<VehicleRow>(null, limit, offset, false).Sortable(new Dictionary<string, string>(), "v.regnr");
            if (ListScope(companyId) is Guid company) query.ForCompany("v.company_id", company);
            return query
                .FilterBy(searchText)
                .SearchFields("v.regnr", "v.vin", "v.producer", "v.model", "coalesce(r.name, co.name)")
                .SelectSql(@"select
                        v.id, v.company_id as companyid, coalesce(r.name, co.name) as companyname,
                        v.regnr as licenseplate, v.producer as manufacturer, v.model, v.year, v.vin,
                        (select max(concat_ws(' ', p.firstname, p.lastname, l.name))
                           from domain.vehicleregistration vr
                           left join domain.privateclient p on p.id = vr.ownerid
                           left join domain.legalclient l on l.id = vr.ownerid
                          where vr.vehicleid = v.id and vr.datetimeto is null) as owner,
                        (select count(*) from domain.work w where w.vehicleid = v.id) as workcount
                      from domain.vehicle v
                      inner join domain.company co on co.id = v.company_id
                      left join tenant_config.requisites r on r.company_id = v.company_id")
                .ToResult();
        }

        [HttpDelete("clients/{id:guid}")]
        public async Task<IActionResult> DeleteClient(Guid id)
        {
            var companyId = CompanyOf("client", id);
            if (companyId == null) return NotFound();
            var client = session.Get<Client>(id);
            ClientsController.EnsureCanDelete(HttpContext, client);
            var name = client.Name;
            session.Delete(client);
            session.Flush();
            await audit.WriteAsync(this.TenantName(), this.CurrentAccount()?.UserName ?? this.UserName(), "client.delete", name, null, companyId);
            return Ok();
        }

        [HttpDelete("vehicles/{id:guid}")]
        public async Task<IActionResult> DeleteVehicle(Guid id)
        {
            var companyId = CompanyOf("vehicle", id);
            if (companyId == null) return NotFound();
            var vehicle = session.Get<Vehicle>(id);
            var name = string.Join(" ", new[] { vehicle.LicensePlate, vehicle.Vin }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var work = Count("select count(*) from domain.work where vehicleid = :id", id);
            if (work > 0)
                throw new UserException($"Cannot delete the vehicle {name}: {(work == 1 ? "1 work order uses" : $"{work} work orders use")} it. Delete those first, or keep the vehicle.");
            var details = string.Join("; ", new[] { vehicle.Title, vehicle.Owner?.Name }.Where(x => !string.IsNullOrWhiteSpace(x)));
            session.Delete(vehicle); // its ownership history goes with it
            session.Flush();
            await audit.WriteAsync(this.TenantName(), this.CurrentAccount()?.UserName ?? this.UserName(), "vehicle.delete", name, details, companyId);
            return Ok();
        }

        /// <summary>The company of the row when the signed in administrator may change it (and works with that company from now on).</summary>
        private Guid? CompanyOf(string table, Guid id)
        {
            var value = session.CreateSQLQuery(SqlDialect.Current.Sql($"select company_id from domain.{table} where id = :id"))
                .SetParameter("id", id).List<object>().FirstOrDefault();
            if (value == null) return null;
            var companyId = value is Guid g ? g : Guid.Parse(value.ToString()!);
            if (!IsSuperAdmin && companyId != this.CurrentAccount().CompanyId) return null;
            companies.SwitchTo(companyId);
            return companyId;
        }

        private long Count(string sql, Guid id) =>
            Convert.ToInt64(session.CreateSQLQuery(SqlDialect.Current.Sql(sql)).SetParameter("id", id).UniqueResult());
    }
}
