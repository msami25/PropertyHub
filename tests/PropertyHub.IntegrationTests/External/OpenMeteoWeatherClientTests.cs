using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyHub.Infrastructure.External;

namespace PropertyHub.IntegrationTests.External;

public sealed class OpenMeteoWeatherClientTests
{
    private static readonly Guid CityId = Guid.NewGuid();

    [Fact]
    public async Task GetCurrentAsync_ShouldMapCurrentConditionsAndCoordinates()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.Query.Should().Contain("latitude=31.5204");
            request.RequestUri.Query.Should().Contain("longitude=74.3587");
            request.RequestUri.Query.Should().Contain(
                "current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m");
            request.RequestUri.Query.Should().Contain("timezone=UTC");
            return Task.FromResult(JsonResponse(WeatherJson()));
        });
        var client = CreateClient(handler);

        var weather = await client.GetCurrentAsync(
            CityId,
            31.520400m,
            74.358700m,
            CancellationToken.None);

        weather.Should().NotBeNull();
        weather!.TemperatureCelsius.Should().Be(32.4m);
        weather.RelativeHumidityPercent.Should().Be(58);
        weather.WindSpeedKilometresPerHour.Should().Be(12.3m);
        weather.Condition.Should().Be("Partly cloudy");
        weather.ObservedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldCacheOnlySuccessfulResultsByCity()
    {
        var calls = 0;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(JsonResponse(WeatherJson()));
        });
        var client = CreateClient(handler);

        var first = await client.GetCurrentAsync(
            CityId,
            31.520400m,
            74.358700m,
            CancellationToken.None);
        var second = await client.GetCurrentAsync(
            CityId,
            31.520400m,
            74.358700m,
            CancellationToken.None);

        first.Should().BeSameAs(second);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldReturnNullOnTimeout()
    {
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return JsonResponse(WeatherJson());
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://weather.test/"),
            Timeout = TimeSpan.FromMilliseconds(50)
        };
        var client = CreateClient(handler, httpClient);

        var weather = await client.GetCurrentAsync(
            CityId,
            31.520400m,
            74.358700m,
            CancellationToken.None);

        weather.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "{}")]
    [InlineData(HttpStatusCode.OK, "{not-json")]
    [InlineData(HttpStatusCode.OK, """{"current":{"time":null}}""")]
    public async Task GetCurrentAsync_ShouldReturnNullForFailureOrInvalidResponse(
        HttpStatusCode statusCode,
        string body)
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(JsonResponse(body, statusCode)));
        var client = CreateClient(handler);

        var weather = await client.GetCurrentAsync(
            CityId,
            31.520400m,
            74.358700m,
            CancellationToken.None);

        weather.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldNotCacheFailures()
    {
        var calls = 0;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(JsonResponse("{}", HttpStatusCode.ServiceUnavailable));
        });
        var client = CreateClient(handler);

        await client.GetCurrentAsync(CityId, 1, 1, CancellationToken.None);
        await client.GetCurrentAsync(CityId, 1, 1, CancellationToken.None);

        calls.Should().Be(2);
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldReturnNullForUnsupportedResponseFormat()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html>Unavailable</html>", Encoding.UTF8, "text/html")
            }));
        var client = CreateClient(handler);

        var weather = await client.GetCurrentAsync(
            CityId,
            31.520400m,
            74.358700m,
            CancellationToken.None);

        weather.Should().BeNull();
    }

    private static OpenMeteoWeatherClient CreateClient(
        HttpMessageHandler handler,
        HttpClient? httpClient = null) =>
        new(
            httpClient ?? new HttpClient(handler)
            {
                BaseAddress = new Uri("https://weather.test/"),
                Timeout = TimeSpan.FromSeconds(5)
            },
            new MemoryCache(new MemoryCacheOptions()),
            new OpenMeteoOptions(
                "https://weather.test",
                TimeSpan.FromSeconds(5),
                TimeSpan.FromMinutes(30)),
            NullLogger<OpenMeteoWeatherClient>.Instance);

    private static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static string WeatherJson() =>
        """
        {
          "current": {
            "time": "2026-07-29T06:00",
            "temperature_2m": 32.4,
            "relative_humidity_2m": 58,
            "weather_code": 2,
            "wind_speed_10m": 12.3
          }
        }
        """;

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            response(request, cancellationToken);
    }
}
