namespace PropertyHub.Infrastructure.External;

public sealed record OpenMeteoOptions(
    string BaseUrl,
    TimeSpan Timeout,
    TimeSpan CacheDuration);
