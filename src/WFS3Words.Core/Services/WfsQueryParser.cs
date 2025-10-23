using System.Xml.Linq;
using Microsoft.Extensions.Primitives;
using WFS3Words.Core.Models;

namespace WFS3Words.Core.Services;

/// <summary>
/// Parser for WFS query string parameters.
/// </summary>
public class WfsQueryParser
{
    /// <summary>
    /// Parses WFS query parameters from a dictionary.
    /// </summary>
    /// <param name="queryParams">Query parameter dictionary (case-insensitive)</param>
    /// <returns>Parsed WFS request</returns>
    public WfsRequest Parse(IDictionary<string, StringValues> queryParams)
    {
        var caseInsensitiveParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in queryParams)
        {
            caseInsensitiveParams[kvp.Key] = kvp.Value.ToString();
        }

        // Try to get BBOX from direct parameter first
        var bbox = ParseBoundingBox(GetValue(caseInsensitiveParams, "bbox"));
        string? srsName = GetValue(caseInsensitiveParams, "srsname") ?? GetValue(caseInsensitiveParams, "srs");

        // If no direct BBOX, try to extract from Filter XML
        if (bbox == null)
        {
            var filterXml = GetValue(caseInsensitiveParams, "filter");
            if (filterXml != null)
            {
                (bbox, var filterSrs) = ExtractBBoxAndSrsFromFilter(filterXml);
                // Use Filter SRS if no explicit srsName/srs parameter was provided
                srsName ??= filterSrs;
            }
        }

        return new WfsRequest
        {
            Service = GetValue(caseInsensitiveParams, "service"),
            Version = GetValue(caseInsensitiveParams, "version"),
            Request = GetValue(caseInsensitiveParams, "request"),
            TypeName = GetValue(caseInsensitiveParams, "typename") ??
                      GetValue(caseInsensitiveParams, "typenames"),
            BBox = bbox,
            MaxFeatures = ParseInt(GetValue(caseInsensitiveParams, "maxfeatures") ??
                                  GetValue(caseInsensitiveParams, "count")),
            OutputFormat = GetValue(caseInsensitiveParams, "outputformat"),
            SrsName = srsName
        };
    }

    private string? GetValue(Dictionary<string, string> parameters, string key)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private BoundingBox? ParseBoundingBox(string? bboxString)
    {
        if (string.IsNullOrWhiteSpace(bboxString))
            return null;

        return BoundingBox.Parse(bboxString);
    }

    private int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// Extracts BBOX and SRS from OGC Filter XML.
    /// Supports Filter elements containing BBOX with gml:Box or gml:Envelope.
    /// Returns tuple of (BoundingBox, SrsName).
    /// </summary>
    private (BoundingBox?, string?) ExtractBBoxAndSrsFromFilter(string filterXml)
    {
        try
        {
            var doc = XDocument.Parse(filterXml);
            var gmlNamespace = XNamespace.Get("http://www.opengis.net/gml");
            var ogcNamespace = XNamespace.Get("http://www.opengis.net/ogc");

            // Look for BBOX element
            var bboxElement = doc.Descendants(ogcNamespace + "BBOX").FirstOrDefault();
            if (bboxElement == null)
                return (null, null);

            // Try to find gml:Box or gml:Envelope
            var boxElement = bboxElement.Descendants(gmlNamespace + "Box").FirstOrDefault() ??
                           bboxElement.Descendants(gmlNamespace + "Envelope").FirstOrDefault();

            if (boxElement == null)
                return (null, null);

            // Extract SRS from srsName attribute
            var srsName = boxElement.Attribute("srsName")?.Value;

            // Try gml:coordinates format (GML 2.x)
            var coordinatesElement = boxElement.Element(gmlNamespace + "coordinates");
            if (coordinatesElement != null)
            {
                return (ParseGmlCoordinates(coordinatesElement.Value), srsName);
            }

            // Try gml:lowerCorner and gml:upperCorner format (GML 3.x)
            var lowerCorner = boxElement.Element(gmlNamespace + "lowerCorner");
            var upperCorner = boxElement.Element(gmlNamespace + "upperCorner");

            if (lowerCorner != null && upperCorner != null)
            {
                return (ParseGmlCorners(lowerCorner.Value, upperCorner.Value), srsName);
            }

            return (null, srsName);
        }
        catch
        {
            // If Filter XML parsing fails, return null
            return (null, null);
        }
    }

    /// <summary>
    /// Parses GML 2.x coordinates format: "x1,y1 x2,y2"
    /// </summary>
    private BoundingBox? ParseGmlCoordinates(string coordinates)
    {
        try
        {
            // GML coordinates can be separated by space or comma
            var parts = coordinates.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return null;

            var corner1 = parts[0].Split(',');
            var corner2 = parts[1].Split(',');

            if (corner1.Length != 2 || corner2.Length != 2)
                return null;

            if (!double.TryParse(corner1[0], out var x1) ||
                !double.TryParse(corner1[1], out var y1) ||
                !double.TryParse(corner2[0], out var x2) ||
                !double.TryParse(corner2[1], out var y2))
            {
                return null;
            }

            // Coordinates might be in any CRS, but we store as minLon, minLat, maxLon, maxLat
            // Ensure min/max order
            var minX = Math.Min(x1, x2);
            var maxX = Math.Max(x1, x2);
            var minY = Math.Min(y1, y2);
            var maxY = Math.Max(y1, y2);

            // Create BoundingBox (note: constructor is minLat, minLon, maxLat, maxLon)
            // We treat X as longitude and Y as latitude
            return new BoundingBox(minY, minX, maxY, maxX);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses GML 3.x corner format: "x1 y1" and "x2 y2"
    /// </summary>
    private BoundingBox? ParseGmlCorners(string lowerCorner, string upperCorner)
    {
        try
        {
            var lower = lowerCorner.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var upper = upperCorner.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (lower.Length < 2 || upper.Length < 2)
                return null;

            if (!double.TryParse(lower[0], out var minX) ||
                !double.TryParse(lower[1], out var minY) ||
                !double.TryParse(upper[0], out var maxX) ||
                !double.TryParse(upper[1], out var maxY))
            {
                return null;
            }

            // Create BoundingBox (note: constructor is minLat, minLon, maxLat, maxLon)
            return new BoundingBox(minY, minX, maxY, maxX);
        }
        catch
        {
            return null;
        }
    }
}
