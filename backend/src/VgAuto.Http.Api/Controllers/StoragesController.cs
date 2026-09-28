using VgAuto.Core.Application.Extensions;
﻿using System;
using System.Linq;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Persistence;
using VgAuto.Http.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace VgAuto.Http.Api.Controllers
{
    [TenantRateLimit]
    [Authorize(Policy = "ServerSidePolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class StoragesController : BaseController<StorageDto, Core.Domain.Storage>
    {

        public StoragesController(Core.Domain.IRepository repository) : base(repository)
        {
        }
        [HttpGet()]
        public virtual ActionResult Get()
        {
            var locations = repository.GetConnection()
               .Query(SqlDialect.Current.Sql(@"select id,name from domain.storage where company_id = @companyId"), new { companyId = this.CompanyId() }).Select(x =>
                new
                {
                    Id = x.id,
                    Name = x.name
                }).ToArray();

            return new JsonResult(locations);
        }

        private static readonly System.Collections.Generic.Dictionary<string, string> SortColumns = new()
        {
            ["id"] = "id",
            ["name"] = "name",
            ["address"] = "address",
        };

        [HttpGet("page")]
        public PagedResult<StorageDto> GetPage(string searchText, string orderby, int limit, int offset, bool desc)
        {
            return
              repository
                .PageQuery<StorageDto>(orderby, limit, offset, desc)
                .FilterBy(searchText)
                .SearchFields("name", "address", "description")
                .ForCompany("company_id", this.CompanyId())
                .Sortable(SortColumns, "id")
                .SelectSql(@"select * from domain.storage")
                .ToResult();
        }

        protected override StorageDto Map(Core.Domain.Storage entity) => VgAuto.Http.Api.Model.DtoMapper.ToDto(entity);

        protected override Core.Domain.Storage CreateFrom(StorageDto model)
        {
            return new Core.Domain.Storage(  model.Name, model.Address, model.Description, model.IntroducedAt);
        }

        protected override void Edit(Core.Domain.Storage entity, StorageDto model)
        {
            entity.ChangeName(model.Name);
            entity.ChangeAddress(model.Address);
            entity.ChangeDescription(model.Description);
        }

    }
}
