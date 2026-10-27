using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PergamonLens.Infrastructure.ExternalServices.Vision;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace PergamonLens.UnitTests.Infrastructure;

public sealed class LocalBarcodeScannerTests
{
    private readonly LocalBarcodeScanner _sut;

    public LocalBarcodeScannerTests()
    {
        _sut = new LocalBarcodeScanner(NullLogger<LocalBarcodeScanner>.Instance);
    }

    [Fact]
    public void Scan_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _sut.Scan(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Scan_WithNonBarcodeImage_ShouldReturnNull()
    {
        // Arrange: crear una imagen PNG lisa sin código de barras (100x100 píxeles rojos)
        using var bitmap = new SKBitmap(100, 100);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Red);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = data.AsStream();

        // Act
        var result = _sut.Scan(stream);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Scan_WithValidEan13BarcodeImage_ShouldDecodeCorrectly()
    {
        // Arrange: generar una imagen de código de barras EAN-13 sintética en memoria (con checksum EAN-13 válido)
        const string expectedBarcode = "8435383600011";
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.EAN_13,
            Options = new EncodingOptions
            {
                Width = 300,
                Height = 150,
                Margin = 10
            }
        };

        using var bitmap = writer.Write(expectedBarcode);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = data.AsStream();

        // Act
        var result = _sut.Scan(stream);

        // Assert
        result.Should().NotBeNull();
        result!.Barcode.Should().Be(expectedBarcode);
        result.BarcodeFormat.Should().Be("EAN_13");
    }

    [Fact]
    public void Scan_WithValidCode128BarcodeImage_ShouldDecodeCorrectly()
    {
        // Arrange: generar un código Code 128 (común en catálogos y números de serie)
        const string expectedBarcode = "PERGAMON-2026";
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = 400,
                Height = 120,
                Margin = 10
            }
        };

        using var bitmap = writer.Write(expectedBarcode);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = data.AsStream();

        // Act
        var result = _sut.Scan(stream);

        // Assert
        result.Should().NotBeNull();
        result!.Barcode.Should().Be(expectedBarcode);
        result.BarcodeFormat.Should().Be("CODE_128");
    }
}
