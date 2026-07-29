using PropertyHub.Application.Models.Properties;
using SixLabors.ImageSharp;

namespace PropertyHub.Application.Services;

public sealed class PropertyImageValidator
{
    public const int MaximumImageCount = 5;
    public const int MaximumFileSizeBytes = 5 * 1024 * 1024;
    public const int MaximumDimensionPixels = 8_000;
    public const long MaximumPixelCount = 40_000_000;

    private static readonly IReadOnlyDictionary<string, string> ContentTypesByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    public PropertyImageValidationResult Validate(
        string fileName,
        string declaredContentType,
        long declaredLength,
        ReadOnlySpan<byte> content)
    {
        if (declaredLength is <= 0 or > MaximumFileSizeBytes
            || content.Length is <= 0 or > MaximumFileSizeBytes)
        {
            return PropertyImageValidationResult.Invalid(
                "Each image must contain data and be no larger than 5 MB.");
        }

        var safeFileName = Path.GetFileName(fileName.Replace('\\', '/'));
        var extension = Path.GetExtension(safeFileName);
        if (string.IsNullOrWhiteSpace(safeFileName)
            || !ContentTypesByExtension.TryGetValue(extension, out var expectedContentType))
        {
            return PropertyImageValidationResult.Invalid(
                "Only JPEG, PNG, and WebP image extensions are allowed.");
        }

        if (!string.Equals(declaredContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return PropertyImageValidationResult.Invalid(
                "The declared image content type does not match its extension.");
        }

        var detectedContentType = DetectContentType(content);
        if (!string.Equals(detectedContentType, expectedContentType, StringComparison.Ordinal))
        {
            return PropertyImageValidationResult.Invalid(
                "The file signature does not match the declared image type.");
        }

        var canonicalExtension = expectedContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new InvalidOperationException("Unsupported image content type.")
        };

        int width;
        int height;
        try
        {
            using var image = Image.Load(content);
            width = image.Width;
            height = image.Height;
        }
        catch (UnknownImageFormatException)
        {
            return PropertyImageValidationResult.Invalid(
                "The file is not a decodable JPEG, PNG, or WebP image.");
        }
        catch (InvalidImageContentException)
        {
            return PropertyImageValidationResult.Invalid(
                "The image content is incomplete or invalid.");
        }

        if (width is < 1 or > MaximumDimensionPixels
            || height is < 1 or > MaximumDimensionPixels
            || (long)width * height > MaximumPixelCount)
        {
            return PropertyImageValidationResult.Invalid(
                "Image dimensions must be at most 8,000 pixels per side and 40 megapixels.");
        }

        return PropertyImageValidationResult.Valid(
            expectedContentType,
            canonicalExtension,
            safeFileName,
            width,
            height);
    }

    private static string? DetectContentType(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 3
            && content[0] == 0xFF
            && content[1] == 0xD8
            && content[2] == 0xFF)
        {
            return "image/jpeg";
        }

        ReadOnlySpan<byte> pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (content.StartsWith(pngSignature))
        {
            return "image/png";
        }

        if (content.Length >= 12
            && content[..4].SequenceEqual("RIFF"u8)
            && content.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        return null;
    }
}
