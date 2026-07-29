# ADR-0005: Use resilient Open-Meteo weather based on Property City coordinates

## Status

Accepted

## Context

The Gate requires a real external API used meaningfully, with timeout handling, error handling, and
caching. A Property already belongs to a normalized City, so City latitude and longitude provide
stable location input without requiring a paid map SDK.

External weather availability must not determine whether a Property detail page succeeds.

## Decision

Store latitude and longitude on `City`. For an approved and available Property, use
`PropertyWeatherService` to pass the Property City ID and coordinates to a typed
`IOpenMeteoWeatherClient`.

Configure the typed `HttpClient` through `HttpClientFactory`. Clamp the configurable timeout to
one through 30 seconds, with a five-second default. Map Open-Meteo current temperature, humidity,
weather code, wind, and UTC observation time into provider-neutral application data.

Cache only successful observations by City, with a 30-minute default. Convert timeouts, HTTP
failures, non-success status codes, invalid measurements, malformed JSON, and unsupported response
formats into a safe unavailable result.

## Consequences

- Weather is location-specific and useful on the Property detail page.
- Several Properties in one City share a cached observation.
- Provider failures do not break Property APIs, SSR, hydration, or page content.
- Failure results are not cached, allowing fast recovery.
- In-memory caching is per API process and resets on container recreation.
- The application intentionally hides unnecessary provider internals from users.
