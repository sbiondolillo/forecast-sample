using System.CommandLine;
using System.CommandLine.Invocation;

namespace Core;

/// <summary>The command tree of the console program: <c>add</c>, <c>list</c> and <c>forecast</c>.</summary>
public static class TrackerCommand
{
    private const string DefaultDatabase = "forecast.db";

    /// <summary>The root command. The geocoding and forecast requests go through the given client.</summary>
    public static RootCommand Create(HttpClient http)
    {
        var geocoding = new GeocodingClient(http);
        var root = new RootCommand("Tracks the places whose weather a person follows.");
        root.Subcommands.Add(CreateAdd(geocoding));
        root.Subcommands.Add(CreateList());
        root.Subcommands.Add(CreateForecast(new ForecastClient(http)));
        return root;
    }

    private static Option<string> DatabaseOption() => new("--database")
    {
        Description = "The SQLite file that holds the tracked locations.",
        DefaultValueFactory = _ => DefaultDatabase,
    };

    private static Command CreateAdd(GeocodingClient geocoding)
    {
        var name = new Argument<string>("name")
        {
            Description = "The name of the place to track.",
            Arity = ArgumentArity.ExactlyOne,
        };
        Option<string> database = DatabaseOption();
        var command = new Command("add", "Adds the place of a name to the tracked locations.");
        command.Arguments.Add(name);
        command.Options.Add(database);
        command.SetAction((result, cancellationToken) =>
            AddAsync(result, geocoding, result.GetRequiredValue(name), result.GetRequiredValue(database), cancellationToken));
        return command;
    }

    private static Command CreateList()
    {
        Option<string> database = DatabaseOption();
        var command = new Command("list", "Lists the tracked locations.");
        command.Options.Add(database);
        command.SetAction(result => List(result, result.GetRequiredValue(database)));
        return command;
    }

    private static Command CreateForecast(ForecastClient forecast)
    {
        var id = new Argument<long>("id")
        {
            Description = "The id of the tracked location.",
            Arity = ArgumentArity.ExactlyOne,
        };
        Option<string> database = DatabaseOption();
        var command = new Command("forecast", "Shows the current weather at a tracked location.");
        command.Arguments.Add(id);
        command.Options.Add(database);
        command.SetAction((result, cancellationToken) =>
            ForecastAsync(result, forecast, result.GetRequiredValue(id), result.GetRequiredValue(database), cancellationToken));
        return command;
    }

    private static async Task<int> ForecastAsync(ParseResult result, ForecastClient forecast, long id, string database, CancellationToken cancellationToken)
    {
        InvocationConfiguration configuration = result.InvocationConfiguration;
        Location? location = new LocationStore(database).Find(id);
        if (location is null)
        {
            await configuration.Error.WriteLineAsync($"No tracked location has the id {id}.").ConfigureAwait(false);
            return 1;
        }

        CurrentWeather weather;
        try
        {
            weather = await forecast.GetCurrentAsync(location.Latitude, location.Longitude, cancellationToken).ConfigureAwait(false);
        }
        catch (ForecastNoAnswerException)
        {
            await configuration.Error.WriteLineAsync($"The forecast service gave no answer for {location.Name}.").ConfigureAwait(false);
            return 1;
        }
        catch (ForecastRefusedException e)
        {
            await configuration.Error.WriteLineAsync(e.Message).ConfigureAwait(false);
            return 1;
        }

        await configuration.Output.WriteLineAsync($"{location.Name} {location.Coordinates}: {weather}").ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> AddAsync(ParseResult result, GeocodingClient geocoding, string name, string database, CancellationToken cancellationToken)
    {
        InvocationConfiguration configuration = result.InvocationConfiguration;
        IReadOnlyList<Place> places;
        try
        {
            places = await geocoding.SearchAsync(name, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is HttpRequestException || (e is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            await configuration.Error.WriteLineAsync($"The geocoding service gave no answer for \"{name}\".").ConfigureAwait(false);
            return 1;
        }

        switch (places.Count)
        {
            case 0:
                await configuration.Error.WriteLineAsync($"No place has the name \"{name}\".").ConfigureAwait(false);
                return 1;
            case > 1:
                await configuration.Error.WriteLineAsync($"The name \"{name}\" matches several places.").ConfigureAwait(false);
                return 1;
            default:
                break;
        }

        Location added = new LocationStore(database).Add(places[0]);
        await configuration.Output.WriteLineAsync($"Added {added.Name} {added.Coordinates}").ConfigureAwait(false);
        return 0;
    }

    private static int List(ParseResult result, string database)
    {
        TextWriter output = result.InvocationConfiguration.Output;
        IReadOnlyList<Location> locations = new LocationStore(database).List();
        if (locations.Count is 0)
        {
            output.WriteLine("No tracked locations.");
            return 0;
        }

        foreach (Location location in locations)
        {
            output.WriteLine($"{location.Id} {location.Name} {location.Coordinates}");
        }

        return 0;
    }
}
