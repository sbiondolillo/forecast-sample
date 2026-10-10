using Core;
using Xunit;

namespace Integration;

/// <summary>The answers that the build suite stubs, got from the real geocoding service. No key is needed.</summary>
public sealed class GeocodingTests
{
    private static async Task<IReadOnlyList<Place>> Search(string name)
    {
        using HttpClient http = Retry.Client();
        return await Retry.Run(() => new GeocodingClient(http).SearchAsync(name, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task KathmanduHasOneResult() =>
        Assert.Equal([new Place("Kathmandu", 27.70169, 85.3206)], await Search("Kathmandu"));

    [Fact]
    public async Task VaduzHasOneResult() =>
        Assert.Equal([new Place("Vaduz", 47.14151, 9.52154)], await Search("Vaduz"));

    [Fact]
    public async Task BostonHasSeveralResults() =>
        Assert.True((await Search("Boston")).Count > 1);

    [Fact]
    public async Task UnknownNameHasNoResultsField()
    {
        using HttpClient http = Retry.Client();
        using HttpResponseMessage response = await Retry.Run(() => http.GetAsync(
            new Uri("https://geocoding-api.open-meteo.com/v1/search?name=Zzzzqqqxxx"), TestContext.Current.CancellationToken));

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("\"results\"", body, StringComparison.Ordinal);
        Assert.Empty(GeocodingClient.Parse(body));
    }
}
