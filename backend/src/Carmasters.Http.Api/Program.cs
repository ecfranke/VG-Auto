using System;
using System.IO;
using System.Threading.Tasks;
using Carmasters.Core.Application.Authorization;
using Carmasters.Core.Application.Configuration;
using Carmasters.Core.Application.Documentation;
using Carmasters.Core.Application.Errors;
using Carmasters.Core.Application.Extensions.Builder;
using Carmasters.Core.Application.Extensions.DependencyInjection;
using Carmasters.Core.Application.Printing;
using Carmasters.Core.Application.RateLimiting;
using Carmasters.Core.Application.Services;
using Carmasters.Core.Application.Email;
using Carmasters.Core.Application.Authentication;
using Carmasters.Core.Domain;
using Carmasters.Core.Repository.Postgres;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Carmasters.Core.Application.Database;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseStaticWebAssets();

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

app.UseRouting();
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseMiddleware<DbConnectionScopeMiddleware>();
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
app.Run();

public partial class Program { }
