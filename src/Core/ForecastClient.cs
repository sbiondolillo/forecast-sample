using System.Globalization;
using System.Text.Json;

namespace Core;

/// <summary>Asks the forecast service of Open-Meteo for the current weather at coordinates.</summary>
public sealed class ForecastClient(HttpClient http)
{
    private const string Endpoint = "https://api.open-meteo.com/v1/forecast";

    /// <summary>The current weather at the coordinates.</summary>
    /// <exception cref="ForecastNoAnswerException">The request got no answer.</exception>
    /// <exception cref="ForecastRefusedException">The service refused the request.</exception>
    public async Task<CurrentWeather> GetCurrentAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(string.Create(CultureInfo.InvariantCulture, $"{Endpoint}?latitude={latitude}&longitude={longitude}&current=temperature_2m,wind_speed_10m"));
        try
        {
            using HttpResponseMessage response = await http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new ForecastRefusedException(ReadReason(body) ?? $"status {(int)response.StatusCode}");
            }

            return Parse(body);
        }
        catch (Exception e) when (e is HttpRequestException || (e is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new ForecastNoAnswerException(e);
        }
    }

    /// <summary>Reads the current weather from the JSON body of a 200 answer.</summary>
    public static CurrentWeather Parse(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement current = document.RootElement.GetProperty("current");
        JsonElement units = document.RootElement.GetProperty("current_units");
        return new CurrentWeather(
            current.GetProperty("temperature_2m").GetDouble(),
            units.GetProperty("temperature_2m").GetString() ?? "",
            current.GetProperty("wind_speed_10m").GetDouble(),
            units.GetProperty("wind_speed_10m").GetString() ?? "");
    }

    private static string? ReadReason(string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            return document.RootElement.ValueKind is JsonValueKind.Object
                && document.RootElement.TryGetProperty("reason", out JsonElement reason)
                && reason.ValueKind is JsonValueKind.String
                ? reason.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>The current weather, with the unit text of the service.</summary>
public sealed record CurrentWeather(double Temperature, string TemperatureUnit, double WindSpeed, string WindSpeedUnit)
{
    /// <summary>The weather as the program prints it: <c>20.4 °C, wind 1.9 km/h</c>.</summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Temperature} {TemperatureUnit}, wind {WindSpeed} {WindSpeedUnit}");
}

/// <summary>The forecast service gave no answer.</summary>
public sealed class ForecastNoAnswerException(Exception inner) : Exception("The forecast service gave no answer.", inner);

/// <summary>The forecast service refused the request.</summary>
public sealed class ForecastRefusedException(string reason) : Exception($"The forecast service refused the request: {reason}")
{
    /// <summary>The reason that the service gave.</summary>
    public string Reason { get; } = reason;
}
