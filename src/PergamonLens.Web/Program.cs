using PergamonLens.Application;
using PergamonLens.Application.Common.Interfaces;
using PergamonLens.Infrastructure;
using PergamonLens.Infrastructure.Identity;
using PergamonLens.Infrastructure.Persistence;
using PergamonLens.Web.Endpoints;
using PergamonLens.Web.Infrastructure;
using PergamonLens.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios de Contexto HTTP y Usuario Actual
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Manejo Global de Excepciones y ProblemDetails (RFC 7807)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Endpoints de autenticación nativos de ASP.NET Core Identity (/auth/register, /auth/login, etc.)
app.MapGroup("/auth")
    .MapIdentityApi<ApplicationUser>()
    .WithTags("Authentication");

// Endpoints de catálogo y casos de uso de la colección
app.MapMusicReleaseEndpoints();
app.MapCollectionEndpoints();
app.MapDiscogsEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    Application = "PergamonLens API",
    Version = "1.0.0",
    Status = "Healthy",
    Timestamp = DateTime.UtcNow
}));

app.Run();

// Habilitar acceso de visibilidad para tests de integración WebApplicationFactory
public partial class Program { }
