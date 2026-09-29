using System.Linq;
﻿using System;
using System.Text;
using System.Threading.Tasks;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.DependencyInjection;
using VgAuto.Core.Domain;
using VgAuto.Core.Application.Extensions;
using VgAuto.Core.Application.Authorization;

namespace VgAuto.Http.Api.Controllers
{
    [TenantRateLimit]
    [Authorize(Policy = "ServerSidePolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class OptionsController : ControllerBase
    {
        private readonly ITenantConfigService tenantConfigService;
        private readonly DatabaseBackup backup;
        private readonly ILogger<OptionsController> logger;

        public OptionsController(
            ITenantConfigService tenantConfigService,
            DatabaseBackup backup,
            ILogger<OptionsController> logger)
        {
            this.tenantConfigService = tenantConfigService;
            this.backup = backup;
            this.logger = logger;
        }

        [HttpGet()]
        public async Task<ActionResult<AppOptions>> Get()
        {
            try
            {
                return await tenantConfigService.GetAppOptionsAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving tenant configuration");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve configuration");
            }
        }

        /// <summary>
        /// Saves the company settings. Everybody may change contact details, invoice and offer options;
        /// the company name, registration number, tax ID and currency are kept unless an administrator saves.
        /// </summary>
        [HttpPut]
        public async Task<ActionResult> Post([FromBody] AppOptions appOptions,
            [FromServices] VgAuto.Core.Application.Authorization.IAdminAuditLog audit)
        {
            if (appOptions?.Requisites == null || appOptions.Pricing?.Invoice == null || appOptions.Pricing.Estimate == null)
                throw new UserException("Incomplete settings.");

            var isAdmin = this.CurrentAccount()?.IsAdmin == true;
            if (!isAdmin)
            {
                var current = await tenantConfigService.GetAppOptionsAsync();
                appOptions = appOptions with
                {
                    Requisites = appOptions.Requisites with
                    {
                        Name = current.Requisites.Name,
                        RegNr = current.Requisites.RegNr,
                        KMKR = current.Requisites.KMKR,
                    },
                    Pricing = appOptions.Pricing with
                    {
                        Currency = null, // null keeps the current currency
                        // the place of registration is set by an administrator; tax names and rates stay editable
                        Taxes = appOptions.Pricing.Taxes == null ? null
                            : appOptions.Pricing.Taxes with { Country = current.Pricing.Taxes?.Country, Region = current.Pricing.Taxes?.Region },
                    },
                };
            }
            try
            {
                await tenantConfigService.SaveAppOptionsAsync(appOptions);
            }
            catch (UserException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving tenant configuration");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to save configuration");
            }
            await audit.WriteAsync(this.TenantName(), this.CurrentAccount()?.UserName ?? this.UserName(), "settings.update", null,
                isAdmin ? "company settings" : "company settings (contact details, invoice and offer options)", this.CompanyId());
            return Ok();
        }

        /// <summary>Currencies that can be chosen in the settings.</summary>
        [HttpGet("currencies")]
        public IActionResult Currencies() =>
            Ok(VgAuto.Core.Domain.Currencies.All.Select(c => new { c.Code, c.Name, Decimals = VgAuto.Core.Domain.Currencies.Decimals(c.Code), c.Culture }));

        /// <summary>Countries and provinces/states with their sales taxes (the settings fill in the taxes from them).</summary>
        [HttpGet("taxregions")]
        public IActionResult TaxRegions() => Ok(VgAuto.Core.Application.Configuration.TaxRegions.All);

        public record TestEmailDto(string To);

        /// <summary>Sends a test message through the company's email transport (its own or the built-in one).</summary>
        [RequireAdmin]
        [HttpPost("testemail")]
        public async Task<ActionResult> SendTestEmail([FromBody] TestEmailDto model,
            [FromServices] VgAuto.Core.Application.Email.ICompanyEmailSender emailSender,
            [FromServices] VgAuto.Core.Application.Authorization.IAdminAuditLog audit)
        {
            if (string.IsNullOrWhiteSpace(model?.To)) throw new UserException("Recipient is required.");
            var requisites = await tenantConfigService.GetRequisitesAsync();
            var message = new VgAuto.Core.Application.Email.EmailMessage(model.To, "Test email", $"This is a test email of {requisites.Name} sent by VG Auto.")
            {
                FromName = requisites.Name,
                ReplyTo = requisites.Email,
                FallbackFromAddress = requisites.Email,
            };
            string transport;
            try
            {
                transport = await emailSender.SendAsync(message);
            }
            catch (VgAuto.Core.Application.Email.EmailDeliveryException ex)
            {
                throw new UserException(ex.Message);
            }
            await audit.WriteAsync(this.TenantName(), this.CurrentAccount()?.UserName ?? this.UserName(), "email.test", model.To, transport, this.CompanyId());
            return Ok(new { transport });
        }

        [RequireAdmin]
        [HttpGet("dbdump")]
        public async Task<IActionResult> DumpDb()
        {
            var dbOptions = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<DbOptions>>().Value;
            var databaseName = dbOptions.MultiTenancy?.Enabled == true
                ? new MultiTenancyDbName(dbOptions, this.TenantName()).Value
                : dbOptions.Name;
            var script = await backup.Dump(databaseName);
            Response.Headers.Append("content-disposition", $"attachment;filename=dbdump{DateTime.Now:yyyyMMddHHmmss}.sql");

            return File(Encoding.UTF8.GetBytes(script), "application/octet-stream");
        }
    }
}