using FluentAssertions;
using Microsoft.Extensions.Configuration;
using PergamonLens.Infrastructure.Persistence;

namespace PergamonLens.UnitTests.Infrastructure;

public sealed class PostgresConnectionStringHelperTests
{
    [Fact]
    public void Normalize_WhenAlreadyNpgsqlFormat_ShouldReturnUnchanged()
    {
        const string npgsql = "Host=localhost;Port=5432;Database=PergamonLensDb;Username=postgres;Password=postgres";

        var result = PostgresConnectionStringHelper.Normalize(npgsql);

        result.Should().Be(npgsql);
    }

    [Fact]
    public void Normalize_WhenPostgresUriWithSslMode_ShouldConvertToNpgsqlFormat()
    {
        const string uri = "postgresql://neon_user:Sec%40ret123@ep-cool-host.eu-central-1.aws.neon.tech/pergamonlens?sslmode=require";

        var result = PostgresConnectionStringHelper.Normalize(uri);

        result.Should().Contain("Host=ep-cool-host.eu-central-1.aws.neon.tech");
        result.Should().Contain("Port=5432");
        result.Should().Contain("Database=pergamonlens");
        result.Should().Contain("Username=neon_user");
        result.Should().Contain("Password=Sec@ret123");
        result.Should().Contain("SSL Mode=Require");
    }

    [Fact]
    public void ResolveConnectionString_ShouldPreferDatabaseUrlOverDefaultConnection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=LocalDb",
                ["DATABASE_URL"] = "postgres://cloud_user:pass@db.cloud.io:5433/proddb?sslmode=require"
            })
            .Build();

        var result = PostgresConnectionStringHelper.ResolveConnectionString(config);

        result.Should().Contain("Host=db.cloud.io");
        result.Should().Contain("Port=5433");
        result.Should().Contain("Database=proddb");
    }
}
