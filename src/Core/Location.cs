using System.Globalization;

namespace Core;

/// <summary>A tracked location.</summary>
public sealed record Location(long Id, string Name, double Latitude, double Longitude)
{
    /// <summary>The coordinates as the program prints them: <c>(27.70169, 85.3206)</c>.</summary>
    public string Coordinates => string.Create(CultureInfo.InvariantCulture, $"({Latitude}, {Longitude})");
}
