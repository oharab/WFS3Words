namespace WFS3Words.Core.Models;

/// <summary>
/// Represents a WFS feature combining a coordinate with its What3Words location data.
/// </summary>
/// <param name="Id">Unique feature identifier</param>
/// <param name="Coordinate">The geographic coordinate</param>
/// <param name="Location">The What3Words location data</param>
public record WfsFeature(
    string Id,
    GeoCoordinate Coordinate,
    What3WordsLocation Location);

/// <summary>
/// Represents a collection of WFS features.
/// </summary>
/// <param name="Features">The collection of features</param>
/// <param name="TotalCount">Total number of features in the collection</param>
/// <param name="BoundingBox">The bounding box containing all features</param>
public record WfsFeatureCollection(
    IReadOnlyList<WfsFeature> Features,
    int TotalCount,
    BoundingBox? BoundingBox = null);
