using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using PergamonLens.Application.DTOs;
using PergamonLens.Domain.Enums;
using PergamonLens.Web.Endpoints;

namespace PergamonLens.IntegrationTests.Api;

public class MusicReleaseEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MusicReleaseEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var uniqueEmail = $"collector_{Guid.NewGuid():N}@pergamonlens.com";
        const string password = "StrongPassword123!";

        // 1. Registro del usuario
        var registerResponse = await client.PostAsJsonAsync("/auth/register", new
        {
            email = uniqueEmail,
            password = password
        });

        registerResponse.EnsureSuccessStatusCode();

        // 2. Inicio de sesión para obtener el Access Token (Bearer)
        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            email = uniqueEmail,
            password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginResult.GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task CreateRelease_And_GetById_ShouldCreateAndRetrieveReleaseSuccessfully()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        var createPayload = new
        {
            Title = "A Love Supreme",
            Artist = "John Coltrane",
            Format = MediaFormat.Vinyl,
            Barcode = "012345678905",
            CatalogueNumber = "AS-77",
            ReleaseYear = 1965,
            DiscsCount = 1,
            VinylSize = VinylSize.Inch12,
            VinylSpeed = VinylSpeed.Rpm33,
            Label = "Impulse!",
            Country = "US",
            Notes = "Gatefold original"
        };

        // Act 1: Crear lanzamiento
        var createResponse = await client.PostAsJsonAsync("/api/releases", createPayload);

        // Assert 1
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull();

        var createdRelease = await createResponse.Content.ReadFromJsonAsync<MusicReleaseDto>();
        createdRelease.Should().NotBeNull();
        createdRelease!.Title.Should().Be("A Love Supreme");
        createdRelease.Artist.Should().Be("John Coltrane");
        createdRelease.Format.Should().Be("Vinyl");
        createdRelease.Status.Should().Be("PendingAnalysis");

        // Act 2: Consultar por ID
        var getResponse = await client.GetAsync($"/api/releases/{createdRelease.Id}");

        // Assert 2
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<MusicReleaseDto>();
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(createdRelease.Id);
        retrieved.Title.Should().Be("A Love Supreme");
    }

    [Fact]
    public async Task CreateRelease_WithInvalidData_ShouldReturnBadRequestWithProblemDetails()
    {
        // Arrange: Título vacío viola la validación FluentValidation
        var client = await CreateAuthenticatedClientAsync();

        var invalidPayload = new
        {
            Title = "",
            Artist = "Pink Floyd",
            Format = MediaFormat.Vinyl
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/releases", invalidPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Error de validación");
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task ConfirmRelease_ShouldUpdateGradingStatusAndNotes()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/releases", new
        {
            Title = "Kind of Blue",
            Artist = "Miles Davis",
            Format = MediaFormat.Vinyl
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<MusicReleaseDto>();

        var confirmPayload = new ConfirmReleaseRequest(
            MediaCondition: MediaCondition.NearMint,
            SleeveCondition: MediaCondition.VeryGoodPlus,
            Notes: "Comprado en feria de coleccionismo");

        // Act
        var confirmResponse = await client.PostAsJsonAsync($"/api/releases/{created!.Id}/confirm", confirmPayload);

        // Assert
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmedDto = await confirmResponse.Content.ReadFromJsonAsync<MusicReleaseDto>();
        confirmedDto.Should().NotBeNull();
        confirmedDto!.Status.Should().Be("Confirmed");
        confirmedDto.MediaCondition.Should().Be("NearMint");
        confirmedDto.SleeveCondition.Should().Be("VeryGoodPlus");
        confirmedDto.Notes.Should().Be("Comprado en feria de coleccionismo");
    }

    [Fact]
    public async Task DeleteRelease_ShouldRemoveReleaseAndReturnNoContent()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/releases", new
        {
            Title = "Temporary Single",
            Artist = "One Hit Wonder",
            Format = MediaFormat.Vinyl
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<MusicReleaseDto>();

        // Act 1: Borrar
        var deleteResponse = await client.DeleteAsync($"/api/releases/{created!.Id}");

        // Assert 1
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act 2: Consultar de nuevo
        var getResponse = await client.GetAsync($"/api/releases/{created.Id}");

        // Assert 2: Debe ser 404
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCollectionStatistics_ShouldReturnAggregatedMetrics()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        await client.PostAsJsonAsync("/api/releases", new
        {
            Title = "Album Stats Test",
            Artist = "Stats Artist",
            Format = MediaFormat.CD
        });

        // Act
        var response = await client.GetAsync("/api/collection/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<CollectionStatisticsDto>();
        stats.Should().NotBeNull();
        stats!.TotalReleases.Should().BeGreaterThanOrEqualTo(1);
    }
}

