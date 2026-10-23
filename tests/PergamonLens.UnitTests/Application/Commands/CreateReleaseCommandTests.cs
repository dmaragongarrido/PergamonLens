using FluentAssertions;
using Moq;
using PergamonLens.Application.Common.Interfaces;
using PergamonLens.Application.MusicReleases.Commands.CreateRelease;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;

namespace PergamonLens.UnitTests.Application.Commands;

public class CreateReleaseCommandTests
{
    private readonly Mock<IMusicReleaseRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly CreateReleaseCommandHandler _handler;
    private readonly CreateReleaseCommandValidator _validator;

    public CreateReleaseCommandTests()
    {
        _repositoryMock = new Mock<IMusicReleaseRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(s => s.UserId).Returns("default-test-user");

        _handler = new CreateReleaseCommandHandler(_repositoryMock.Object, _currentUserServiceMock.Object);
        _validator = new CreateReleaseCommandValidator();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldPersistAndReturnDto()
    {
        // Arrange
        var command = new CreateReleaseCommand(
            Title: "The Dark Side of the Moon",
            Artist: "Pink Floyd",
            Format: MediaFormat.Vinyl,
            Barcode: "5099902987613",
            CatalogueNumber: "SHVL 804",
            ReleaseYear: 1973,
            DiscsCount: 1,
            VinylSize: VinylSize.Inch12,
            VinylSpeed: VinylSpeed.Rpm33,
            Label: "Harvest",
            Country: "UK",
            Notes: "Gatefold cover");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("The Dark Side of the Moon");
        result.Artist.Should().Be("Pink Floyd");
        result.Format.Should().Be("Vinyl");
        result.Barcode.Should().Be("5099902987613");
        result.CatalogueNumber.Should().Be("SHVL 804");
        result.ReleaseYear.Should().Be(1973);
        result.DiscsCount.Should().Be(1);
        result.UserId.Should().Be("default-test-user");

        _repositoryMock.Verify(r => r.AddAsync(It.Is<MusicRelease>(m =>
            m.Title == "The Dark Side of the Moon" &&
            m.Artist == "Pink Floyd" &&
            m.UserId.Value == "default-test-user" &&
            m.Barcode != null && m.Barcode.Value == "5099902987613"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", "Pink Floyd")]
    [InlineData("   ", "Pink Floyd")]
    [InlineData("The Dark Side of the Moon", "")]
    [InlineData("The Dark Side of the Moon", "   ")]
    public void Validator_WhenTitleOrArtistIsEmpty_ShouldHaveValidationError(string title, string artist)
    {
        // Arrange
        var command = new CreateReleaseCommand(Title: title, Artist: artist);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" || e.PropertyName == "Artist");
    }

    [Fact]
    public void Validator_WhenBarcodeIsInvalid_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateReleaseCommand(
            Title: "Random Access Memories",
            Artist: "Daft Punk",
            Barcode: "123456"); // Invalid length / checksum

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Barcode");
    }

    [Fact]
    public void Validator_WhenReleaseYearIsBeforeHistory_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateReleaseCommand(
            Title: "Ancient Symphony",
            Artist: "Unknown",
            ReleaseYear: 1800);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseYear");
    }
}
