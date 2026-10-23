using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PergamonLens.Domain.Enums;

namespace PergamonLens.IntegrationTests.Api;

public class EndpointAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndpointAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/releases")]
    [InlineData("/api/releases/48270116-2915-4235-953b-f4586f2b7a0d")]
    [InlineData("/api/collection/statistics")]
    [InlineData("/api/discogs/search?q=Beatles")]
    public async Task ProtectedEndpoints_WhenAccessedAnonymously_ShouldReturnUnauthorized(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostRelease_WhenAccessedAnonymously_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/releases", new
        {
            Title = "Unauthorized Album",
            Artist = "Unknown Artist",
            Format = MediaFormat.Vinyl
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteRelease_WhenAccessedAnonymously_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/releases/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
