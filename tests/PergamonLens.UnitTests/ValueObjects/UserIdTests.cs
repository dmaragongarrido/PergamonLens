using FluentAssertions;
using PergamonLens.Domain.Exceptions;
using PergamonLens.Domain.ValueObjects;
using Xunit;

namespace PergamonLens.UnitTests.ValueObjects;

public class UserIdTests
{
    [Fact]
    public void New_ShouldGenerateValidGuidBasedUserId()
    {
        // Act
        var userId = UserId.New();

        // Assert
        userId.Should().NotBeNull();
        userId.Value.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(userId.Value, out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("user-12345")]
    [InlineData("auth0|123456789")]
    [InlineData("b13a7cb8-5b4d-4eb3-8d02-0e8c0fa370b5")]
    public void From_WithValidString_ShouldCreateUserId(string validId)
    {
        // Act
        var userId = UserId.From(validId);

        // Assert
        userId.Value.Should().Be(validId);
        userId.ToString().Should().Be(validId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void From_WithEmptyOrWhitespace_ShouldThrowInvalidUserIdException(string? invalidId)
    {
        // Act
        var act = () => UserId.From(invalidId);

        // Assert
        act.Should().Throw<InvalidUserIdException>()
            .WithMessage("*no puede estar vacío*");
    }

    [Fact]
    public void From_WithExceedingMaxLength_ShouldThrowInvalidUserIdException()
    {
        // Arrange
        var tooLong = new string('a', 451);

        // Act
        var act = () => UserId.From(tooLong);

        // Assert
        act.Should().Throw<InvalidUserIdException>()
            .WithMessage("*no puede superar los 450 caracteres*");
    }

    [Fact]
    public void TryFrom_WithInvalidString_ShouldReturnNull()
    {
        // Act
        var result = UserId.TryFrom("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryFrom_WithValidString_ShouldReturnUserId()
    {
        // Act
        var result = UserId.TryFrom("user-1");

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("user-1");
    }

    [Fact]
    public void TwoUserIds_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var id1 = UserId.From("test-user-id");
        var id2 = UserId.From("test-user-id");

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void ImplicitOperator_ToString_ShouldReturnValue()
    {
        // Arrange
        var userId = UserId.From("user-xyz");

        // Act
        string stringVal = userId;

        // Assert
        stringVal.Should().Be("user-xyz");
    }
}
