using FluentAssertions;
using PergamonLens.Domain.Exceptions;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.ValueObjects;

public class ReleaseYearTests
{
    [Theory]
    [InlineData(1877)] // Invención del fonógrafo
    [InlineData(1967)] // Sgt. Pepper's Lonely Hearts Club Band
    [InlineData(1973)] // The Dark Side of the Moon
    [InlineData(1991)] // Nevermind
    [InlineData(2026)] // Año actual
    public void Create_WithValidYear_ShouldInstantiateSuccessfully(int validYear)
    {
        // Act
        var releaseYear = ReleaseYear.Create(validYear);

        // Assert
        releaseYear.Should().NotBeNull();
        releaseYear.Value.Should().Be(validYear);
    }

    [Fact]
    public void Create_WithYearBefore1877_ShouldThrowInvalidReleaseYearException()
    {
        // Arrange
        const int beforeInvention = 1876;

        // Act
        Action act = () => ReleaseYear.Create(beforeInvention);

        // Assert
        act.Should().Throw<InvalidReleaseYearException>()
            .WithMessage("*entre 1877 y *");
    }

    [Fact]
    public void Create_WithYearFarInTheFuture_ShouldThrowInvalidReleaseYearException()
    {
        // Arrange
        int farFuture = DateTime.UtcNow.Year + 2;

        // Act
        Action act = () => ReleaseYear.Create(farFuture);

        // Assert
        act.Should().Throw<InvalidReleaseYearException>()
            .WithMessage("*entre 1877 y *");
    }

    [Fact]
    public void Equals_WithSameYear_ShouldBeEqual()
    {
        // Arrange
        var year1 = ReleaseYear.Create(1973);
        var year2 = ReleaseYear.Create(1973);

        // Assert
        year1.Should().Be(year2);
        (year1 == year2).Should().BeTrue();
    }
}

