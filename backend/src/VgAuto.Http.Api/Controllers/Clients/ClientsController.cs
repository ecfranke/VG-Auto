using VgAuto.Core.Application.Extensions;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Domain;
using VgAuto.Http.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using System;
using System.Linq;
using System.Reflection;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace VgAuto.Http.Api.Controllers.Clients
{
    [TenantRateLimit]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : BaseController<ClientPageDto, Client>
    {
        public ClientsController(IRepository repository) : base(repository)
        {
        }


        private static readonly System.Collections.Generic.Dictionary<string, string> SortColumns = new()
        {
            ["id"] = "c.id",
            ["name"] = "name",
            ["introducedat"] = "c.introducedat",
            ["phone"] = "c.phone",
            ["email"] = "email",
        };

        [HttpGet("page")]
        public PagedResult<ClientPageDto> GetPage(string searchText, string orderby, int limit, int offset, bool desc)
        {

            return
                repository
                  .PageQuery<ClientPageDto>(orderby, limit, offset, desc)
                  .FilterBy(searchText)
                  .SearchFields("p.firstname", "p.lastname", "l.name", "ce.address", "c.phone", "c.address")
                  .ForCompany("c.company_id", this.CompanyId())
                  .Sortable(SortColumns, "c.id")
                  .SelectSql(@"SELECT
                                    c.id, 
                                    concat_ws(' ',c.country,c.region,c.city,c.address,c.postalcode)  as address,
                                    ce.address AS email,  
                                    c.introducedat,
                                    (l.id IS NOT NULL) AS iscompany,
                                    concat_ws(' ',p.firstname,p.lastname,l.name)  as name,
                                    c.phone
                                FROM domain.client AS c
                                    left join domain.clientemail ce on ce.clientid = c.id and ce.isactive = true
                                    LEFT JOIN domain.legalclient AS l ON c.id = l.id
                                    LEFT JOIN domain.privateclient AS p ON c.id = p.id")
                  .ToResult();
        }

         
        [Authorize(Policy = "ServerSidePolicy")]
        public override ActionResult Get(Guid id)
        {
            var client = repository.Get<Client>(id, false);
            if (client is PrivateClient) return LocalRedirect("/api/privateclients/" + id);
            if (client is LegalClient) return LocalRedirect("/api/legalclients/" + id);
            return base.NotFound();
        }

        /// <summary>A client is only deleted when nothing refers to it; otherwise the user is told why.</summary>
        protected override void BeforeDelete(Client client) => EnsureCanDelete(HttpContext, client);

        public static void EnsureCanDelete(Microsoft.AspNetCore.Http.HttpContext context, Client client)
        {
            // through the NHibernate session: it runs inside the request's transaction (MySQL requires that)
            var session = (NHibernate.ISession)context.RequestServices.GetService(typeof(NHibernate.ISession));
            long Count(string sql) => Convert.ToInt64(session.CreateSQLQuery(VgAuto.Core.Application.Database.SqlDialect.Current.Sql(sql))
                .SetParameter("id", client.Id).UniqueResult());
            var work = Count("select count(*) from domain.work where clientid = :id");
            var vehicles = Count("select count(distinct vehicleid) from domain.vehicleregistration where ownerid = :id");
            if (work == 0 && vehicles == 0) return;

            var reasons = new System.Collections.Generic.List<string>();
            if (work > 0) reasons.Add(work == 1 ? "1 work order" : $"{work} work orders");
            if (vehicles > 0) reasons.Add(vehicles == 1 ? "1 vehicle" : $"{vehicles} vehicles");
            throw new UserException($"Cannot delete the client: it has {string.Join(" and ", reasons)}. Delete those first, or keep the client.");
        }

        public static AddressComponent CreateAddress(AddressDto addressDto)
        {
            return new AddressComponent(
               addressDto?.Street,
               addressDto?.Country,
               addressDto?.Region,
               addressDto?.City,
               addressDto?.PostalCode
            );
        }
    }
}
