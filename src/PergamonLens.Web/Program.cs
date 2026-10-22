using PergamonLens.Application;
using PergamonLens.Application.Common.Interfaces;
using PergamonLens.Infrastructure;
using PergamonLens.Infrastructure.Identity;
using PergamonLens.Infrastructure.Persistence;
using PergamonLens.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios de Contexto HTTP y Usuario Actual
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Registro de servicios de las capas de Clean Architecture
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ASP.NET Core Identity & Autorización
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<PergamonLensDbContext>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Endpoints de autenticación nativos de ASP.NET Core Identity (/auth/register, /auth/login, etc.)
app.MapGroup("/auth")
    .MapIdentityApi<ApplicationUser>()
    .WithTags("Authentication");

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
