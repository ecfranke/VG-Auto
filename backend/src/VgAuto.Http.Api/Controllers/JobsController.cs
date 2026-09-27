using VgAuto.Core.Application.Extensions;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Domain;
using VgAuto.Core.Persistence;
using VgAuto.Http.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace VgAuto.Http.Api.Controllers
{
    [TenantRateLimit]
    [Authorize(Policy = "ServerSidePolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : BaseController<RepairJobDto, RepairJob>
    {
        public JobsController(IRepository repository) : base(repository)
        {
        }
    }
}
