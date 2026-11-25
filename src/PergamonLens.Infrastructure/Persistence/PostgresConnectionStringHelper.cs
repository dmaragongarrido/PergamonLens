using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PergamonLens.Infrastructure.Persistence;

/// <summary>
/// Resuelve y normaliza cadenas de conexión de PostgreSQL soportando tanto el formato nativo de Npgsql
/// como el formato URI estándar (<c>postgres://</c> / <c>postgresql://</c>) usado por Neon, Render, Supabase y Railway.
/// </summary>
public static class PostgresConnectionStringHelper
{
    private const string DefaultLocalConnection =
        "Host=localhost;Port=5432;Database=PergamonLensDb;Username=postgres;Password=postgres";

    public static string ResolveConnectionString(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];
        var raw = !string.IsNullOrWhiteSpace(databaseUrl)
            ? databaseUrl
            : configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultLocalConnection;
        }

        return Normalize(raw);
    }

    public static string Normalize(string connectionString)
    {
        var trimmed = connectionString.Trim();
        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var uri = new Uri(trimmed);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'))
        };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var userInfoParts = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(userInfoParts[0]);
            if (userInfoParts.Length > 1)
            {
                builder.Password = Uri.UnescapeDataString(userInfoParts[1]);
            }
        }

        if (!string.IsNullOrEmpty(uri.Query))
        {
            var pairs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var kv = pair.Split('=', 2);
                if (kv.Length != 2) continue;

                var key = Uri.UnescapeDataString(kv[0]);
                var value = Uri.UnescapeDataString(kv[1]);

                if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase) &&
                    Enum.TryParse<SslMode>(value, ignoreCase: true, out var sslMode))
                {
                    builder.SslMode = sslMode;
                }
            }
        }

        return builder.ConnectionString;
    }
}
