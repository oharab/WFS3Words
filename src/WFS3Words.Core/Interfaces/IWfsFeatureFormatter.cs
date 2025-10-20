using WFS3Words.Core.Models;

namespace WFS3Words.Core.Interfaces;

/// <summary>
/// Formatter for WFS feature responses (GetFeature, GetFeatureType).
/// </summary>
public interface IWfsFeatureFormatter
{
    /// <summary>
    /// Generates a GML (Geography Markup Language) response for a feature collection.
    /// </summary>
    /// <param name="collection">The feature collection to format</param>
    /// <param name="version">WFS version</param>
    /// <param name="srsName">Target coordinate reference system (e.g., EPSG:3857)</param>
    /// <returns>GML XML document as string</returns>
    string FormatAsGml(WfsFeatureCollection collection, string version = "2.0.0", string? srsName = null);

    /// <summary>
    /// Generates a GeoJSON response for a feature collection (WFS 3.0).
    /// </summary>
    /// <param name="collection">The feature collection to format</param>
    /// <param name="srsName">Target coordinate reference system (e.g., EPSG:3857)</param>
    /// <returns>GeoJSON document as string</returns>
    string FormatAsGeoJson(WfsFeatureCollection collection, string? srsName = null);

    /// <summary>
    /// Generates a DescribeFeatureType XML response.
    /// </summary>
    /// <param name="version">WFS version</param>
    /// <returns>DescribeFeatureType XML document as string</returns>
    string GenerateFeatureTypeDescription(string version = "2.0.0");
}
