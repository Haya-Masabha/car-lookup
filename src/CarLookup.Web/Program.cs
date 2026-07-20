using CarLookup.Web.Infrastructure;
using CarLookup.Web.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<VpicExceptionHandler>();

builder.Services
    .AddOptions<VpicOptions>()
    .Bind(builder.Configuration.GetSection(VpicOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Registered as a concrete type so the caching decorator below can wrap it.
builder.Services
    .AddHttpClient<VpicVehicleCatalogService>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<VpicOptions>>().Value;

        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = options.Timeout;
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CarLookup/1.0");
    })
    // vPIC is a free public service that occasionally drops a request; retry transient
    // failures rather than surfacing them to the user.
    .AddStandardResilienceHandler();

builder.Services.AddScoped<IVehicleCatalogService>(serviceProvider => new CachingVehicleCatalogService(
    serviceProvider.GetRequiredService<VpicVehicleCatalogService>(),
    serviceProvider.GetRequiredService<IMemoryCache>(),
    serviceProvider.GetRequiredService<IOptions<VpicOptions>>()));

var app = builder.Build();

app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

// Probed by Docker and by the AWS load balancer / instance health check.
app.MapHealthChecks("/health");

app.MapControllers();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

/// <summary>Exposed so the integration tests can bootstrap the application.</summary>
public partial class Program;
