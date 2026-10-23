using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PergamonLens.IntegrationTests.Api;

public class FrontendStaticFilesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FrontendStaticFilesTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRoot_WithHtmlAcceptHeader_ShouldRedirectToIndexHtml()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().Be(new Uri("/index.html", UriKind.Relative));
    }

    [Fact]
    public async Task GetIndexHtml_ShouldReturnSuccessAndHtmlContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/index.html");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("PergamonLens");
        html.Should().Contain("Bóveda de Colección");
        html.Should().Contain("cameraVideo");
    }

    [Fact]
    public async Task GetStylesCss_ShouldReturnSuccessAndCssContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/css/styles.css");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/css");

        var css = await response.Content.ReadAsStringAsync();
        css.Should().Contain("--bg-main");
        css.Should().Contain("--accent-gold");
    }

    [Fact]
    public async Task GetAppJs_ShouldReturnSuccessAndJavaScriptContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/js/app.js");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.Should().Match(mt => mt == "application/javascript" || mt == "text/javascript");

        var js = await response.Content.ReadAsStringAsync();
        js.Should().Contain("pergamonlens_auth_token");
        js.Should().Contain("apiFetch");
    }
}
