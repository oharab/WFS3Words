namespace WFS3Words.Core.Models;

/// <summary>
/// Represents a What3Words location with its 3-word address and associated metadata.
/// </summary>
/// <param name="Words">The 3-word address (e.g., "filled.count.soap")</param>
/// <param name="Coordinates">The center point coordinates</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code</param>
/// <param name="Square">The bounding box of the 3x3m grid square</param>
/// <param name="NearestPlace">Nearest populated place (optional)</param>
/// <param name="Language">Language code of the 3-word address</param>
/// <param name="Map">URL to What3Words map (optional)</param>
public record What3WordsLocation(
    string Words,
    GeoCoordinate Coordinates,
    string Country,
    BoundingBox Square,
    string? NearestPlace = null,
    string Language = "en",
    string? Map = null);

/// <summary>
/// Represents the bounding box (square) information from the What3Words API.
/// </summary>
/// <param name="SouthWest">Southwest corner of the square</param>
/// <param name="NorthEast">Northeast corner of the square</param>
public record What3WordsSquare(
    GeoCoordinate SouthWest,
    GeoCoordinate NorthEast)
{
    /// <summary>
    /// Converts the square to a BoundingBox.
    /// </summary>
    public BoundingBox ToBoundingBox() => new(
        SouthWest.Latitude,
        SouthWest.Longitude,
        NorthEast.Latitude,
        NorthEast.Longitude);
}

/// <summary>
/// Represents the API response from What3Words convert-to-3wa endpoint.
/// </summary>
public record What3WordsApiResponse
{
    public string? Country { get; init; }
    public What3WordsSquareDto? Square { get; init; }
    public string? NearestPlace { get; init; }
    public What3WordsCoordinatesDto? Coordinates { get; init; }
    public string? Words { get; init; }
    public string? Language { get; init; }
    public string? Map { get; init; }
}

/// <summary>
/// DTO for coordinates in What3Words API response.
/// </summary>
public record What3WordsCoordinatesDto
{
    public double Lat { get; init; }
    public double Lng { get; init; }
}

/// <summary>
/// DTO for square bounds in What3Words API response.
/// </summary>
public record What3WordsSquareDto
{
    public What3WordsCoordinatesDto? Southwest { get; init; }
    public What3WordsCoordinatesDto? Northeast { get; init; }
}
