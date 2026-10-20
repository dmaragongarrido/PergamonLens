using FluentAssertions;
using Moq;
using PergamonLens.Application.Common.Exceptions;
using PergamonLens.Application.MusicReleases.Commands.ConfirmRelease;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.Application.Commands;

public class ConfirmReleaseCommandTests
{
    private readonly Mock<IMusicReleaseRepository> _repositoryMock;
    private readonly ConfirmReleaseCommandHandler _handler;
    private readonly ConfirmReleaseCommandValidator _validator;

    public ConfirmReleaseCommandTests()
    {
        _repositoryMock = new Mock<IMusicReleaseRepository>();
        _handler = new ConfirmReleaseCommandHandler(_repositoryMock.Object);
        _validator = new ConfirmReleaseCommandValidator();
    }

    [Fact]
    public async Task Handle_WhenReleaseExists_ShouldSetGradingConfirmAndReturnDto()
    {
        // Arrange
        var release = MusicRelease.Create("Wish You Were Here", "Pink Floyd");
        _repositoryMock
            .Setup(r => r.GetByIdAsync(release.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(release);

        var command = new ConfirmReleaseCommand(
            release.Id.Value,
            MediaCondition.NearMint,
            MediaCondition.VeryGoodPlus,
            Notes: "Bought in Camden Market 2022");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Confirmed");
        result.MediaCondition.Should().Be("NearMint");
        result.SleeveCondition.Should().Be("VeryGoodPlus");
        result.Notes.Should().Be("Bought in Camden Market 2022");

        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<MusicRelease>(m =>
            m.Id == release.Id &&
            m.Status == ProcessingStatus.Confirmed &&
            m.MediaCondition == MediaCondition.NearMint &&
            m.SleeveCondition == MediaCondition.VeryGoodPlus), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReleaseNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(new MusicReleaseId(nonExistentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MusicRelease?)null);

        var command = new ConfirmReleaseCommand(
            nonExistentId,
            MediaCondition.Mint,
            MediaCondition.Mint);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Validator_WhenReleaseIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = new ConfirmReleaseCommand(
            Guid.Empty,
            MediaCondition.Mint,
            MediaCondition.Mint);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseId");
    }
}

