using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.Services;
using PergamonLens.Infrastructure.BackgroundServices;
using PergamonLens.Infrastructure.BackgroundServices.ChannelQueue;
using PergamonLens.Infrastructure.ExternalServices.Discogs;
using PergamonLens.Infrastructure.ExternalServices.Vision;
using PergamonLens.Infrastructure.Persistence;
using PergamonLens.Infrastructure.Persistence.Repositories;

namespace PergamonLens.Infrastructure;

/// <summary>
/// Métodos de extensión para configurar la inyección de dependencias de la capa de Infraestructura.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Persistencia: PostgreSQL & EF Core
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=PergamonLensDb;Username=postgres;Password=postgres";

        services.AddDbContext<PergamonLensDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PergamonLensDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        services.AddScoped<IMusicReleaseRepository, MusicReleaseRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();

        // 2. Cliente HTTP Resiliente para Discogs (Polly)
        services.Configure<DiscogsOptions>(configuration.GetSection(DiscogsOptions.SectionName));

        services.AddHttpClient<IDiscogsService, DiscogsService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DiscogsOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);

            if (!string.IsNullOrWhiteSpace(options.Token))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Discogs token={options.Token}");
            }
        })
        .AddStandardResilienceHandler();

        // 3. Servicio de Visión IA Multimodal
        services.Configure<VisionOptions>(configuration.GetSection(VisionOptions.SectionName));
        services.AddScoped<IVisionService, VisionService>();

        // 4. Cola y Worker Asíncrono en Segundo Plano (System.Threading.Channels)
        services.AddSingleton<IReleaseScanQueue, ReleaseScanQueue>();
        services.AddHostedService<ReleaseScanBackgroundWorker>();

        return services;
    }
}
