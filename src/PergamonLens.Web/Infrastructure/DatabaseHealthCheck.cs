using Microsoft.Extensions.Diagnostics.HealthChecks;
using PergamonLens.Infrastructure.Persistence;

namespace PergamonLens.Web.Infrastructure;

/// <summary>
/// Health Check ligero que verifica la conectividad real contra PostgreSQL para Docker y plataformas Cloud.
/// </summary>
public sealed class DatabaseHealthCheck(PergamonLensDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL health check failed.", ex);
        }
    }
}
