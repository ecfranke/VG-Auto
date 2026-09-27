using System;
using System.Data;
using System.Threading.Tasks;
using VgAuto.Core.Application;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Model;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Domain;
using VgAuto.Http.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VgAuto.Http.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DemoController : ControllerBase
    {
        private readonly ILogger<DemoController> _logger; 
        private readonly IOptions<JwtOptions> _jwtOptions;
        private readonly DbOptions _dbOptions;
        private readonly IDemoSetupService _demoSetupService;

        public DemoController(
              ILogger<DemoController> logger,
              IConfiguration configuration,
              IOptions<JwtOptions> jwtOptions,
              IOptions<DbOptions> dbOptions,
              IDemoSetupService demoSetupService)
        {
            _logger = logger; 
            _jwtOptions = jwtOptions;
            _dbOptions = dbOptions.Value;
            _demoSetupService = demoSetupService;
            _demoEnabled = configuration.GetValue("Demo:Enabled", false);
        }
        private readonly bool _demoEnabled;

        [AllowAnonymous]
        [DemoRateLimit]
        [HttpPost("setup")]
        public async Task<ActionResult<DemoSetupResponse>> SetupDemo([FromBody] DemoSetupRequest request)
        {
            if (!_demoEnabled)
            {
                return NotFound();
            }
            if (_dbOptions.MultiTenancy?.Enabled != true)
            {
                return BadRequest("Demo setup requires multi-tenancy to be enabled");
            }

            try
            {
                // Use the demo setup service to create a new tenant with sample data
                var (username, password, tenantName) = await _demoSetupService.CreateDemoTenant(request.CompanyName);
                 
                var response = new DemoSetupResponse
                {
                    Username = username,
                    Password = password  
                };

                _logger.LogInformation("Created demo instance with tenant name {TenantName}", tenantName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create demo instance: {Error}", ex.Message);
                return StatusCode(500, "Failed to create demo instance");
            }
        }
    }
}