using System.Linq;
using Carmasters.Core.Application.RateLimiting;
using Carmasters.Core.Application.Services;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHibernate;

namespace Carmasters.Http.Api.Controllers
{
    [TenantRateLimit]
    [Authorize(Policy = "ServerSidePolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class QueryController : ControllerBase
    {
        private readonly ISession session;

        public QueryController(ISession session)
        {
            this.session = session;
        }

        /// <summary>Global quick search. Every word of the search text must appear in the result name.</summary>
        [HttpGet("{searchText}")]
        public dynamic Get(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return new dynamic[0];

            var d = SqlDialect.Current;
            var parameters = new DynamicParameters();
            var conditions = new WildcardTokens(searchText).AllTokens()
                .Take(10)
                .Select((word, i) =>
                {
                    parameters.Add("w" + i, SqlDialect.ContainsPattern(word));
                    return d.ILike("name", "@w" + i);
                })
                .ToArray();
            if (conditions.Length == 0) return new dynamic[0];

            var sql = d.Sql($@"select * from ( 
                        select id,'Client' as resourcename, concat_ws(' ',firstname,lastname) as name,'klient' as controller from domain.privateclient
                        union all
                        select id,'Client' as resourcename,concat_ws(' ',name,(case when regnr is null or regnr='' then null else {d.Concat("'('", "regnr", "')'")} end)) as name,'klient' as controller from domain.legalclient
                        union all
                        select id, 'Vehicle' as resourcename,concat_ws(' ',regnr,(case when vin is null or vin='' then null else {d.Concat("'('", "vin", "')'")} end)) as name,'soiduk' as controller  from domain.vehicle  
                        union all
                        select id,'Work nr. ' as resourcename, {d.CastToText("number")} as name, 'too' as controller from domain.work
                        union all
                        select work.id, 'Invoice nr. ' as resourcename, {d.CastToText("invoice.number")} as name,'too' as controller from domain.invoice inner join domain.work on work.invoiceid = invoice.id
                        union all
                        select work.id, 'Estimate nr. ' as resourcename, estimate.number as name,'too' as controller from domain.offer inner join domain.work on work.id = offer.workid inner join domain.estimate on estimate.id = offer.estimateid
                        union all
                        select id, 'Sparepart' as resourcename , code as name, 'varuosa' as controller from domain.sparepart
                        union all
                        select id, 'Employer' as resourcename,concat_ws(' ',firstname,lastname) as name,'tootaja' as controller from domain.employee 
                        ) results   
                        where {string.Join(" and ", conditions)}
                        limit 10");

            return session.Connection.Query(sql, parameters).ToList();
        }
    }
}
