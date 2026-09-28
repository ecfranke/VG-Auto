using System;
using System.IO;
using System.Threading.Tasks;
using VgAuto.Core.Application.Authorization;
using VgAuto.Core.Application.Configuration;
using VgAuto.Core.Application.Documentation;
using VgAuto.Core.Application.Errors;
using VgAuto.Core.Application.Extensions.Builder;
using VgAuto.Core.Application.Extensions.DependencyInjection;
using VgAuto.Core.Application.Printing;
using VgAuto.Core.Application.RateLimiting;
using VgAuto.Core.Application.Services;
using VgAuto.Core.Application.Email;
using VgAuto.Core.Application.Authentication;
using VgAuto.Core.Domain;
using VgAuto.Core.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using VgAuto.Core.Application.Database;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseStaticWebAssets();
// Integrates with systemd (Type=notify, journald) and Windows services; no effect when run from a console.
builder.Host.UseSystemd();
builder.Host.UseWindowsService();

// Secrets live outside source control. Environment variables (e.g. DbOptions__Password) override files.
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

StartupValidation.Validate(builder.Configuration, builder.Environment);

builder.Services
    .AddPersistanceServices(builder.Configuration)
    .AddScoped<ITemplateService, RazorViewsTemplateService>()
    .AddScoped<IPdfGenerator, PdfGenerator>()
    .AddScoped<PricingFooterHtmlGenerator>()
    .AddScoped<PricingBodyHtmlGenerator>()
    .AddScoped<IPricingSender, PricingPdfMailSender>()
    .AddEmail(builder.Configuration)
    .AddDemoSetupServices()
    .AddCorsToApp(builder.Configuration)
    .AddControllersWithViewsToApp()
    .AddHealthChecks().Services
    .AddSwaggerToApp()
    .AddJwtAuthenticationToApp(builder.Configuration)
    .AddHttpContextAccessor()
    .AddDistributedMemoryCache()
    .AddApplicationOptions(builder.Configuration)
    .AddExceptionHandler<JsonExceptionHandler>()
    .AddTenantConfigurationServices()
    .AddLoginFlows(builder.Configuration);

builder.Services.AddSingleton<RateLimitStrategyFactory>();
if (builder.Configuration.GetValue("DbOptions:Provider", DatabaseProvider.PostgreSql) == DatabaseProvider.MySql)
{
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o => o.JsonSerializerOptions.Converters.Add(new VgAuto.Http.Api.UtcDateTimeConverter()));
}
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Only proxies on this machine (nginx, the Next.js server) are trusted by default.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
    {
        options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
    }
});

var app = builder.Build();
JsonErrorDto.IncludeDetails = app.Configuration.GetValue("Errors:IncludeDetails", app.Environment.IsDevelopment());

app.UseForwardedHeaders();
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        await Task.CompletedTask; //JsonExceptionHandler wont run without this
    });
});
app.UseStatusCodePages();
app.UseNHibernate();

// wwwroot (tailwind.css, print.css) is public: the PDF renderer loads it without a token
app.UseStaticFiles();
app.UseRouting();
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseMiddleware<DbConnectionScopeMiddleware>();
app.UseMiddleware<AccountStatusMiddleware>();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();
app.UseAuthorization();
app.UseRateLimiting();

// css used by the pdf renderer, must stay public
app.MapStaticAssets().AllowAnonymous();

if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        var js = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documentation", "SwaggerJwtInetercept.js")).ReplaceLineEndings(" ");
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        c.RoutePrefix = "swagger";
        c.EnablePersistAuthorization();
        c.UseRequestInterceptor(js);
    });
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
// "--pdf-setup": make sure the PDF renderer has a browser (downloads Chrome when none is installed), then exit
if (System.Linq.Enumerable.Contains(args, "--pdf-setup"))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var generator = (VgAuto.Core.Application.Services.PdfGenerator)scope.ServiceProvider.GetRequiredService<VgAuto.Core.Application.Services.IPdfGenerator>();
        Console.WriteLine("PDF browser: " + await generator.EnsureBrowserAsync());
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("PDF browser setup failed: " + ex.Message);
        return 1;
    }
}

app.Run();
return 0;

public partial class Program { }
