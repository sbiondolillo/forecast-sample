using System.Text.Json;

namespace Core;

/// <summary>Asks the geocoding service of Open-Meteo for the places of a name.</summary>
public sealed class GeocodingClient(HttpClient http)
{
    private const string Endpoint = "https://geocoding-api.open-meteo.com/v1/search";

    /// <summary>The places that the service gives for the name. An answer with no <c>results</c> gives an empty list.</summary>
    /// <exception cref="HttpRequestException">The request got no answer, or an answer with an error status.</exception>
    public async Task<IReadOnlyList<Place>> SearchAsync(string name, CancellationToken cancellationToken = default)
    {
        var uri = new Uri($"{Endpoint}?name={Uri.EscapeDataString(name)}");
        using HttpResponseMessage response = await http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return Parse(body);
    }

    /// <summary>Reads the places from the JSON body of an answer.</summary>
    public static IReadOnlyList<Place> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("results", out JsonElement results) || results.ValueKind is not JsonValueKind.Array)
        {
            return [];
        }

        return [.. results.EnumerateArray().Select(item => new Place(
            item.GetProperty("name").GetString() ?? "",
            item.GetProperty("latitude").GetDouble(),
            item.GetProperty("longitude").GetDouble()))];
    }
}

/// <summary>A place that the geocoding service found.</summary>
public sealed record Place(string Name, double Latitude, double Longitude);
