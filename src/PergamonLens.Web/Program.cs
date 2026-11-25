using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PergamonLens.Application;
using PergamonLens.Application.Common.Interfaces;
using PergamonLens.Infrastructure;
using PergamonLens.Infrastructure.Identity;
using PergamonLens.Infrastructure.Persistence;
using PergamonLens.Web.Endpoints;
using PergamonLens.Web.Infrastructure;
using PergamonLens.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Puerto dinámico para entornos Cloud / Contenedores (Render, Azure Container Apps, Railway, etc.)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Soporte para Reverse Proxy TLS en Cloud (X-Forwarded-For / X-Forwarded-Proto)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Health Check nativo con verificación de conectividad a PostgreSQL
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgresql");

// Servicios de Contexto HTTP y Usuario Actual
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Manejo Global de Excepciones y ProblemDetails (RFC 7807)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Configuración de serialización JSON con soporte para Enums en formato string
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Registro de servicios de las capas de Clean Architecture
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// CORS para acceso desde frontend / PWA móvil
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ASP.NET Core Identity & Autorización
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<PergamonLensDbContext>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Aplicar migraciones pendientes de base de datos automáticamente al arrancar
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PergamonLensDbContext>();
    dbContext.Database.Migrate();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();

// Habilitar archivos estáticos para la interfaz Web / PWA
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Health Check público para Docker, Render y Azure Container Apps
app.MapHealthChecks("/health");

// Endpoints de autenticación nativos de ASP.NET Core Identity (/auth/register, /auth/login, etc.)
app.MapGroup("/auth")
    .MapIdentityApi<ApplicationUser>()
    .WithTags("Authentication");

// Endpoints de catálogo y casos de uso de la colección
app.MapMediaReleaseEndpoints();
app.MapCollectionEndpoints();
app.MapDiscogsEndpoints();
app.MapBookCatalogEndpoints();
app.MapGameCatalogEndpoints();

// Root endpoint: Redirige al navegador a la interfaz Web o devuelve JSON de estado para APIs/Tests
app.MapGet("/", (HttpContext context) =>
{
    if (context.Request.Headers.Accept.ToString().Contains("text/html"))
    {
        return Results.Redirect("/index.html");
    }

    return Results.Ok(new
    {
        Application = "PergamonLens API",
        Version = "1.0.0",
        Status = "Healthy",
        Timestamp = DateTime.UtcNow
    });
});

app.Run();

// Habilitar acceso de visibilidad para tests de integración WebApplicationFactory
public partial class Program { }
