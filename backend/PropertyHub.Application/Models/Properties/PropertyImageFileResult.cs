namespace PropertyHub.Application.Models.Properties;

public sealed record PropertyImageFileResult(
    Stream Content,
    string ContentType,
    long FileSizeBytes,
    bool IsPublic);
