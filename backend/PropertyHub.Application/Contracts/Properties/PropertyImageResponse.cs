namespace PropertyHub.Application.Contracts.Properties;

public sealed record PropertyImageResponse(
    Guid Id,
    string Url,
    byte SortOrder,
    bool IsPrimary,
    string ContentType,
    long FileSizeBytes);
