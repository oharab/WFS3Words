using WFS3Words.Core.Models;

namespace WFS3Words.Core.Interfaces;

/// <summary>
/// Service for transforming coordinates between different coordinate reference systems (CRS).
/// </summary>
public interface ICoordinateTransformationService
{
    /// <summary>
    /// Gets a list of supported EPSG codes.
    /// </summary>
    IReadOnlyList<string> SupportedEpsgCodes { get; }

    /// <summary>
    /// Transforms a coordinate from WGS84 (EPSG:4326) to the target CRS.
    /// </summary>
    /// <param name="coordinate">Coordinate in WGS84 (EPSG:4326)</param>
    /// <param name="targetEpsgCode">Target EPSG code (e.g., "EPSG:3857", "EPSG:27700")</param>
    /// <returns>Transformed coordinate in target CRS</returns>
    /// <exception cref="ArgumentException">Thrown when target CRS is not supported</exception>
    GeoCoordinate Transform(GeoCoordinate coordinate, string targetEpsgCode);

    /// <summary>
    /// Transforms a coordinate from the source CRS to WGS84 (EPSG:4326).
    /// </summary>
    /// <param name="coordinate">Coordinate in source CRS</param>
    /// <param name="sourceEpsgCode">Source EPSG code (e.g., "EPSG:3857", "EPSG:27700")</param>
    /// <returns>Transformed coordinate in WGS84</returns>
    /// <exception cref="ArgumentException">Thrown when source CRS is not supported</exception>
    GeoCoordinate TransformToWgs84(GeoCoordinate coordinate, string sourceEpsgCode);

    /// <summary>
    /// Transforms a bounding box from the source CRS to WGS84 (EPSG:4326).
    /// </summary>
    /// <param name="bbox">Bounding box in source CRS</param>
    /// <param name="sourceEpsgCode">Source EPSG code (e.g., "EPSG:3857", "EPSG:27700")</param>
    /// <returns>Transformed bounding box in WGS84</returns>
    /// <exception cref="ArgumentException">Thrown when source CRS is not supported</exception>
    BoundingBox TransformBBoxToWgs84(BoundingBox bbox, string sourceEpsgCode);

    /// <summary>
    /// Checks if a CRS is supported.
    /// </summary>
    /// <param name="epsgCode">EPSG code to check (e.g., "EPSG:4326")</param>
    /// <returns>True if supported, false otherwise</returns>
    bool IsSupported(string epsgCode);

    /// <summary>
    /// Normalizes an EPSG code to standard format (e.g., "4326" -> "EPSG:4326").
    /// </summary>
    /// <param name="epsgCode">EPSG code in any format</param>
    /// <returns>Normalized EPSG code</returns>
    string NormalizeEpsgCode(string epsgCode);
}
