using Core;
using Reqnroll;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using Xunit;

namespace Specs.Steps;

[Binding]
public sealed class TrackerSteps : IDisposable
{
    private const string NoResultBody = """{"generationtime_ms":0.41663647}""";

    private readonly StubHandler _handler = new();
    private Func<HttpRequestMessage, HttpResponseMessage> _geocoding = NoAnswer;
    private Func<HttpRequestMessage, HttpResponseMessage> _forecast = NoAnswer;
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "forecast-" + Guid.NewGuid().ToString("N"));
    private string _database = "";
    private string _output = "";
    private string _error = "";
    private int _exitCode;

    public TrackerSteps() =>
        _handler.Answer = request => request.RequestUri?.Host is "api.open-meteo.com" ? _forecast(request) : _geocoding(request);

    public void Dispose()
    {
        _handler.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Given("a database file that does not exist yet")]
    public void GivenADatabaseFileThatDoesNotExistYet()
    {
        Directory.CreateDirectory(_directory);
        _database = Path.Combine(_directory, "forecast.db");
    }

    [Given("the geocoding service answers with one result named {string} at latitude {double} and longitude {double}")]
    public void GivenOneResult(string name, double latitude, double longitude)
    {
        string body = string.Create(CultureInfo.InvariantCulture, $$"""{"results":[{"id":1,"name":"{{name}}","latitude":{{latitude}},"longitude":{{longitude}},"country":"Somewhere"}],"generationtime_ms":0.2}""");
        _geocoding = _ => StubHandler.Json(body);
    }

    [Given("the geocoding service answers with no result")]
    public void GivenNoResult() => _geocoding = _ => StubHandler.Json(NoResultBody);

    [Given("the geocoding service answers with several results")]
    public void GivenSeveralResults()
    {
        const string body = """{"results":[{"id":4930956,"name":"Boston","latitude":42.35843,"longitude":-71.05977},{"id":2655095,"name":"Boston","latitude":52.97626,"longitude":-0.02626}],"generationtime_ms":0.3}""";
        _geocoding = _ => StubHandler.Json(body);
    }

    [Given("the geocoding service gives no answer")]
    public void GivenNoAnswer() => _geocoding = NoAnswer;

    [Given("the forecast service answers with temperature {double} {string} and wind speed {double} {string}")]
    public void GivenForecast(double temperature, string temperatureUnit, double windSpeed, string windSpeedUnit)
    {
        string body = string.Create(CultureInfo.InvariantCulture, $$$"""{"latitude":27.732864,"longitude":85.34831,"elevation":1293.0,"current_units":{"time":"iso8601","interval":"seconds","temperature_2m":"{{{temperatureUnit}}}","wind_speed_10m":"{{{windSpeedUnit}}}"},"current":{"time":"2026-10-06T11:30","interval":900,"temperature_2m":{{{temperature}}},"wind_speed_10m":{{{windSpeed}}}}}""");
        _forecast = _ => StubHandler.Json(body);
    }

    [Given("the forecast service gives no answer")]
    public void GivenForecastNoAnswer() => _forecast = NoAnswer;

    [Given("the forecast service answers with the status 400 and the reason {string}")]
    public void GivenForecastRefuses(string reason)
    {
        string body = $$"""{"reason":"{{reason}}","error":true}""";
        _forecast = _ => new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    [Given("a person ran the program with {string} on the database file")]
    public async Task GivenAPersonRan(string arguments) => await Run(arguments, withDatabase: true);

    [When("a person runs the program with {string} on the database file")]
    public async Task WhenAPersonRunsOnTheDatabase(string arguments) => await Run(arguments, withDatabase: true);

    [When("a person runs the program with {string}")]
    public async Task WhenAPersonRuns(string arguments) => await Run(arguments, withDatabase: false);

    [Then("the program sent the request {string}")]
    public void ThenSentTheRequest(string request) => Assert.Contains(request, _handler.Requests);

    [Then("the program sent no request")]
    public void ThenSentNoRequest() => Assert.Empty(_handler.Requests);

    [Then("the program sent no request to the forecast service")]
    public void ThenSentNoForecastRequest() => Assert.DoesNotContain(_handler.Requests, request => request.Contains("https://api.open-meteo.com", StringComparison.Ordinal));

    [Then("standard output is {string}")]
    public void ThenOutputIs(string expected) => Assert.Equal(expected, _output.TrimEnd());

    [Then("standard output is empty")]
    public void ThenOutputIsEmpty() => Assert.Equal("", _output);

    [Then("standard output has the lines")]
    public void ThenOutputHasTheLines(string lines) =>
        Assert.Equal(lines.ReplaceLineEndings("\n"), _output.ReplaceLineEndings("\n").TrimEnd());

    [Then("standard error is not empty")]
    public void ThenErrorIsNotEmpty() => Assert.NotEqual("", _error.Trim());

    [Then("standard error has a line that says no place has the name and holds {string}")]
    public void ThenErrorSaysNoPlace(string name) => AssertErrorLine("no place has the name", name);

    [Then("standard error has a line that says the name matches several places and holds {string}")]
    public void ThenErrorSaysSeveral(string name) => AssertErrorLine("matches several places", name);

    [Then("standard error has a line that says the service gave no answer")]
    public void ThenErrorSaysNoAnswer() => AssertErrorLine("gave no answer", "");

    [Then("standard error has a line that says no tracked location has the id and holds {string}")]
    public void ThenErrorSaysNoLocation(string id) => AssertErrorLine("no tracked location has the id", id);

    [Then("standard error has a line that says the forecast service gave no answer and holds {string}")]
    public void ThenErrorSaysForecastNoAnswer(string name) => AssertErrorLine("forecast service gave no answer", name);

    [Then("standard error has a line that says the forecast service refused the request and holds {string}")]
    public void ThenErrorSaysForecastRefused(string reason) => AssertErrorLine("forecast service refused the request", reason);

    [Then("the exit code is {int}")]
    public void ThenExitCodeIs(int expected) => Assert.Equal(expected, _exitCode);

    [Then("the exit code is not 0")]
    public void ThenExitCodeIsNotZero() => Assert.NotEqual(0, _exitCode);

    [Then("standard output holds the option {string} and its default value {string}")]
    public void ThenOutputHoldsOption(string option, string defaultValue) =>
        Assert.Contains(_output.Split('\n'), line => line.Contains(option, StringComparison.Ordinal) && line.Contains($"[default: {defaultValue}]", StringComparison.Ordinal));

    private static HttpResponseMessage NoAnswer(HttpRequestMessage request) => throw new HttpRequestException("No answer.");

    private void AssertErrorLine(string phrase, string name) =>
        Assert.Contains(
            _error.Split('\n'),
            line => line.Contains(phrase, StringComparison.OrdinalIgnoreCase) && line.Contains(name, StringComparison.Ordinal));

    private async Task Run(string arguments, bool withDatabase)
    {
        var args = new List<string>(arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (withDatabase)
        {
            args.Add("--database");
            args.Add(_database);
        }

        using var http = new HttpClient(_handler, disposeHandler: false);
        using var output = new StringWriter();
        using var error = new StringWriter();
        RootCommand root = TrackerCommand.Create(http);
        _exitCode = await root.Parse([.. args]).InvokeAsync(new InvocationConfiguration { Output = output, Error = error });
        _output = output.ToString();
        _error = error.ToString();
    }
}
