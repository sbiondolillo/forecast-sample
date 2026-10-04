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
        using SqliteCommand select = connection.CreateCommand();
        select.CommandText = "SELECT id, name, latitude, longitude FROM locations ORDER BY id";
        try
        {
            using SqliteDataReader reader = select.ExecuteReader();
            var locations = new List<Location>();
            while (reader.Read())
            {
                locations.Add(new Location(reader.GetInt64(0), reader.GetString(1), reader.GetDouble(2), reader.GetDouble(3)));
            }

            return locations;
        }
        catch (SqliteException e) when (e.SqliteErrorCode is 1)
        {
            // A file with no table holds no locations.
            return [];
        }
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
