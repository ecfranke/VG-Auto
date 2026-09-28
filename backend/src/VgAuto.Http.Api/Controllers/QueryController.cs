using VgAuto.Core.Application.Extensions;
﻿using System.Linq;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHibernate;

namespace VgAuto.Http.Api.Controllers
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
            parameters.Add("companyId", this.CompanyId());

            var sql = d.Sql($@"select * from ( 
                        select pc.id,'Client' as resourcename, concat_ws(' ',firstname,lastname) as name,'klient' as controller, c.company_id from domain.privateclient pc inner join domain.client c on c.id = pc.id
                        union all
                        select lc.id,'Client' as resourcename,concat_ws(' ',name,(case when regnr is null or regnr='' then null else {d.Concat("'('", "regnr", "')'")} end)) as name,'klient' as controller, c.company_id from domain.legalclient lc inner join domain.client c on c.id = lc.id
                        union all
                        select id, 'Vehicle' as resourcename,concat_ws(' ',regnr,(case when vin is null or vin='' then null else {d.Concat("'('", "vin", "')'")} end)) as name,'soiduk' as controller, company_id from domain.vehicle  
                        union all
                        select id,'Work nr. ' as resourcename, {d.CastToText("number")} as name, 'too' as controller, company_id from domain.work
                        union all
                        select work.id, 'Invoice nr. ' as resourcename, {d.CastToText("invoice.number")} as name,'too' as controller, work.company_id from domain.invoice inner join domain.work on work.invoiceid = invoice.id
                        union all
                        select work.id, 'Estimate nr. ' as resourcename, estimate.number as name,'too' as controller, work.company_id from domain.offer inner join domain.work on work.id = offer.workid inner join domain.estimate on estimate.id = offer.estimateid
                        union all
                        select id, 'Sparepart' as resourcename , code as name, 'varuosa' as controller, company_id from domain.sparepart
                        union all
                        select id, 'Employer' as resourcename,concat_ws(' ',firstname,lastname) as name,'tootaja' as controller, company_id from domain.employee 
                        ) results   
                        where company_id = @companyId and {string.Join(" and ", conditions)}
                        limit 10");

            return session.Connection.Query(sql, parameters).ToList();
        }
    }
}
