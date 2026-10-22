using PergamonLens.Application;
using PergamonLens.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Registro de servicios de las capas de Clean Architecture
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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
