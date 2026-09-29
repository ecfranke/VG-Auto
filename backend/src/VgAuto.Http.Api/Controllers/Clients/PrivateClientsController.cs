using VgAuto.Core.Application.RateLimiting;
using VgAuto.Http.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace VgAuto.Http.Api.Controllers.Clients
{
    [TenantRateLimit]
    [Authorize(Policy = "ServerSidePolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class PrivateClientsController : BaseController<PrivateClientDto, Core.Domain.PrivateClient>
    {
        public PrivateClientsController(Core.Domain.IRepository repository) : base(repository)
        {
        }

        protected override void BeforeDelete(Core.Domain.PrivateClient client) => ClientsController.EnsureCanDelete(HttpContext, client);
        protected override PrivateClientDto Map(Core.Domain.PrivateClient entity) => VgAuto.Http.Api.Model.DtoMapper.ToDto(entity);

        protected override Core.Domain.PrivateClient CreateFrom(PrivateClientDto model)
        {
            var client = new Core.Domain.PrivateClient(model.IntroducedAt,
                                                 model.FirstName,
                                                 model.LastName,
                                                 ClientsController.CreateAddress(model.Address),
                                                 model.Phone,
                                                 model.IsAsshole,
                                                 model.Description,
                                                 model.PersonalCode);
            client.UsesEmail(model.EmailAddresses, model.CurrentEmail);
            return client;
        }

        protected override void Edit(Core.Domain.PrivateClient entity, PrivateClientDto model)
        {
            entity.Edit(model.FirstName,
                        model.LastName,
                        ClientsController.CreateAddress(model.Address),
                        model.Phone,
                        model.IsAsshole,
                        model.Description,
                        model.PersonalCode);
            entity.UsesEmail(model.EmailAddresses, model.CurrentEmail);
        }
    }
}
