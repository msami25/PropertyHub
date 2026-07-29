namespace PropertyHub.Application.Models.Properties;

public sealed record PropertyImageUpload(
    string FileName,
    string ContentType,
    long Length,
    Stream Content);
