using System.Threading.Tasks;
using VgAuto.Core.Application.Email;
using VgAuto.Core.Domain;
using Microsoft.Extensions.Logging;

namespace VgAuto.Core.Application.Services
{
    /// <summary>Sends an estimate or invoice as PDF attachment through the configured email transport (SMTP or Microsoft Graph).</summary>
    public class PricingPdfMailSender : IPricingSender
    {
        private readonly ITenantConfigService tenantConfigService;
        private readonly ILogger<PricingPdfMailSender> logger;
        private readonly IPdfGenerator pdfGenerator;
        private readonly IEmailSender emailSender;

        public PricingPdfMailSender(
            ILogger<PricingPdfMailSender> logger,
            IPdfGenerator pdfGenerator,
            ITenantConfigService tenantConfigService,
            IEmailSender emailSender)
        {
            this.tenantConfigService = tenantConfigService;
            this.logger = logger;
            this.pdfGenerator = pdfGenerator;
            this.emailSender = emailSender;
        }

        public async Task Send(Pricing pricing)
        {
            if (string.IsNullOrWhiteSpace(pricing.Email))
                throw new UserException("Cannot send an email, recipient email not provided.");

            try
            {
                EmailAddresses.Parse(pricing.Email); // check the addresses before the PDF is made
            }
            catch (EmailDeliveryException ex)
            {
                throw new UserException(ex.Message);
            }

            var requisites = await tenantConfigService.GetRequisitesAsync();
            var pricingConfig = await tenantConfigService.GetPricingAsync();

            var isInvoice = pricing is Invoice;
            var body = isInvoice ? pricingConfig.Invoice.EmailContent : pricingConfig.Estimate.EmailContent;

            var pdfBytes = await pdfGenerator.Generate(pricing);

            var message = new EmailMessage(pricing.Email, pricing.GetDisplayName(), body)
            {
                FromName = requisites.Name,
                ReplyTo = requisites.Email,
                FallbackFromAddress = requisites.Email,
            };
            message.Attachments.Add(new EmailAttachment(pricing.GetFileName(), "application/pdf", pdfBytes));

            try
            {
                await emailSender.SendAsync(message);
            }
            catch (EmailDeliveryException ex)
            {
                // shown to the user, the details are in the log
                throw new UserException(ex.Message);
            }
            logger.LogInformation("{type} {subject} sent via {transport}", isInvoice ? "Invoice" : "Estimate", message.Subject, emailSender.Name);
        }
    }
}
