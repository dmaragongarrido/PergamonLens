using FluentAssertions;
using Moq;
using PergamonLens.Application.MusicReleases.Queries.SearchDiscogs;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.Application.Queries;

public class SearchDiscogsQueryTests
{
    private readonly Mock<IDiscogsService> _discogsServiceMock;
    private readonly SearchDiscogsQueryHandler _handler;
    private readonly SearchDiscogsQueryValidator _validator;

    public SearchDiscogsQueryTests()
    {
        _discogsServiceMock = new Mock<IDiscogsService>();
        _handler = new SearchDiscogsQueryHandler(_discogsServiceMock.Object);
        _validator = new SearchDiscogsQueryValidator();
    }

    [Fact]
    public async Task Handle_WithBarcode_ShouldCallSearchByBarcodeAsync()
    {
        // Arrange
        var barcode = "0094638246817";
        var expectedResults = new List<DiscogsSearchResult>
        {
            new(249504, "Abbey Road", "The Beatles", 1969, "Apple", "PCS 7088", "UK", "https://img.jpg")
        };

        _discogsServiceMock
            .Setup(s => s.SearchByBarcodeAsync(It.Is<Barcode>(b => b.Value == barcode), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        var query = new SearchDiscogsQuery(Barcode: barcode);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Abbey Road");
        result[0].Artist.Should().Be("The Beatles");
    }

    [Fact]
    public async Task Handle_WithArtistAndTitle_ShouldCallSearchByQueryAsync()
    {
        // Arrange
        var expectedResults = new List<DiscogsSearchResult>
        {
            new(1234, "Kind of Blue", "Miles Davis", 1959, "Columbia", "CL 1355", "US", "https://img.jpg")
        };

        _discogsServiceMock
            .Setup(s => s.SearchByQueryAsync("Miles Davis", "Kind of Blue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        var query = new SearchDiscogsQuery(Artist: "Miles Davis", Title: "Kind of Blue");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Kind of Blue");
    }

    [Fact]
    public async Task Handle_WithFreeTextQuery_ShouldCallSearchByFreeTextAsync()
    {
        // Arrange
        var expectedResults = new List<DiscogsSearchResult>
        {
            new(5678, "A Night at the Opera", "Queen", 1975, "EMI", "EMTC 103", "UK", "https://img.jpg")
        };

        _discogsServiceMock
            .Setup(s => s.SearchByFreeTextAsync("Queen A Night at the Opera", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        var query = new SearchDiscogsQuery(Query: "Queen A Night at the Opera");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("A Night at the Opera");
        result[0].Artist.Should().Be("Queen");
    }

    [Fact]
    public void Validator_WithFreeTextQuery_ShouldBeValid()
    {
        // Arrange
        var query = new SearchDiscogsQuery(Query: "The Beatles");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WhenAllCriteriaAreEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var query = new SearchDiscogsQuery();

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}

