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
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "forecast-" + Guid.NewGuid().ToString("N"));
    private string _database = "";
    private string _output = "";
    private string _error = "";
    private int _exitCode;

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
        _handler.Answer = _ => StubHandler.Json(body);
    }

    [Given("the geocoding service answers with no result")]
    public void GivenNoResult() => _handler.Answer = _ => StubHandler.Json(NoResultBody);

    [Given("the geocoding service answers with several results")]
    public void GivenSeveralResults()
    {
        const string body = """{"results":[{"id":4930956,"name":"Boston","latitude":42.35843,"longitude":-71.05977},{"id":2655095,"name":"Boston","latitude":52.97626,"longitude":-0.02626}],"generationtime_ms":0.3}""";
        _handler.Answer = _ => StubHandler.Json(body);
    }

    [Given("the geocoding service gives no answer")]
    public void GivenNoAnswer() => _handler.Answer = _ => throw new HttpRequestException("No answer.");

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

    [Then("the exit code is {int}")]
    public void ThenExitCodeIs(int expected) => Assert.Equal(expected, _exitCode);

    [Then("the exit code is not 0")]
    public void ThenExitCodeIsNotZero() => Assert.NotEqual(0, _exitCode);

    [Then("standard output holds the option {string} and its default value {string}")]
    public void ThenOutputHoldsOption(string option, string defaultValue) =>
        Assert.Contains(_output.Split('\n'), line => line.Contains(option, StringComparison.Ordinal) && line.Contains($"[default: {defaultValue}]", StringComparison.Ordinal));

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
