using Core;
using Xunit;

namespace Integration;

/// <summary>The answers that the build suite stubs, got from the real forecast service. No key is needed.</summary>
public sealed class ForecastTests
{
    private static async Task<CurrentWeather> Get(double latitude, double longitude)
    {
        using HttpClient http = Retry.Client();
        return await Retry.Run(() => new ForecastClient(http).GetCurrentAsync(latitude, longitude, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task KathmanduHasTheCurrentWeather()
    {
        CurrentWeather weather = await Get(27.70169, 85.3206);

        Assert.Equal(("°C", "km/h"), (weather.TemperatureUnit, weather.WindSpeedUnit));
    }

    [Fact]
    public async Task VaduzHasTheCurrentWeather()
    {
        CurrentWeather weather = await Get(47.14151, 9.52154);

        Assert.Equal(("°C", "km/h"), (weather.TemperatureUnit, weather.WindSpeedUnit));
    }

    [Fact]
    public async Task LatitudeNinetyNineIsRefused()
    {
        var e = await Assert.ThrowsAsync<ForecastRefusedException>(() => Get(99, 85.3206));

        Assert.Equal("Latitude must be in range of -90 to 90°. Given: 99.0.", e.Reason);
    }
}
