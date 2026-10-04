using Core;
using Xunit;

namespace Specs.Unit;

public sealed class GeocodingClientTests
{
    [Fact]
    public async Task SearchSendsTheNameEscapedInTheQuery()
    {
        using var handler = new StubHandler { Answer = _ => StubHandler.Json("""{"generationtime_ms":0.1}""") };
        using var http = new HttpClient(handler);

        await new GeocodingClient(http).SearchAsync("Rio de Janeiro", TestContext.Current.CancellationToken);

        Assert.Equal(["GET https://geocoding-api.open-meteo.com/v1/search?name=Rio%20de%20Janeiro"], handler.Requests);
    }

    [Fact]
    public async Task SearchReadsTheOnlyResult()
    {
        using var handler = new StubHandler { Answer = _ => StubHandler.Json("""{"results":[{"id":1,"name":"Vaduz","latitude":47.14151,"longitude":9.52154}]}""") };
        using var http = new HttpClient(handler);

        IReadOnlyList<Place> places = await new GeocodingClient(http).SearchAsync("Vaduz", TestContext.Current.CancellationToken);

        Assert.Equal([new Place("Vaduz", 47.14151, 9.52154)], places);
    }

    [Fact]
    public void ParseAnAnswerWithoutResultsGivesNoPlace() =>
        Assert.Empty(GeocodingClient.Parse("""{"generationtime_ms":0.41663647}"""));

    [Fact]
    public void ParseSeveralResultsGiveSeveralPlaces() =>
        Assert.Equal(2, GeocodingClient.Parse("""{"results":[{"name":"A","latitude":1,"longitude":2},{"name":"B","latitude":3,"longitude":4}]}""").Count);

    [Fact]
    public async Task SearchAnErrorStatusThrows()
    {
        using var handler = new StubHandler { Answer = _ => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError) };
        using var http = new HttpClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => new GeocodingClient(http).SearchAsync("x", TestContext.Current.CancellationToken));
    }
}
