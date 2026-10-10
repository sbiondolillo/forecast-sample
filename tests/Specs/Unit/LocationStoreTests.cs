using Core;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Specs.Unit;

public sealed class LocationStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "forecast-" + Guid.NewGuid().ToString("N"));

    public LocationStoreTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private LocationStore Store() => new(Path.Combine(_directory, "forecast.db"));

    [Fact]
    public void ListAMissingFileHoldsNoLocationAndStaysMissing()
    {
        LocationStore store = Store();

        Assert.Empty(store.List());
        Assert.False(store.Exists);
    }

    [Fact]
    public void ListAFileWithoutTheTableHoldsNoLocation()
    {
        string path = Path.Combine(_directory, "forecast.db");
        Execute(path, "CREATE TABLE other (id INTEGER)");

        Assert.Empty(new LocationStore(path).List());
    }

    [Fact]
    public void ListATableMissingAColumnSurfacesTheError()
    {
        string path = Path.Combine(_directory, "forecast.db");
        Execute(path, "CREATE TABLE locations (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");

        Assert.Throws<SqliteException>(() => new LocationStore(path).List());
    }

    private static void Execute(string path, string sql)
    {
        using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [Fact]
    public void AddAssignsIdsInOrderAndListKeepsThem()
    {
        LocationStore store = Store();

        Location first = store.Add(new Place("Kathmandu", 27.70169, 85.3206));
        Location second = store.Add(new Place("Vaduz", 47.14151, 9.52154));

        Assert.Equal([1L, 2L], [first.Id, second.Id]);
        Assert.Equal([first, second], Store().List());
    }

    [Fact]
    public void FindGivesTheLocationOfTheIdOrNull()
    {
        LocationStore store = Store();
        store.Add(new Place("Kathmandu", 27.70169, 85.3206));
        Location vaduz = store.Add(new Place("Vaduz", 47.14151, 9.52154));

        Assert.Equal(vaduz, store.Find(2));
        Assert.Null(store.Find(3));
    }

    [Fact]
    public void FindInAMissingFileGivesNullAndDoesNotCreateTheFile()
    {
        LocationStore store = Store();

        Assert.Null(store.Find(1));
        Assert.False(store.Exists);
    }

    [Fact]
    public void CoordinatesPrintInTheShortestRoundTripForm() =>
        Assert.Equal("(27.70169, 85.3206)", new Location(1, "Kathmandu", 27.70169, 85.3206).Coordinates);
}
