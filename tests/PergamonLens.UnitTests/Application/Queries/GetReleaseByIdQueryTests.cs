using FluentAssertions;
using Moq;
using PergamonLens.Application.MusicReleases.Queries.GetReleaseById;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.Application.Queries;

public class GetReleaseByIdQueryTests
{
    private readonly Mock<IMusicReleaseRepository> _repositoryMock;
    private readonly GetReleaseByIdQueryHandler _handler;
    private readonly GetReleaseByIdQueryValidator _validator;

    public GetReleaseByIdQueryTests()
    {
        _repositoryMock = new Mock<IMusicReleaseRepository>();
        _handler = new GetReleaseByIdQueryHandler(_repositoryMock.Object);
        _validator = new GetReleaseByIdQueryValidator();
    }

    [Fact]
    public async Task Handle_WhenReleaseExists_ShouldReturnDto()
    {
        // Arrange
        var release = MusicRelease.Create(UserId.New(), "Blue Train", "John Coltrane");
        _repositoryMock
            .Setup(r => r.GetByIdAsync(release.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(release);

        var query = new GetReleaseByIdQuery(release.Id.Value);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(release.Id.Value);
        result.Title.Should().Be("Blue Train");
        result.Artist.Should().Be("John Coltrane");
    }

    [Fact]
    public async Task Handle_WhenReleaseDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(new MusicReleaseId(nonExistentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MusicRelease?)null);

        var query = new GetReleaseByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Validator_WhenIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var query = new GetReleaseByIdQuery(Guid.Empty);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
