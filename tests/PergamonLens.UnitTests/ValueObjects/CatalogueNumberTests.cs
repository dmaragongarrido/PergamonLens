using FluentAssertions;
using PergamonLens.Domain.Exceptions;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.ValueObjects;

public class CatalogueNumberTests
{
    [Theory]
    [InlineData("PCS 7027")]
    [InlineData("SW-383")]
    [InlineData("K 50 001")]
    [InlineData("MFSL 1-017")]
    public void Create_WithValidCatalogueNumber_ShouldInstantiateSuccessfully(string validNumber)
    {
        // Act
        var catalogueNumber = CatalogueNumber.Create(validNumber);

        // Assert
        catalogueNumber.Should().NotBeNull();
        catalogueNumber.Value.Should().Be(validNumber);
    }

    [Fact]
    public void Create_WithLowercaseAndSpaces_ShouldTrimAndUppercase()
    {
        // Act
        var catalogueNumber = CatalogueNumber.Create("  pcs 7027  ");

        // Assert
        catalogueNumber.Value.Should().Be("PCS 7027");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ShouldThrowInvalidCatalogueNumberException(string? emptyInput)
    {
        // Act
        Action act = () => CatalogueNumber.Create(emptyInput);

        // Assert
        act.Should().Throw<InvalidCatalogueNumberException>()
            .WithMessage("*no puede estar vacío*");
    }

    [Fact]
    public void Create_WhenExceedsMaxLength_ShouldThrowInvalidCatalogueNumberException()
    {
        // Arrange
        string tooLong = new('A', CatalogueNumber.MaxLength + 1);

        // Act
        Action act = () => CatalogueNumber.Create(tooLong);

        // Assert
        act.Should().Throw<InvalidCatalogueNumberException>()
            .WithMessage($"*superar los {CatalogueNumber.MaxLength} caracteres*");
    }

    [Fact]
    public void Equals_WithSameValueDifferentCasingAndSpacing_ShouldBeEqual()
    {
        // Arrange
        var cat1 = CatalogueNumber.Create("PCS 7027");
        var cat2 = CatalogueNumber.Create("  pcs 7027 ");

        // Assert
        cat1.Should().Be(cat2);
        (cat1 == cat2).Should().BeTrue();
    }
}

