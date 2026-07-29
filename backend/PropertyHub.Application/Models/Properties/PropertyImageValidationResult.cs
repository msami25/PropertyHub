namespace PropertyHub.Application.Models.Properties;

public sealed record PropertyImageValidationResult(
    bool IsValid,
    string? Error = null,
    string? CanonicalContentType = null,
    string? CanonicalExtension = null,
    string? SafeOriginalFileName = null,
    int? Width = null,
    int? Height = null)
{
    public static PropertyImageValidationResult Invalid(string error) => new(false, error);

    public static PropertyImageValidationResult Valid(
        string contentType,
        string extension,
        string originalFileName,
        int width,
        int height) =>
        new(true, CanonicalContentType: contentType, CanonicalExtension: extension,
            SafeOriginalFileName: originalFileName, Width: width, Height: height);
}
