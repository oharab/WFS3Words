namespace WFS3Words.Core.Models;

/// <summary>
/// Represents a geographic coordinate with latitude and longitude.
/// </summary>
/// <param name="Latitude">Latitude in decimal degrees (-90 to 90)</param>
/// <param name="Longitude">Longitude in decimal degrees (-180 to 180)</param>
public record GeoCoordinate(double Latitude, double Longitude)
{
    /// <summary>
    /// Validates that the coordinate values are within valid ranges.
    /// </summary>
    public bool IsValid() =>
        Latitude >= -90 && Latitude <= 90 &&
        Longitude >= -180 && Longitude <= 180;

    /// <summary>
    /// Returns the coordinate as a comma-separated string (latitude,longitude).
    /// </summary>
    public override string ToString() => $"{Latitude},{Longitude}";
}
