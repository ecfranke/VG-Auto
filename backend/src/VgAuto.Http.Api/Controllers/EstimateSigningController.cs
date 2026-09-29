using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHibernate;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Application.Signing;
using VgAuto.Core.Domain;

namespace VgAuto.Http.Api.Controllers
{
    /// <summary>
    /// The page where a client reviews and signs an estimate from the link in its email, without signing in.
    /// Called by the Next.js server only (every call carries the shared server secret). The link names the tenant,
    /// company and estimate; the token itself is only stored as a hash.
    /// </summary>
    [ApiController]
    [Route("api/public/estimates/{token}")]
    [AllowAnonymous]
    public class EstimateSigningController : ControllerBase
    {
        private readonly SignatureLinks links;
        private readonly JwtOptions jwtOptions;
        private readonly ILogger<EstimateSigningController> logger;

        // the database session and everything using it are resolved after the link set the tenant (see Open)
        public EstimateSigningController(SignatureLinks links, IOptions<JwtOptions> jwtOptions, ILogger<EstimateSigningController> logger)
        {
            this.links = links;
            this.jwtOptions = jwtOptions.Value;
            this.logger = logger;
        }

        public record SecretDto(string ServerSecret);

        /// <summary>ClientIp: the address of the client's browser, as the Next.js server saw it.</summary>
        public record SignDto(string ServerSecret, string Name, string Signature, bool Accepted, string ClientIp = null);

        /// <summary>Status: open (can be signed), signed or expired. Html: the estimate as printed (with the signature once signed).</summary>
        public record SigningDto(string Status, string CompanyName, string CompanyEmail, string CompanyPhone, string Code,
            string ClientName, string VehicleTitle, DateTime IssuedOn, string Total, DateTime ValidUntil,
            string SignerName, DateTime? SignedAt, string Html);

        [LimitRequests(MaxRequests = 30, TimeWindow = 60)]
        [HttpPost("view")]
        public async Task<IActionResult> Show(string token, SecretDto model)
        {
            if (!SecretOk(model?.ServerSecret)) return await Reject();
            var opened = await Open(token);
            if (opened.Error != null) return opened.Error;
            return Ok(await ToDto(opened.Link, opened.Estimate));
        }

        [LimitRequests(MaxRequests = 10, TimeWindow = 60)]
        [HttpPost("sign")]
        public async Task<IActionResult> Sign(string token, SignDto model)
        {
            if (!SecretOk(model?.ServerSecret)) return await Reject();
            var opened = await Open(token);
            if (opened.Error != null) return opened.Error;
            var (link, estimate, offer) = (opened.Link, opened.Estimate, opened.Offer);

            if (link.ExpiresAt < DateTime.UtcNow) return Gone();
            var signatures = Get<IEstimateSignatures>();
            if (await signatures.GetAsync(estimate.Id) != null) return Conflict(Message("This estimate has been signed already."));

            var name = model.Name?.Trim();
            if (string.IsNullOrEmpty(name)) return BadRequest(Message("Please enter your name."));
            if (name.Length > 200) return BadRequest(Message("The name is too long."));
            if (!model.Accepted) return BadRequest(Message("Please confirm that you accept the estimate."));
            string image;
            try
            {
                image = SignatureImage.Validate(model.Signature);
            }
            catch (UserException ex)
            {
                return BadRequest(Message(ex.Message));
            }

            var session = Get<ISession>();
            using (var transaction = session.BeginTransaction())
            {
                var ip = model.ClientIp?.Trim();
                if (!await signatures.AddAsync(new EstimateSignature(estimate.Id, name, DateTime.UtcNow, image), link.CompanyId,
                        string.IsNullOrEmpty(ip) ? null : ip.Length > 64 ? ip[..64] : ip))
                {
                    return Conflict(Message("This estimate has been signed already."));
                }
                // the work comes up as recently changed in the workshop
                offer.Work.Changed();
                session.Update(offer.Work);
                transaction.Commit();
            }
            logger.LogInformation("Estimate {code} signed online by {name}", estimate.GetNumber(), name);
            return Ok(await ToDto(link, estimate));
        }

        /// <summary>The estimate as PDF, with the signature once signed.</summary>
        [LimitRequests(MaxRequests = 10, TimeWindow = 60)]
        [HttpPost("pdf")]
        public async Task<IActionResult> Pdf(string token, SecretDto model)
        {
            if (!SecretOk(model?.ServerSecret)) return await Reject();
            var opened = await Open(token);
            if (opened.Error != null) return opened.Error;
            var estimate = opened.Estimate;
            var signed = await Get<IEstimateSignatures>().GetAsync(estimate.Id) != null;
            var disposition = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
            disposition.SetHttpFileName(signed ? estimate.GetFileName().Replace(".pdf", "_signed.pdf") : estimate.GetFileName());
            Response.Headers.ContentDisposition = disposition.ToString();
            return File(await Get<IPdfGenerator>().Generate(estimate), "application/pdf");
        }

        private record Opened(SignatureLink Link, Estimate Estimate, Offer Offer, IActionResult Error);

        /// <summary>The link of the token, its estimate and offer; an error result when the link is unknown or the estimate is gone.</summary>
        private async Task<Opened> Open(string token)
        {
            var link = await links.FindAsync(token);
            if (link == null) return new Opened(null, null, null, NotFound(Message("This link is not valid.")));

            // the request has no signed in user: the link names the tenant (its database) and the company
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Spn, link.TenantName) }, "SignatureLink"));
            var session = Get<ISession>();
            Get<ICompanyScope>().SwitchTo(link.CompanyId);

            var estimate = session.Get<Estimate>(link.EstimateId);
            var offer = estimate == null ? null : session.QueryOver<Offer>().Where(x => x.Estimate.Id == estimate.Id).SingleOrDefault();
            if (offer == null || estimate.CompanyId != link.CompanyId)
                return new Opened(link, null, null, NotFound(Message("This estimate is not available any more. Please contact the workshop.")));
            return new Opened(link, estimate, offer, null);
        }

        private async Task<SigningDto> ToDto(SignatureLink link, Estimate estimate)
        {
            var config = Get<ITenantConfigService>();
            var requisites = await config.GetRequisitesAsync();
            var pricing = await config.GetPricingAsync();
            var currency = Currencies.Normalize(estimate.Currency ?? pricing.Currency);
            var signature = await Get<IEstimateSignatures>().GetAsync(estimate.Id);
            var status = signature != null ? "signed" : link.ExpiresAt < DateTime.UtcNow ? "expired" : "open";
            var html = await Get<PricingBodyHtmlGenerator>().Generate(estimate);
            return new SigningDto(status, requisites.Name, requisites.Email, requisites.Phone, estimate.GetNumber(),
                estimate.PartyName, estimate.VehicleTitle, estimate.IssuedOn, Currencies.Format(estimate.GetTotal(true), currency),
                link.ExpiresAt, signature?.SignerName, signature?.SignedAt, html);
        }

        private T Get<T>() => HttpContext.RequestServices.GetRequiredService<T>();

        private static object Message(string message) => new { isUserError = true, exceptionMessage = message };

        private IActionResult Gone() => StatusCode(410, Message("This link has expired. Please ask the workshop to send the estimate again."));

        private bool SecretOk(string secret) => AppJwtToken.SecretsEqual(jwtOptions.ConsumerSecret, secret);

        private static async Task<IActionResult> Reject()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            return new UnauthorizedResult();
        }
    }
}
