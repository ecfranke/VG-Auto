using Carmasters.Core.Application;
using Carmasters.Core.Application.Configuration;
using Carmasters.Core.Application.Model;
using Carmasters.Core.Application.Printing;
using Carmasters.Core.Domain;
using FluentNHibernate.Conventions.Inspections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using NHibernate.Criterion;

using System.Threading;

namespace Carmasters.Core.Application.Services
{
    public interface IPdfGenerator
    {
        Task<byte[]> Generate(Pricing pricing);
        IPricingHtmlGenerator GetBodyGenerator();
        IPricingHtmlGenerator GetFooterGenerator();
    }

    public interface IPricingHtmlGenerator
    {
        Task<string> Generate(Pricing pricing); 
    }

    public class PricingFooterHtmlGenerator : PricingHtmlBaseGenerator
    {
        public PricingFooterHtmlGenerator(
            ITemplateService templateService,
            ITenantConfigService tenantConfigService)
            : base(templateService, tenantConfigService)
        {
        }

        public override async Task<string> Generate(Pricing pricing)
        {
            var model = await CreatePricingModelAsync(pricing);
            var footerHtml = await templateService.RenderAsync("Print/Footer", model);
            return footerHtml;
        }
    }


    public class PricingBodyHtmlGenerator : PricingHtmlBaseGenerator
    {
        public PricingBodyHtmlGenerator(
            ITemplateService templateService,
            ITenantConfigService tenantConfigService)
            : base(templateService, tenantConfigService)
        {
        }

        public override async Task<string> Generate(Pricing pricing)
        {
            var model = await CreatePricingModelAsync(pricing);
            var html = await templateService.RenderAsync("Print/PricingOutput", model);
            return html;
        }
    }



    public abstract class PricingHtmlBaseGenerator : IPricingHtmlGenerator
    {
        protected readonly ITemplateService templateService;
        protected readonly ITenantConfigService tenantConfigService;

        public PricingHtmlBaseGenerator(
            ITemplateService templateService,
            ITenantConfigService tenantConfigService)
        {
            this.templateService = templateService;
            this.tenantConfigService = tenantConfigService;
        }

        protected async Task<PricingPrintModel> CreatePricingModelAsync(Pricing pricing)
        {
            var requisites = await tenantConfigService.GetRequisitesAsync();
            var pricingOptions = await tenantConfigService.GetPricingAsync();

            var model = new PricingPrintModel
            {
                Pricing = pricing,
                RequisitesOptions = requisites,
                PricingOptions = pricingOptions
            };

            return model;
        }

        public abstract Task<string> Generate(Pricing pricing);
    }
    //does not work in another assembly
    public class PdfGenerator : IPdfGenerator
    {
        private readonly IWebHostEnvironment env;
       
        private readonly IConfiguration configuration;
        private readonly PricingBodyHtmlGenerator bodyHtmlGenerator;
        private readonly PricingFooterHtmlGenerator footerHtmlGenerator;
        private readonly ILogger<PdfGenerator> logger;
        private readonly Uri serverUri;

        public PdfGenerator(IWebHostEnvironment env, IConfiguration configuration,
             PricingBodyHtmlGenerator bodyHtmlGenerator,
             PricingFooterHtmlGenerator footerHtmlGenerator,
             IServer server,
             ILogger<PdfGenerator> logger)
        {
            this.env = env;
           
            this.configuration = configuration;
            this.bodyHtmlGenerator = bodyHtmlGenerator;
            this.footerHtmlGenerator = footerHtmlGenerator;
            this.logger = logger;
            var addressFeature = server.Features.Get<IServerAddressesFeature>();
            // the css used in the PDF is loaded from this API itself
            var address = addressFeature?.Addresses.FirstOrDefault(a => a.StartsWith("http://")) ?? addressFeature?.Addresses.FirstOrDefault() ?? "http://localhost:15567";
            serverUri = new Uri(address.Replace("://+", "://localhost").Replace("://*", "://localhost").Replace("://0.0.0.0", "://localhost"));
            logger.LogDebug("Pdf service reachable at : " + serverUri); 
        }

        IPricingHtmlGenerator IPdfGenerator.GetBodyGenerator()
        {
            return bodyHtmlGenerator;
        }

        IPricingHtmlGenerator IPdfGenerator.GetFooterGenerator()
        {
            return footerHtmlGenerator;
        }

        public async Task<byte[]> Generate(Pricing pricing ) 
        {  
            var stream = default(MemoryStream);
            var pdfDirectory = configuration["PdfDirectory"];
            if (string.IsNullOrWhiteSpace(pdfDirectory)) pdfDirectory = Path.Combine(AppContext.BaseDirectory, "pdf");
            Directory.CreateDirectory(pdfDirectory);
            var pdfLocalFile = new FileInfo(Path.Combine(pdfDirectory, pricing.GetFileName()));
            stream = await Print(pricing);
            using (stream)
            {
                var pdfBytes = stream.ToArray();
                if (pdfLocalFile.Exists) pdfLocalFile.Delete();
                File.WriteAllBytes(pdfLocalFile.FullName, pdfBytes);
                return pdfBytes;
            }
        }

        private static string _executablePath;
        private static readonly SemaphoreSlim browserGate = new(1, 1);

        /// <summary>
        /// Uses PuppeteerExecutablePath (an installed Chrome/Chromium/Edge) when configured,
        /// otherwise downloads Chrome once into PuppeteerPath.
        /// </summary>
        private async Task PreparePuppeteerAsync()
        {
            if (!string.IsNullOrWhiteSpace(_executablePath)) return;
            await browserGate.WaitAsync();
            try
            {
                if (!string.IsNullOrWhiteSpace(_executablePath)) return;

                var installed = configuration["PuppeteerExecutablePath"];
                if (!string.IsNullOrWhiteSpace(installed))
                {
                    if (!File.Exists(installed)) throw new FileNotFoundException($"PuppeteerExecutablePath '{installed}' does not exist.");
                    _executablePath = installed;
                    return;
                }

                var downloadPath = configuration["PuppeteerPath"];
                if (string.IsNullOrWhiteSpace(downloadPath))
                    downloadPath = Path.Combine(AppContext.BaseDirectory, "puppeteer");
                var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = downloadPath });
                var stableVersion = await browserFetcher.DownloadAsync(BrowserTag.Stable);
                _executablePath = browserFetcher.GetExecutablePath(stableVersion.BuildId);
                logger.LogInformation("Puppeteer browser: {path}", _executablePath);
            }
            finally
            {
                browserGate.Release();
            }
        }

        private async Task<MemoryStream> Print(Pricing pricing )
        {
             
            var html = await bodyHtmlGenerator.Generate(pricing); 

            await PreparePuppeteerAsync();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" },
                ExecutablePath = _executablePath
            });
             
            var page = await browser.NewPageAsync(); 

            await page.SetViewportAsync(new ViewPortOptions() { DeviceScaleFactor = 1, Width = 1440, Height = 2880, IsMobile = false, HasTouch = false });
            await page.SetContentAsync(html, options: new NavigationOptions() { WaitUntil = new [] { WaitUntilNavigation.Load  } });
            var tailWindCss = $"{serverUri.Scheme}://localhost:{serverUri.Port}/tailwind.css";
            var printCss = $"{serverUri.Scheme}://localhost:{serverUri.Port}/print.css";
            await page.AddStyleTagAsync(tailWindCss);
            await page.AddStyleTagAsync(printCss);
           
             
            var pdfContent = await page.PdfStreamAsync(new PdfOptions
            {
                PrintBackground = false,
                Format = PaperFormat.A4, 
                DisplayHeaderFooter = false  
            });
            return (MemoryStream)pdfContent;
        } 
    }
}
