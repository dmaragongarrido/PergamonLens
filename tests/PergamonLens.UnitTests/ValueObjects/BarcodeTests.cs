using FluentAssertions;
using PergamonLens.Domain.Exceptions;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.ValueObjects;

public class BarcodeTests
{
    [Theory]
    [InlineData("4006381333931")] // EAN-13 estándar
    [InlineData("036000291452")] // UPC-A estándar (12 dígitos)
    [InlineData("9780201379624")] // ISBN-13 de libro técnico
    [InlineData("96385074")] // EAN-8 estándar
    public void Create_WithValidBarcode_ShouldInstantiateSuccessfully(string validCode)
    {
        // Act
        var barcode = Barcode.Create(validCode);

        // Assert
        barcode.Should().NotBeNull();
        barcode.Value.Should().Be(validCode);
    }

    [Theory]
    [InlineData("0 36000 29145 2", "036000291452")]
    [InlineData("0-36000-29145-2", "036000291452")]
    [InlineData("4 006381 333931", "4006381333931")]
    public void Create_WithSpacesOrDashes_ShouldNormalizeDigits(string formatted, string expectedNormalized)
    {
        // Act
        var barcode = Barcode.Create(formatted);

        // Assert
        barcode.Value.Should().Be(expectedNormalized);
    }

    [Theory]
    [InlineData("4006381333932")] // Checksum incorrecto (debería ser 1)
    [InlineData("036000291459")] // Checksum incorrecto (debería ser 2)
    public void Create_WithInvalidChecksum_ShouldThrowInvalidBarcodeException(string invalidChecksum)
    {
        // Act
        Action act = () => Barcode.Create(invalidChecksum);

        // Assert
        act.Should().Throw<InvalidBarcodeException>()
            .WithMessage("*dígito de control*");
    }

    [Theory]
    [InlineData("12345")] // Demasiado corto
    [InlineData("1234567890")] // 10 dígitos (ni EAN-8 ni UPC-A ni EAN-13)
    [InlineData("123456789012345")] // 15 dígitos
    public void Create_WithInvalidLength_ShouldThrowInvalidBarcodeException(string invalidLength)
    {
        // Act
        Action act = () => Barcode.Create(invalidLength);

        // Assert
        act.Should().Throw<InvalidBarcodeException>()
            .WithMessage("*longitud*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ShouldThrowInvalidBarcodeException(string? emptyInput)
    {
        // Act
        Action act = () => Barcode.Create(emptyInput);

        // Assert
        act.Should().Throw<InvalidBarcodeException>()
            .WithMessage("*no puede estar vacío*");
    }

    [Fact]
    public void Create_WithAlphanumericCharacters_ShouldThrowInvalidBarcodeException()
    {
        // Arrange
        const string alphaNumeric = "400638133393A";

        // Act
        Action act = () => Barcode.Create(alphaNumeric);

        // Assert
        act.Should().Throw<InvalidBarcodeException>()
            .WithMessage("*no numéricos*");
    }

    [Fact]
    public void Equals_WithSameValueDifferentFormatting_ShouldBeEqual()
    {
        // Arrange
        var barcode1 = Barcode.Create("036000291452");
        var barcode2 = Barcode.Create("0 36000 29145 2");

        // Assert
        barcode1.Should().Be(barcode2);
        (barcode1 == barcode2).Should().BeTrue();
    }
}

