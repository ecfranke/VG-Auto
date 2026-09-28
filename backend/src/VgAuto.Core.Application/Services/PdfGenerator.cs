using VgAuto.Core.Application;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Model;
using VgAuto.Core.Application.Printing;
using VgAuto.Core.Domain;
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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using NHibernate.Criterion;

using System.Threading;

namespace VgAuto.Core.Application.Services
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
                PricingOptions = pricingOptions,
                TaxIdLabel = TaxRegions.TaxIdLabel(pricingOptions.Taxes?.Country, pricingOptions.Taxes?.Region),
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
            try { Directory.CreateDirectory(pdfDirectory); }
            catch (Exception ex) { logger.LogWarning(ex, "PdfDirectory {dir} cannot be created", pdfDirectory); }
            var pdfLocalFile = new FileInfo(Path.Combine(pdfDirectory, pricing.GetFileName()));
            try
            {
                stream = await Print(pricing);
            }
            catch (UserException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Creating the PDF {file} failed", pricing.GetFileName());
                throw new UserException($"The PDF could not be created: {ex.Message}");
            }
            using (stream)
            {
                var pdfBytes = stream.ToArray();
                try
                {
                    // the copy on disk is only kept for reference: a write error must not stop the document
                    if (pdfLocalFile.Exists) pdfLocalFile.Delete();
                    File.WriteAllBytes(pdfLocalFile.FullName, pdfBytes);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Saving {file} failed", pdfLocalFile.FullName);
                }
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

                // a browser downloaded before (by the installer or an earlier request)
                var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = downloadPath });
                var downloaded = browserFetcher.GetInstalledBrowsers().FirstOrDefault();
                if (downloaded != null)
                {
                    _executablePath = browserFetcher.GetExecutablePath(downloaded.BuildId);
                    return;
                }

                // a browser installed on the server
                var local = InstalledBrowsers().FirstOrDefault(File.Exists);
                if (local != null)
                {
                    _executablePath = local;
                    logger.LogInformation("Pdf browser: {path}", local);
                    return;
                }

                try
                {
                    var stableVersion = await browserFetcher.DownloadAsync(BrowserTag.Stable);
                    _executablePath = browserFetcher.GetExecutablePath(stableVersion.BuildId);
                    logger.LogInformation("Puppeteer browser: {path}", _executablePath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Downloading Chrome for the PDF renderer into {path} failed", downloadPath);
                    throw new UserException(PdfUnavailable);
                }
            }
            finally
            {
                browserGate.Release();
            }
        }

        private const string PdfUnavailable =
            "The PDF could not be created: no browser is available on the server. " +
            "Run \"vgauto pdf-setup\" on the server, or set PuppeteerExecutablePath to an installed Chrome or Chromium.";

        /// <summary>Usual locations of Chrome, Chromium and Edge.</summary>
        private static IEnumerable<string> InstalledBrowsers()
        {
            if (OperatingSystem.IsWindows())
            {
                foreach (var root in new[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramFiles(x86)") })
                {
                    if (string.IsNullOrEmpty(root)) continue;
                    yield return Path.Combine(root, @"Google\Chrome\Application\chrome.exe");
                    yield return Path.Combine(root, @"Microsoft\Edge\Application\msedge.exe");
                }
                yield break;
            }
            if (OperatingSystem.IsMacOS())
            {
                yield return "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
                yield return "/Applications/Chromium.app/Contents/MacOS/Chromium";
                yield return "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge";
                yield break;
            }
            yield return "/usr/bin/google-chrome-stable";
            yield return "/usr/bin/google-chrome";
            yield return "/usr/bin/chromium";
            yield return "/usr/bin/chromium-browser";
            yield return "/usr/bin/microsoft-edge";
        }

        /// <summary>Makes sure a browser is available (used by the installer: "--pdf-setup").</summary>
        public async Task<string> EnsureBrowserAsync()
        {
            await PreparePuppeteerAsync();
            return _executablePath;
        }

        private async Task<MemoryStream> Print(Pricing pricing )
        {
             
            var html = await bodyHtmlGenerator.Generate(pricing); 

            await PreparePuppeteerAsync();

            IBrowser launched;
            try
            {
                launched = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" },
                    ExecutablePath = _executablePath
                });
            }
            catch (Exception ex)
            {
                // usually missing system libraries (libnss3, libgbm1 ...) on a minimal server
                logger.LogError(ex, "Starting the PDF browser {path} failed", _executablePath);
                throw new UserException("The PDF could not be created: the browser on the server does not start (missing system libraries?). " +
                    "Run \"vgauto pdf-setup\" on the server. Details are in the server log.");
            }
            await using var browser = launched;
             
            var page = await browser.NewPageAsync(); 

            await page.SetViewportAsync(new ViewPortOptions() { DeviceScaleFactor = 1, Width = 1440, Height = 2880, IsMobile = false, HasTouch = false });
            await page.SetContentAsync(html, options: new NavigationOptions() { WaitUntil = new [] { WaitUntilNavigation.Load  } });
            // the styles are read from wwwroot directly (no request to the API itself, which may sit behind a proxy)
            foreach (var css in new[] { "tailwind.css", "print.css" })
            {
                var file = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), css);
                if (File.Exists(file))
                    await page.AddStyleTagAsync(new AddTagOptions { Content = await File.ReadAllTextAsync(file) });
                else
                    await page.AddStyleTagAsync($"{serverUri.Scheme}://localhost:{serverUri.Port}/{css}");
            }
           
             
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
