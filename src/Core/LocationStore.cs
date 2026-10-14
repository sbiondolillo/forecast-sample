using Microsoft.Data.Sqlite;

namespace Core;

/// <summary>The tracked locations in a SQLite database file.</summary>
public sealed class LocationStore(string path)
{
    /// <summary>Whether the database file exists.</summary>
    public bool Exists => File.Exists(path);

    /// <summary>Stores the place, creating the file and the table when they are new, and gives the stored location.</summary>
    public Location Add(Place place)
    {
        using SqliteConnection connection = Open(SqliteOpenMode.ReadWriteCreate);
        using (SqliteCommand create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE IF NOT EXISTS locations (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, latitude REAL NOT NULL, longitude REAL NOT NULL)";
            create.ExecuteNonQuery();
        }

        using SqliteCommand insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO locations (name, latitude, longitude) VALUES ($name, $latitude, $longitude) RETURNING id";
        insert.Parameters.AddWithValue("$name", place.Name);
        insert.Parameters.AddWithValue("$latitude", place.Latitude);
        insert.Parameters.AddWithValue("$longitude", place.Longitude);
        long id = (long)insert.ExecuteScalar()!;
        return new Location(id, place.Name, place.Latitude, place.Longitude);
    }

    /// <summary>The tracked locations in id order. A missing file holds none.</summary>
    public IReadOnlyList<Location> List()
    {
        if (!Exists)
        {
            return [];
        }

        using SqliteConnection connection = Open(SqliteOpenMode.ReadOnly);

        // A file with no table holds no locations.
        if (!HasTable(connection))
        {
            return [];
        }

        using SqliteCommand select = connection.CreateCommand();
        select.CommandText = "SELECT id, name, latitude, longitude FROM locations ORDER BY id";
        using SqliteDataReader reader = select.ExecuteReader();
        var locations = new List<Location>();
        while (reader.Read())
        {
            locations.Add(new Location(reader.GetInt64(0), reader.GetString(1), reader.GetDouble(2), reader.GetDouble(3)));
        }

        return locations;
    }

    /// <summary>The tracked location with the id, or null. A missing file holds none, and the lookup does not create it.</summary>
    public Location? Find(long id) => List().FirstOrDefault(location => location.Id == id);

    /// <summary>Removes the tracked location with the id and gives it, or gives null when none has the id. A missing file or a file with no table holds none, and the removal does not create the file.</summary>
    public Location? Remove(long id)
    {
        if (!Exists)
        {
            return null;
        }

        using SqliteConnection connection = Open(SqliteOpenMode.ReadWrite);
        if (!HasTable(connection))
        {
            return null;
        }

        using SqliteCommand delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM locations WHERE id = $id RETURNING id, name, latitude, longitude";
        delete.Parameters.AddWithValue("$id", id);
        using SqliteDataReader reader = delete.ExecuteReader();
        return reader.Read()
            ? new Location(reader.GetInt64(0), reader.GetString(1), reader.GetDouble(2), reader.GetDouble(3))
            : null;
    }

    private static bool HasTable(SqliteConnection connection)
    {
        using SqliteCommand probe = connection.CreateCommand();
        probe.CommandText = "SELECT 1 FROM sqlite_schema WHERE type = 'table' AND name = 'locations'";
        return probe.ExecuteScalar() is not null;
    }

    private SqliteConnection Open(SqliteOpenMode mode)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            Pooling = false,
        }.ToString());
        connection.Open();
        return connection;
    }
}
