using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Carmasters.Core.Application;
using Carmasters.Core.Application.Authorization;
using Carmasters.Core.Application.Configuration;
using Carmasters.Core.Application.Database;
using Carmasters.Core.Application.Model;
using Carmasters.Core.Application.RateLimiting;
using Carmasters.Http.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Carmasters.Http.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository repository;
        private readonly ILogger<UsersController> logger;
        private readonly IOptions<JwtOptions> jwtOptions;
        public UsersController(IUserRepository repository, IOptions<JwtOptions> jwtOptions, ILogger<UsersController> logger)
        {
            this.repository = repository;
            this.logger = logger;
            this.jwtOptions = jwtOptions;
        }

        [AllowAnonymous, LimitRequests(MaxRequests = 60, TimeWindow = 60)]
        [HttpGet("profilepicture/{jwt?}")]
        public IActionResult GetProfilePicture(string jwt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jwt)) return File(Array.Empty<byte>(), "image/jpeg");
                var jwtToken = AppJwtToken.LoadJwt(jwtOptions.Value, jwt);
                var tenantName = jwtToken.Claims.First(x => x.Type == ClaimTypes.Spn || x.Type == "spn").Value;
                var empId = Guid.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.UserData || x.Type == "userdata").Value);
                var user = repository.GetBy(new UserIdentifier(tenantName, empId));
                if (user?.ProfileImage == null) return File(Array.Empty<byte>(), "image/jpeg");
                return File(user.ProfileImage, "image/jpeg");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot resolve user picture");
                return File(Array.Empty<byte>(), "image/jpeg");
            }
        }

        [TenantRateLimit]
        [Authorize(Policy = "ServerSidePolicy")]
        [HttpPost("extendsession")]
        public IActionResult ExtendSession()
        {
            logger.LogInformation("Extending user session for user {name}", User.Identity?.Name);
            var jwt = AppJwtToken.Generate(jwtOptions.Value, HttpContext.User);
            return Ok(jwt);
        }
    }
}
