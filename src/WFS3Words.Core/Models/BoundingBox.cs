namespace WFS3Words.Core.Models;

/// <summary>
/// Represents a geographic bounding box defined by minimum and maximum coordinates.
/// </summary>
/// <param name="MinLatitude">Minimum latitude (south)</param>
/// <param name="MinLongitude">Minimum longitude (west)</param>
/// <param name="MaxLatitude">Maximum latitude (north)</param>
/// <param name="MaxLongitude">Maximum longitude (east)</param>
public record BoundingBox(
    double MinLatitude,
    double MinLongitude,
    double MaxLatitude,
    double MaxLongitude)
{
    /// <summary>
    /// Southwest corner of the bounding box.
    /// </summary>
    public GeoCoordinate SouthWest => new(MinLatitude, MinLongitude);

    /// <summary>
    /// Northeast corner of the bounding box.
    /// </summary>
    public GeoCoordinate NorthEast => new(MaxLatitude, MaxLongitude);

    /// <summary>
    /// Width of the bounding box in degrees of longitude.
    /// </summary>
    public double Width => MaxLongitude - MinLongitude;

    /// <summary>
    /// Height of the bounding box in degrees of latitude.
    /// </summary>
    public double Height => MaxLatitude - MinLatitude;

    /// <summary>
    /// Validates that the bounding box has valid coordinates and proper min/max ordering.
    /// </summary>
    public bool IsValid() =>
        MinLatitude >= -90 && MinLatitude <= 90 &&
        MaxLatitude >= -90 && MaxLatitude <= 90 &&
        MinLongitude >= -180 && MinLongitude <= 180 &&
        MaxLongitude >= -180 && MaxLongitude <= 180 &&
        MinLatitude <= MaxLatitude &&
        MinLongitude <= MaxLongitude;

    /// <summary>
    /// Checks if a coordinate is within this bounding box.
    /// </summary>
    public bool Contains(GeoCoordinate coordinate) =>
        coordinate.Latitude >= MinLatitude &&
        coordinate.Latitude <= MaxLatitude &&
        coordinate.Longitude >= MinLongitude &&
        coordinate.Longitude <= MaxLongitude;

    /// <summary>
    /// Returns the bounding box in WFS format: minx,miny,maxx,maxy
    /// </summary>
    public override string ToString() =>
        $"{MinLongitude},{MinLatitude},{MaxLongitude},{MaxLatitude}";

    /// <summary>
    /// Parses a WFS-format bounding box string (minx,miny,maxx,maxy).
    /// </summary>
    public static BoundingBox? Parse(string bbox)
    {
        var parts = bbox.Split(',');
        if (parts.Length != 4)
            return null;

        if (!double.TryParse(parts[0], out var minLon) ||
            !double.TryParse(parts[1], out var minLat) ||
            !double.TryParse(parts[2], out var maxLon) ||
            !double.TryParse(parts[3], out var maxLat))
            return null;

        var box = new BoundingBox(minLat, minLon, maxLat, maxLon);
        return box.IsValid() ? box : null;
    }
}
