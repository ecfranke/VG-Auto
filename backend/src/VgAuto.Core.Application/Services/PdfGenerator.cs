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
            ITenantConfigService tenantConfigService,
            VgAuto.Core.Application.Signing.IEstimateSignatures signatures)
            : base(templateService, tenantConfigService, signatures)
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
            ITenantConfigService tenantConfigService,
            VgAuto.Core.Application.Signing.IEstimateSignatures signatures)
            : base(templateService, tenantConfigService, signatures)
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
        private readonly VgAuto.Core.Application.Signing.IEstimateSignatures signatures;

        public PricingHtmlBaseGenerator(
            ITemplateService templateService,
            ITenantConfigService tenantConfigService,
            VgAuto.Core.Application.Signing.IEstimateSignatures signatures)
        {
            this.templateService = templateService;
            this.tenantConfigService = tenantConfigService;
            this.signatures = signatures;
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
                // an estimate the client signed online is printed with the signature
                Signature = pricing is Estimate && pricing.Id != Guid.Empty ? await signatures.GetAsync(pricing.Id) : null,
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
        private static System.Collections.Generic.IEnumerable<PuppeteerSharp.BrowserData.InstalledBrowser> SafeInstalled(BrowserFetcher fetcher)
        {
            try { return fetcher.GetInstalledBrowsers().ToList(); }
            catch { return Array.Empty<PuppeteerSharp.BrowserData.InstalledBrowser>(); }
        }

        /// <param name="forceShell">use (or download) chrome-headless-shell even when another browser is there (fallback after a failed start)</param>
        private async Task PreparePuppeteerAsync(bool forceShell = false)
        {
            if (!forceShell && !string.IsNullOrWhiteSpace(_executablePath)) return;
            await browserGate.WaitAsync();
            try
            {
                if (!forceShell && !string.IsNullOrWhiteSpace(_executablePath)) return;

                var installed = configuration["PuppeteerExecutablePath"];
                if (!forceShell && !string.IsNullOrWhiteSpace(installed))
                {
                    if (!File.Exists(installed)) throw new FileNotFoundException($"PuppeteerExecutablePath '{installed}' does not exist.");
                    _executablePath = installed;
                    return;
                }

                var downloadPath = configuration["PuppeteerPath"];
                if (string.IsNullOrWhiteSpace(downloadPath))
                    downloadPath = Path.Combine(AppContext.BaseDirectory, "puppeteer");

                // a browser downloaded before (by the installer or an earlier request);
                // chrome-headless-shell is preferred: it is made for servers and needs fewer system libraries
                var shellFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = downloadPath, Browser = SupportedBrowser.ChromeHeadlessShell });
                var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = downloadPath });
                var shell = SafeInstalled(shellFetcher).FirstOrDefault(b => b.Browser == SupportedBrowser.ChromeHeadlessShell);
                if (shell != null)
                {
                    _executablePath = shellFetcher.GetExecutablePath(shell.BuildId);
                    return;
                }

                // the version PuppeteerSharp is tested with (a much newer "stable" Chrome may not start with it)
                var buildId = PuppeteerSharp.BrowserData.Chrome.DefaultBuildId;
                try
                {
                    var shellVersion = await shellFetcher.DownloadAsync(buildId);
                    _executablePath = shellFetcher.GetExecutablePath(shellVersion.BuildId);
                    logger.LogInformation("Puppeteer browser (headless shell {version}): {path}", buildId, _executablePath);
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Downloading chrome-headless-shell {version} into {path} failed", buildId, downloadPath);
                }
                if (forceShell) throw new UserException(PdfUnavailable);

                var downloaded = SafeInstalled(browserFetcher).FirstOrDefault();
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
                    var chromeVersion = await browserFetcher.DownloadAsync(buildId);
                    _executablePath = browserFetcher.GetExecutablePath(chromeVersion.BuildId);
                    logger.LogInformation("Puppeteer browser {version}: {path}", buildId, _executablePath);
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

        /// <summary>A writable home for the browser: the service runs with a read-only home directory.</summary>
        private string BrowserHome()
        {
            var root = configuration["PuppeteerPath"];
            if (string.IsNullOrWhiteSpace(root)) root = Path.Combine(Path.GetTempPath(), "vg-auto-browser");
            var home = Path.Combine(root, "home");
            try { Directory.CreateDirectory(Path.Combine(home, "crashes")); }
            catch (Exception ex) { logger.LogWarning(ex, "Browser home {dir} cannot be created", home); home = Path.GetTempPath(); }
            return home;
        }

        private Task<IBrowser> LaunchAsync()
        {
            var home = BrowserHome();
            return Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = _executablePath,
                Args = BrowserArgs.Append("--crash-dumps-dir=" + Path.Combine(home, "crashes")).ToArray(),
                Env =
                {
                    ["HOME"] = home,
                    ["XDG_CONFIG_HOME"] = Path.Combine(home, ".config"),
                    ["XDG_CACHE_HOME"] = Path.Combine(home, ".cache"),
                },
                Timeout = 60000,
            });
        }

        private static readonly string[] BrowserArgs =
        {
            "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage", "--disable-gpu",
            "--no-zygote", "--no-first-run", "--disable-crash-reporter", "--disable-breakpad",
        };

        /// <summary>
        /// Runs the browser directly once to get its error output (the launcher only reports "Failed to launch browser"),
        /// logs all of it and returns the line that explains the problem.
        /// </summary>
        private async Task<string> DiagnoseAsync(string executable, Exception launchError)
        {
            try
            {
                var home = BrowserHome();
                var psi = new System.Diagnostics.ProcessStartInfo(executable)
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                psi.ArgumentList.Add("--headless");
                foreach (var arg in BrowserArgs) psi.ArgumentList.Add(arg);
                psi.ArgumentList.Add("--crash-dumps-dir=" + Path.Combine(home, "crashes"));
                psi.ArgumentList.Add("--dump-dom");
                psi.ArgumentList.Add("about:blank");
                psi.Environment["HOME"] = home;
                psi.Environment["XDG_CONFIG_HOME"] = Path.Combine(home, ".config");
                psi.Environment["XDG_CACHE_HOME"] = Path.Combine(home, ".cache");
                using var process = System.Diagnostics.Process.Start(psi);
                var stderr = process.StandardError.ReadToEndAsync();
                var stdout = process.StandardOutput.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                try { await process.WaitForExitAsync(timeout.Token); }
                catch (OperationCanceledException) { try { process.Kill(true); } catch { } return "the browser did not answer within 30 seconds"; }
                var output = (await stderr) + "\n" + (await stdout);
                logger.LogError("PDF browser test run of {path} exited with {code}:\n{output}", executable, process.ExitCode, output);
                if (process.ExitCode == 0) return LaunchFailure(launchError) + "; the browser itself starts, see the server log";
                return LaunchFailure(new Exception(output)) + $" (exit code {process.ExitCode})";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PDF browser test run of {path} failed", executable);
                return ex.Message.Length > 300 ? ex.Message[..300] : ex.Message;
            }
        }

        /// <summary>The useful part of the browser's error output (not the crash reporter or D-Bus noise).</summary>
        private static string LaunchFailure(Exception ex)
        {
            var text = ex.InnerException?.Message ?? ex.Message;
            var lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            static bool Noise(string l) => l.Contains("crashpad", StringComparison.OrdinalIgnoreCase) || l.Contains("dbus", StringComparison.OrdinalIgnoreCase)
                || l.Contains("Fontconfig", StringComparison.OrdinalIgnoreCase) || l.StartsWith("Failed to launch browser", StringComparison.OrdinalIgnoreCase);
            var reason = lines.FirstOrDefault(l => l.Contains(".so") || l.Contains("error while loading", StringComparison.OrdinalIgnoreCase))
                ?? lines.FirstOrDefault(l => !Noise(l) && (l.Contains("FATAL") || l.Contains("ERROR") || l.Contains("Check failed") || l.Contains("Timed out", StringComparison.OrdinalIgnoreCase)))
                ?? lines.FirstOrDefault(l => !Noise(l))
                ?? lines.FirstOrDefault()
                ?? ex.GetType().Name;
            return reason.Length > 300 ? reason[..300] : reason;
        }

        /// <summary>Makes sure a browser is available (used by the installer: "--pdf-setup").</summary>
        public async Task<string> EnsureBrowserAsync()
        {
            await PreparePuppeteerAsync();
            // start it once and print a test page, so problems show up here and not when the first invoice is sent
            IBrowser browser;
            try
            {
                browser = await LaunchAsync();
            }
            catch (Exception first) when (first is not UserException)
            {
                Console.Error.WriteLine($"Starting {_executablePath} failed: {await DiagnoseAsync(_executablePath, first)}\nTrying chrome-headless-shell ...");
                await PreparePuppeteerAsync(forceShell: true);
                browser = await LaunchAsync();
            }
            await using (browser)
            {
                var page = await browser.NewPageAsync();
                await page.SetContentAsync("<p>VG Auto PDF test</p>");
                var pdf = await page.PdfDataAsync(new PdfOptions { Format = PaperFormat.A4 });
                if (pdf.Length < 100) throw new InvalidOperationException("The test PDF is empty.");
            }
            return _executablePath;
        }

        private async Task<MemoryStream> Print(Pricing pricing )
        {
             
            var html = await bodyHtmlGenerator.Generate(pricing); 

            await PreparePuppeteerAsync();

            IBrowser launched;
            try
            {
                launched = await LaunchAsync();
            }
            catch (Exception first) when (first is not UserException)
            {
                logger.LogError(first, "Starting the PDF browser {path} failed", _executablePath);
                // a full Chrome may not run on a minimal or locked down server: try chrome-headless-shell once
                var failed = _executablePath;
                try
                {
                    await PreparePuppeteerAsync(forceShell: true);
                    if (_executablePath == failed) throw;
                    launched = await LaunchAsync();
                }
                catch (Exception second)
                {
                    if (second != first) logger.LogError(second, "Starting the PDF browser {path} failed", _executablePath);
                    _executablePath = failed;
                    throw new UserException("The PDF could not be created: the browser on the server does not start (" + await DiagnoseAsync(failed, first) + "). " +
                        "Run \"sudo vgauto pdf-setup\" on the server; the full output is in \"sudo vgauto logs api\".");
                }
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
