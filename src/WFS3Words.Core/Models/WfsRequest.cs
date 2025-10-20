namespace WFS3Words.Core.Models;

/// <summary>
/// Represents a parsed WFS request.
/// </summary>
public record WfsRequest
{
    /// <summary>
    /// WFS service name (should be "WFS")
    /// </summary>
    public string? Service { get; init; }

    /// <summary>
    /// WFS version (1.0.0, 1.1.0, 2.0.0, etc.)
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// WFS request type (GetCapabilities, DescribeFeatureType, GetFeature)
    /// </summary>
    public string? Request { get; init; }

    /// <summary>
    /// Feature type name for DescribeFeatureType and GetFeature requests
    /// </summary>
    public string? TypeName { get; init; }

    /// <summary>
    /// Bounding box for GetFeature spatial queries
    /// </summary>
    public BoundingBox? BBox { get; init; }

    /// <summary>
    /// Maximum number of features to return
    /// </summary>
    public int? MaxFeatures { get; init; }

    /// <summary>
    /// Output format (GML, GeoJSON, etc.)
    /// </summary>
    public string? OutputFormat { get; init; }
}
