using FluentAssertions;
using PropertyHub.Application.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PropertyHub.UnitTests.Services;

public sealed class PropertyImageValidatorTests
{
    private readonly PropertyImageValidator _validator = new();

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    public void Validate_ShouldAcceptJpegWithMatchingExtensionAndSignature(
        string fileName,
        string contentType)
    {
        var content = CreateImage("jpeg");

        var result = _validator.Validate(fileName, contentType, content.Length, content);

        result.IsValid.Should().BeTrue();
        result.CanonicalExtension.Should().Be(".jpg");
    }

    [Fact]
    public void Validate_ShouldAcceptPngAndStripClientPath()
    {
        var content = CreateImage("png");

        var result = _validator.Validate(
            @"C:\client\property.png",
            "image/png",
            content.Length,
            content);

        result.IsValid.Should().BeTrue();
        result.SafeOriginalFileName.Should().Be("property.png");
    }

    [Fact]
    public void Validate_ShouldAcceptWebPWithMatchingSignature()
    {
        var content = CreateImage("webp");

        var result = _validator.Validate(
            "property.webp",
            "image/webp",
            content.Length,
            content);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("property.exe", "image/jpeg")]
    [InlineData("property.png", "application/octet-stream")]
    public void Validate_ShouldRejectUnsupportedExtensionOrDeclaredContentType(
        string fileName,
        string contentType)
    {
        byte[] content = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        var result = _validator.Validate(fileName, contentType, content.Length, content);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldRejectSignatureMismatch()
    {
        byte[] content = [0x4D, 0x5A, 0x90, 0x00];

        var result = _validator.Validate(
            "property.jpg",
            "image/jpeg",
            content.Length,
            content);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("signature");
    }

    [Fact]
    public void Validate_ShouldRejectOversizedDeclaredLength()
    {
        byte[] content = [0xFF, 0xD8, 0xFF];

        var result = _validator.Validate(
            "property.jpg",
            "image/jpeg",
            PropertyImageValidator.MaximumFileSizeBytes + 1L,
            content);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("5 MB");
    }

    [Fact]
    public void Validate_ShouldRejectExcessiveDimensions()
    {
        var content = CreateImage("png", PropertyImageValidator.MaximumDimensionPixels + 1, 1);

        var result = _validator.Validate(
            "property.png",
            "image/png",
            content.Length,
            content);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("8,000 pixels");
    }

    [Fact]
    public void Validate_ShouldRejectTruncatedImageEvenWhenMagicBytesMatch()
    {
        byte[] content = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        var result = _validator.Validate(
            "property.png",
            "image/png",
            content.Length,
            content);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("invalid");
    }

    private static byte[] CreateImage(string format, int width = 1, int height = 1)
    {
        using var image = new Image<Rgba32>(width, height);
        using var stream = new MemoryStream();
        switch (format)
        {
            case "jpeg":
                image.SaveAsJpeg(stream);
                break;
            case "webp":
                image.SaveAsWebp(stream);
                break;
            default:
                image.SaveAsPng(stream);
                break;
        }
        return stream.ToArray();
    }
}
