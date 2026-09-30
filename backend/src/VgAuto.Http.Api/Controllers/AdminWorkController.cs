using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NHibernate;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Application.Extensions;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Application.Signing;
using VgAuto.Core.Domain;
using VgAuto.Http.Api.Models;

namespace VgAuto.Http.Api.Controllers
{
    /// <summary>
    /// The work of every company for super administrators: list, details with the documents, PDFs, and deleting a work
    /// with everything in it, also when its estimates were sent or signed and it has an invoice (audit logged).
    /// </summary>
    [TenantRateLimit]
    [Authorize(Policy = "ServerSidePolicy")]
    [RequireSuperAdmin]
    [Route("api/admin/works")]
    [ApiController]
    public class AdminWorkController : ControllerBase
    {
        private readonly ISession session;
        private readonly IRepository repository;
        private readonly ICompanyScope companies;
        private readonly IAdminAuditLog audit;
        private readonly IEstimateSignatures signatures;
        private readonly ISignatureLinkRepository signatureLinks;
        private readonly IPdfGenerator pdfGenerator;
        private readonly IConfiguration configuration;

        public AdminWorkController(ISession session, IRepository repository, ICompanyScope companies, IAdminAuditLog audit,
            IEstimateSignatures signatures, ISignatureLinkRepository signatureLinks, IPdfGenerator pdfGenerator, IConfiguration configuration)
        {
            this.session = session;
            this.repository = repository;
            this.companies = companies;
            this.audit = audit;
            this.signatures = signatures;
            this.signatureLinks = signatureLinks;
            this.pdfGenerator = pdfGenerator;
            this.configuration = configuration;
        }

        // ---------------------------------------------------------------- models

        /// <summary>A row of the list; Code is the readable work number (RP_TF_2019_HC_2026_09_28_15).</summary>
        public class WorkRow
        {
            public Guid Id { get; set; }
            public Guid CompanyId { get; set; }
            public string CompanyName { get; set; }
            public int Number { get; set; }
            public DateTime StartedOn { get; set; }
            public string ClientName { get; set; }
            public int? VehicleYear { get; set; }
            public string VehicleManufacturer { get; set; }
            public string VehicleModel { get; set; }
            public string LicensePlate { get; set; }
            public bool HasRepairs { get; set; }
            public int? InvoiceNumber { get; set; }
            public string InvoiceCode { get; set; }
            public bool InvoiceSent { get; set; }
            public bool InvoicePaid { get; set; }
            public int Offers { get; set; }
            public int SentOffers { get; set; }
            public int SignedOffers { get; set; }
            public string UserStatus { get; set; }

            public string Code => WorkCode.Format(HasRepairs || InvoiceNumber != null, ClientName, VehicleYear, VehicleManufacturer, VehicleModel,
                StartedOn, Number.ToString(System.Globalization.CultureInfo.InvariantCulture));
            public string Status => InvoiceNumber != null ? "completed" : UserStatus?.ToLowerInvariant();
        }

        public record DocumentDto(Guid Id, string Kind, string Code, DateTime IssuedOn, string IssuedBy, DateTime? SentOn, string SentTo,
            DateTime? AcceptedOn, string SignedBy, DateTime? SignedOn, string Total, bool? Paid);

        public record WorkDetailDto(Guid Id, Guid CompanyId, string CompanyName, string Code, int Number, DateTime StartedOn, string StartedBy,
            string Status, string ClientName, string ClientEmail, string Vehicle, string LicensePlate, string Notes,
            int RepairJobs, IReadOnlyList<DocumentDto> Documents);

        // ---------------------------------------------------------------- list and details

        [HttpGet]
        public PagedResult<WorkRow> List(string searchText, Guid? companyId, int limit = 50, int offset = 0)
        {
            var d = SqlDialect.Current;
            var query = repository
                .PageQuery<WorkRow>(null, limit, offset, true)
                .Sortable(new Dictionary<string, string>(), "w.changedon");
            if (companyId != null) query.ForCompany("w.company_id", companyId.Value);
            return query
                .FilterBy(searchText)
                .SearchFields(d.CastToText("w.number"), "p.firstname", "p.lastname", "l.name", "v.regnr", "v.vin", "v.producer", "v.model",
                    "coalesce(r.name, c.name)", "ip.code")
                .SelectSql($@"select
                        w.id, w.company_id as companyid, coalesce(r.name, c.name) as companyname, w.number, w.startedon,
                        concat_ws(' ', p.firstname, p.lastname, l.name) as clientname,
                        v.year as vehicleyear, v.producer as vehiclemanufacturer, v.model as vehiclemodel, v.regnr as licenseplate,
                        exists (select 1 from domain.repairjob j where j.workid = w.id) as hasrepairs,
                        i.number as invoicenumber, ip.code as invoicecode,
                        (ip.senton is not null) as invoicesent, coalesce(i.ispaid, false) as invoicepaid,
                        (select count(*) from domain.offer o where o.workid = w.id) as offers,
                        (select count(*) from domain.offer o inner join domain.pricing op on op.id = o.estimateid
                          where o.workid = w.id and op.senton is not null) as sentoffers,
                        (select count(*) from domain.offer o inner join domain.estimate_signature s on s.estimate_id = o.estimateid
                          where o.workid = w.id) as signedoffers,
                        w.userstatus
                      from domain.work w
                      inner join domain.company c on c.id = w.company_id
                      left join tenant_config.requisites r on r.company_id = w.company_id
                      left join domain.privateclient p on p.id = w.clientid
                      left join domain.legalclient l on l.id = w.clientid
                      left join domain.vehicle v on v.id = w.vehicleid
                      left join domain.invoice i on i.id = w.invoiceid
                      left join domain.pricing ip on ip.id = w.invoiceid")
                .ToResult();
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<WorkDetailDto>> Get(Guid id)
        {
            var work = Load(id);
            if (work == null) return NotFound();
            var currency = work.Invoice?.Currency;
            var documents = new List<DocumentDto>();
            foreach (var offer in work.Offers.Where(o => o.Estimate != null).OrderBy(o => o.OrderNr))
            {
                var e = offer.Estimate;
                var signature = await signatures.GetAsync(e.Id);
                documents.Add(new DocumentDto(e.Id, "estimate", e.GetNumber(), e.IssuedOn, e.Issuer?.Name, e.SentOn, e.Email,
                    offer.AcceptedOn, signature?.SignerName, signature?.SignedAt, Money(e), null));
            }
            if (work.Invoice is Invoice invoice)
            {
                documents.Add(new DocumentDto(invoice.Id, "invoice", invoice.GetNumber(), invoice.IssuedOn, invoice.Issuer?.Name, invoice.SentOn,
                    invoice.Email, null, null, null, Money(invoice), invoice.IsPaid));
            }
            var status = work.Invoice != null ? "completed" : work.UserStatus.ToString().ToLowerInvariant();
            return new WorkDetailDto(work.Id, work.CompanyId, CompanyName(work.CompanyId), work.Code, work.Number, work.StartedOn, work.Starter?.Name,
                status, work.Client?.Name, work.Client?.CurrentEmail, work.Vehicle?.Title, work.Vehicle?.LicensePlate, work.Notes,
                work.Jobs.Count(), documents);
        }

        /// <summary>An estimate or the invoice of the work as PDF.</summary>
        [HttpGet("{id:guid}/pdf/{documentId:guid}")]
        public async Task<IActionResult> Pdf(Guid id, Guid documentId)
        {
            var work = Load(id);
            var document = work == null ? null
                : work.Invoice?.Id == documentId ? (Pricing)work.Invoice
                : work.Offers.Select(o => o.Estimate).FirstOrDefault(e => e?.Id == documentId);
            if (document == null) return NotFound();
            var disposition = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline");
            disposition.SetHttpFileName(document.GetFileName());
            Response.Headers.ContentDisposition = disposition.ToString();
            return File(await pdfGenerator.Generate(document), "application/pdf");
        }

        // ---------------------------------------------------------------- delete

        /// <summary>
        /// Deletes the work with its offers, repair jobs, estimates (also sent or signed, with their signatures and links)
        /// and its invoice. An invoice that is not the company's last one leaves a gap in the invoice numbers.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var work = Load(id);
            if (work == null) return NotFound();
            var companyId = work.CompanyId;
            var code = work.Code;
            var estimates = work.Offers.Select(o => o.Estimate).Where(e => e != null).ToList();
            var invoice = work.Invoice;
            var signed = new List<string>();
            foreach (var estimate in estimates)
            {
                if (await signatures.GetAsync(estimate.Id) != null) signed.Add(estimate.GetNumber());
            }
            var details = string.Join("; ", new[]
            {
                CompanyName(companyId),
                estimates.Count > 0 ? $"{estimates.Count} estimate(s) ({estimates.Count(e => e.SentOn != null)} sent)" : null,
                signed.Count > 0 ? "signed: " + string.Join(", ", signed) : null,
                invoice != null ? $"invoice {invoice.GetNumber()} (no. {((Invoice)invoice).Number}){(invoice.SentOn != null ? ", sent" : "")}" : null,
            }.Where(x => x != null));

            // the work first (its offers and repair jobs go with it), then the documents it referred to
            session.Delete(work);
            session.Flush();
            foreach (var estimate in estimates)
            {
                await signatures.DeleteAsync(estimate.Id);
                session.Delete(estimate);
            }
            if (invoice != null) session.Delete(invoice);
            session.Flush();

            foreach (var document in estimates.Cast<Pricing>().Append(invoice).Where(x => x != null)) DeletePdf(document);
            await signatureLinks.DeleteForEstimatesAsync(estimates.Select(e => e.Id).ToList());
            await audit.WriteAsync(this.TenantName(), this.CurrentAccount()?.UserName ?? this.UserName(), "work.delete", code, details, companyId);
            return Ok();
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>The work in its company (the session is switched to it); null when there is none.</summary>
        private Work Load(Guid id)
        {
            var companyId = session.CreateSQLQuery(SqlDialect.Current.Sql("select company_id from domain.work where id = :id"))
                .SetParameter("id", id)
                .List<object>()
                .Select(x => x is Guid g ? g : Guid.Parse(x.ToString()!))
                .Cast<Guid?>()
                .FirstOrDefault();
            if (companyId == null) return null;
            companies.SwitchTo(companyId.Value);
            return session.Get<Work>(id);
        }

        private string CompanyName(Guid companyId) =>
            session.CreateSQLQuery(SqlDialect.Current.Sql(
                    "select coalesce(r.name, c.name) from domain.company c left join tenant_config.requisites r on r.company_id = c.id where c.id = :id"))
                .SetParameter("id", companyId)
                .List<object>()
                .FirstOrDefault()?.ToString();

        private static string Money(Pricing pricing) =>
            Currencies.Format(pricing.GetTotal(true), Currencies.Normalize(pricing.Currency ?? "CAD"));

        private void DeletePdf(Pricing pricing)
        {
            var directory = configuration["PdfDirectory"];
            if (string.IsNullOrWhiteSpace(directory)) directory = Path.Combine(AppContext.BaseDirectory, "pdf");
            var file = new FileInfo(Path.Combine(directory, pricing.GetFileName()));
            if (file.Exists) file.Delete();
        }
    }
}
