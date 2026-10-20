using FluentAssertions;
using Moq;
using PergamonLens.Application.Common.Exceptions;
using PergamonLens.Application.MusicReleases.Commands.DeleteRelease;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.Application.Commands;

public class DeleteReleaseCommandTests
{
    private readonly Mock<IMusicReleaseRepository> _repositoryMock;
    private readonly DeleteReleaseCommandHandler _handler;
    private readonly DeleteReleaseCommandValidator _validator;

    public DeleteReleaseCommandTests()
    {
        _repositoryMock = new Mock<IMusicReleaseRepository>();
        _handler = new DeleteReleaseCommandHandler(_repositoryMock.Object);
        _validator = new DeleteReleaseCommandValidator();
    }

    [Fact]
    public async Task Handle_WhenReleaseExists_ShouldCallDeleteAsync()
    {
        // Arrange
        var release = MusicRelease.Create("Rumours", "Fleetwood Mac");
        _repositoryMock
            .Setup(r => r.GetByIdAsync(release.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(release);

        var command = new DeleteReleaseCommand(release.Id.Value);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(release, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReleaseNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(new MusicReleaseId(nonExistentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MusicRelease?)null);

        var command = new DeleteReleaseCommand(nonExistentId);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Validator_WhenReleaseIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = new DeleteReleaseCommand(Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseId");
    }
}

