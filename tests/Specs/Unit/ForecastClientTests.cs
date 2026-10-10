using Core;
using System.Globalization;
using Xunit;

namespace Specs.Unit;

public sealed class ForecastClientTests
{
    private const string Body = """{"latitude":27.732864,"longitude":85.34831,"generationtime_ms":0.1,"elevation":1293.0,"current_units":{"time":"iso8601","interval":"seconds","temperature_2m":"°C","wind_speed_10m":"km/h"},"current":{"time":"2026-10-06T11:30","interval":900,"temperature_2m":20.4,"wind_speed_10m":1.9}}""";

    [Fact]
    public async Task GetCurrentSendsTheCoordinatesInTheInvariantCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            using var handler = new StubHandler { Answer = _ => StubHandler.Json(Body) };
            using var http = new HttpClient(handler);

            await new ForecastClient(http).GetCurrentAsync(27.70169, 85.3206, TestContext.Current.CancellationToken);

            Assert.Equal(["GET https://api.open-meteo.com/v1/forecast?latitude=27.70169&longitude=85.3206&current=temperature_2m,wind_speed_10m"], handler.Requests);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task GetCurrentReadsTheWeatherAndTheUnits()
    {
        using var handler = new StubHandler { Answer = _ => StubHandler.Json(Body) };
        using var http = new HttpClient(handler);

        CurrentWeather weather = await new ForecastClient(http).GetCurrentAsync(1, 2, TestContext.Current.CancellationToken);

        Assert.Equal(new CurrentWeather(20.4, "°C", 1.9, "km/h"), weather);
        Assert.Equal("20.4 °C, wind 1.9 km/h", weather.ToString());
    }

    [Fact]
    public async Task GetCurrentAStatus400ThrowsARefusalWithTheReason()
    {
        using var handler = new StubHandler { Answer = _ => Refusal("""{"reason":"Latitude must be in range of -90 to 90°. Given: 99.0.","error":true}""") };
        using var http = new HttpClient(handler);

        ForecastRefusedException e = await Assert.ThrowsAsync<ForecastRefusedException>(() => new ForecastClient(http).GetCurrentAsync(99, 2, TestContext.Current.CancellationToken));

        Assert.Equal("Latitude must be in range of -90 to 90°. Given: 99.0.", e.Reason);
    }

    [Fact]
    public async Task GetCurrentARefusalWithoutReasonGivesTheStatus()
    {
        using var handler = new StubHandler { Answer = _ => Refusal("not json") };
        using var http = new HttpClient(handler);

        ForecastRefusedException e = await Assert.ThrowsAsync<ForecastRefusedException>(() => new ForecastClient(http).GetCurrentAsync(1, 2, TestContext.Current.CancellationToken));

        Assert.Equal("status 400", e.Reason);
    }

    [Fact]
    public async Task GetCurrentATransportFailureThrowsNoAnswer()
    {
        using var handler = new StubHandler();
        using var http = new HttpClient(handler);

        await Assert.ThrowsAsync<ForecastNoAnswerException>(() => new ForecastClient(http).GetCurrentAsync(1, 2, TestContext.Current.CancellationToken));
    }

    private static HttpResponseMessage Refusal(string body) => new(System.Net.HttpStatusCode.BadRequest)
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
    };
}
