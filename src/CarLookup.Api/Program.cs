using CarLookup.Api.Infrastructure;
using CarLookup.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<VpicExceptionHandler>();

// In Docker the Angular app is served by nginx, which proxies /api to this service, so requests
// are same-origin. This policy only matters for local development, where the Angular dev server
// runs on its own port.
const string DevelopmentCorsPolicy = "development-spa";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options => options.AddPolicy(DevelopmentCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

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

app.UseExceptionHandler();

if (allowedOrigins.Length > 0)
{
    app.UseCors(DevelopmentCorsPolicy);
}

if (app.Environment.IsDevelopment())
{
    // Machine-readable API description at /openapi/v1.json.
    app.MapOpenApi();
}

// Probed by the Docker health check and by the AWS instance health check.
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();

/// <summary>Exposed so tests can bootstrap the application.</summary>
public partial class Program;
