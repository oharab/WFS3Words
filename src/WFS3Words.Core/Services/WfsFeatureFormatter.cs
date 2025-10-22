using System.Text;
using System.Text.Json;
using System.Xml;
using WFS3Words.Core.Interfaces;
using WFS3Words.Core.Models;

namespace WFS3Words.Core.Services;

/// <summary>
/// Formatter for WFS feature responses.
/// </summary>
public class WfsFeatureFormatter : IWfsFeatureFormatter
{
    private readonly ICoordinateTransformationService _transformationService;

    public WfsFeatureFormatter(ICoordinateTransformationService transformationService)
    {
        _transformationService = transformationService;
    }

    /// <inheritdoc />
    public string FormatAsGml(WfsFeatureCollection collection, string version = "2.0.0", string? srsName = null)
    {
        var targetSrs = _transformationService.NormalizeEpsgCode(srsName!);

        // Determine GML version based on WFS version
        // WFS 1.0.0 requires GML 2.1.2
        // WFS 1.1.0 and 2.0.0 use GML 3.x
        var useGml2 = version == "1.0.0";

        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new StringWriter();
        using var writer = XmlWriter.Create(stringWriter, settings);

        writer.WriteStartDocument();

        // WFS 1.0.0 and 2.0.0 use different namespaces and attributes
        var wfsNamespace = version == "1.0.0" ? "http://www.opengis.net/wfs" : "http://www.opengis.net/wfs/2.0";

        writer.WriteStartElement("wfs", "FeatureCollection", wfsNamespace);
        writer.WriteAttributeString("xmlns", "gml", null, "http://www.opengis.net/gml");
        writer.WriteAttributeString("xmlns", "w3w", null, "http://what3words.com");
        writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");

        // WFS 1.0.0 doesn't have feature count attributes
        // WFS 2.0.0 uses numberMatched and numberReturned
        if (version != "1.0.0")
        {
            writer.WriteAttributeString("numberMatched", collection.TotalCount.ToString());
            writer.WriteAttributeString("numberReturned", collection.TotalCount.ToString());
            writer.WriteAttributeString("timeStamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }

        // Bounding box (if provided)
        if (collection.BoundingBox != null)
        {
            if (useGml2)
            {
                WriteBoundingBoxGml2(writer, collection.BoundingBox, targetSrs);
            }
            else
            {
                // WFS 2.0.0 uses wfs:boundedBy element, not gml:boundedBy
                WriteBoundingBoxWfs20(writer, collection.BoundingBox, targetSrs);
            }
        }

        // Features
        foreach (var feature in collection.Features)
        {
            if (useGml2)
            {
                WriteFeatureGml2(writer, feature, targetSrs);
            }
            else
            {
                // WFS 2.0.0 uses wfs:member, not gml:featureMember
                WriteFeatureWfs20(writer, feature, targetSrs);
            }
        }

        writer.WriteEndElement(); // FeatureCollection
        writer.WriteEndDocument();
        writer.Flush();

        return stringWriter.ToString();
    }

    /// <inheritdoc />
    public string FormatAsGeoJson(WfsFeatureCollection collection, string? srsName = null)
    {
        var targetSrs = _transformationService.NormalizeEpsgCode(srsName!);

        var geoJson = new
        {
            type = "FeatureCollection",
            features = collection.Features.Select(f =>
            {
                var coord = _transformationService.Transform(f.Coordinate, targetSrs);
                return new
                {
                    type = "Feature",
                    id = f.Id,
                    geometry = new
                    {
                        type = "Point",
                        coordinates = new[] { coord.Longitude, coord.Latitude }
                    },
                    properties = new
                    {
                        words = f.Location.Words,
                        country = f.Location.Country,
                        nearestPlace = f.Location.NearestPlace,
                        language = f.Location.Language,
                        map = f.Location.Map,
                        square = new
                        {
                            southwest = new
                            {
                                lat = f.Location.Square.MinLatitude,
                                lng = f.Location.Square.MinLongitude
                            },
                            northeast = new
                            {
                                lat = f.Location.Square.MaxLatitude,
                                lng = f.Location.Square.MaxLongitude
                            }
                        }
                    }
                };
            }).ToArray(),
            numberMatched = collection.TotalCount,
            numberReturned = collection.Features.Count,
            crs = targetSrs != "EPSG:4326" ? new { type = "name", properties = new { name = targetSrs } } : null
        };

        return JsonSerializer.Serialize(geoJson, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    /// <inheritdoc />
    public string GenerateFeatureTypeDescription(string version = "2.0.0")
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new StringWriter();
        using var writer = XmlWriter.Create(stringWriter, settings);

        writer.WriteStartDocument();
        writer.WriteStartElement("xsd", "schema", "http://www.w3.org/2001/XMLSchema");
        writer.WriteAttributeString("xmlns", "w3w", null, "http://what3words.com");
        writer.WriteAttributeString("xmlns", "gml", null, "http://www.opengis.net/gml");
        writer.WriteAttributeString("targetNamespace", "http://what3words.com");
        writer.WriteAttributeString("elementFormDefault", "qualified");

        // Import GML schema
        writer.WriteStartElement("xsd", "import", "http://www.w3.org/2001/XMLSchema");
        writer.WriteAttributeString("namespace", "http://www.opengis.net/gml");
        writer.WriteEndElement();

        // Define location feature type
        writer.WriteStartElement("xsd", "complexType", "http://www.w3.org/2001/XMLSchema");
        writer.WriteAttributeString("name", "locationType");

        writer.WriteStartElement("xsd", "complexContent", "http://www.w3.org/2001/XMLSchema");
        writer.WriteStartElement("xsd", "extension", "http://www.w3.org/2001/XMLSchema");
        writer.WriteAttributeString("base", "gml:AbstractFeatureType");

        writer.WriteStartElement("xsd", "sequence", "http://www.w3.org/2001/XMLSchema");

        // Define properties
        WriteElement(writer, "words", "xsd:string");
        WriteElement(writer, "country", "xsd:string");
        WriteElement(writer, "nearestPlace", "xsd:string");
        WriteElement(writer, "language", "xsd:string");
        WriteElement(writer, "geometry", "gml:PointPropertyType");

        writer.WriteEndElement(); // sequence
        writer.WriteEndElement(); // extension
        writer.WriteEndElement(); // complexContent
        writer.WriteEndElement(); // complexType

        // Element declaration
        writer.WriteStartElement("xsd", "element", "http://www.w3.org/2001/XMLSchema");
        writer.WriteAttributeString("name", "location");
        writer.WriteAttributeString("type", "w3w:locationType");
        writer.WriteAttributeString("substitutionGroup", "gml:_Feature");
        writer.WriteEndElement();

        writer.WriteEndElement(); // schema
        writer.WriteEndDocument();
        writer.Flush();

        return stringWriter.ToString();
    }

    private void WriteBoundingBoxGml2(XmlWriter writer, BoundingBox bbox, string srsName)
    {
        var minCoord = _transformationService.Transform(new GeoCoordinate(bbox.MinLatitude, bbox.MinLongitude), srsName);
        var maxCoord = _transformationService.Transform(new GeoCoordinate(bbox.MaxLatitude, bbox.MaxLongitude), srsName);

        writer.WriteStartElement("gml", "boundedBy", "http://www.opengis.net/gml");
        writer.WriteStartElement("gml", "Box", "http://www.opengis.net/gml");
        writer.WriteAttributeString("srsName", srsName);

        // GML 2 uses <gml:coordinates> with comma-separated tuples
        writer.WriteElementString("gml", "coordinates", "http://www.opengis.net/gml",
            $"{minCoord.Longitude},{minCoord.Latitude} {maxCoord.Longitude},{maxCoord.Latitude}");

        writer.WriteEndElement(); // Box
        writer.WriteEndElement(); // boundedBy
    }

    private void WriteBoundingBoxGml3(XmlWriter writer, BoundingBox bbox, string srsName)
    {
        var minCoord = _transformationService.Transform(new GeoCoordinate(bbox.MinLatitude, bbox.MinLongitude), srsName);
        var maxCoord = _transformationService.Transform(new GeoCoordinate(bbox.MaxLatitude, bbox.MaxLongitude), srsName);

        writer.WriteStartElement("gml", "boundedBy", "http://www.opengis.net/gml");
        writer.WriteStartElement("gml", "Envelope", "http://www.opengis.net/gml");
        writer.WriteAttributeString("srsName", srsName);

        writer.WriteElementString("gml", "lowerCorner", "http://www.opengis.net/gml",
            $"{minCoord.Longitude} {minCoord.Latitude}");
        writer.WriteElementString("gml", "upperCorner", "http://www.opengis.net/gml",
            $"{maxCoord.Longitude} {maxCoord.Latitude}");

        writer.WriteEndElement(); // Envelope
        writer.WriteEndElement(); // boundedBy
    }

    private void WriteBoundingBoxWfs20(XmlWriter writer, BoundingBox bbox, string srsName)
    {
        var minCoord = _transformationService.Transform(new GeoCoordinate(bbox.MinLatitude, bbox.MinLongitude), srsName);
        var maxCoord = _transformationService.Transform(new GeoCoordinate(bbox.MaxLatitude, bbox.MaxLongitude), srsName);

        // WFS 2.0 uses wfs:boundedBy element with gml:Envelope child
        writer.WriteStartElement("wfs", "boundedBy", "http://www.opengis.net/wfs/2.0");
        writer.WriteStartElement("gml", "Envelope", "http://www.opengis.net/gml");
        writer.WriteAttributeString("srsName", srsName);

        writer.WriteElementString("gml", "lowerCorner", "http://www.opengis.net/gml",
            $"{minCoord.Longitude} {minCoord.Latitude}");
        writer.WriteElementString("gml", "upperCorner", "http://www.opengis.net/gml",
            $"{maxCoord.Longitude} {maxCoord.Latitude}");

        writer.WriteEndElement(); // Envelope
        writer.WriteEndElement(); // wfs:boundedBy
    }

    private void WriteFeatureGml2(XmlWriter writer, WfsFeature feature, string srsName)
    {
        var coord = _transformationService.Transform(feature.Coordinate, srsName);

        writer.WriteStartElement("gml", "featureMember", "http://www.opengis.net/gml");
        writer.WriteStartElement("w3w", "location", "http://what3words.com");
        writer.WriteAttributeString("gml", "id", "http://www.opengis.net/gml", feature.Id);

        // Properties
        writer.WriteElementString("w3w", "words", "http://what3words.com", feature.Location.Words);
        writer.WriteElementString("w3w", "country", "http://what3words.com", feature.Location.Country);

        if (!string.IsNullOrEmpty(feature.Location.NearestPlace))
        {
            writer.WriteElementString("w3w", "nearestPlace", "http://what3words.com", feature.Location.NearestPlace);
        }

        writer.WriteElementString("w3w", "language", "http://what3words.com", feature.Location.Language);

        // Geometry - GML 2 format
        writer.WriteStartElement("w3w", "geometry", "http://what3words.com");
        writer.WriteStartElement("gml", "Point", "http://www.opengis.net/gml");
        writer.WriteAttributeString("srsName", srsName);

        // GML 2 uses <gml:coordinates> with comma-separated lon,lat
        writer.WriteElementString("gml", "coordinates", "http://www.opengis.net/gml",
            $"{coord.Longitude},{coord.Latitude}");

        writer.WriteEndElement(); // Point
        writer.WriteEndElement(); // geometry

        writer.WriteEndElement(); // location
        writer.WriteEndElement(); // featureMember
    }

    private void WriteFeatureGml3(XmlWriter writer, WfsFeature feature, string srsName)
    {
        var coord = _transformationService.Transform(feature.Coordinate, srsName);

        writer.WriteStartElement("gml", "featureMember", "http://www.opengis.net/gml");
        writer.WriteStartElement("w3w", "location", "http://what3words.com");
        writer.WriteAttributeString("gml", "id", "http://www.opengis.net/gml", feature.Id);

        // Properties
        writer.WriteElementString("w3w", "words", "http://what3words.com", feature.Location.Words);
        writer.WriteElementString("w3w", "country", "http://what3words.com", feature.Location.Country);

        if (!string.IsNullOrEmpty(feature.Location.NearestPlace))
        {
            writer.WriteElementString("w3w", "nearestPlace", "http://what3words.com", feature.Location.NearestPlace);
        }

        writer.WriteElementString("w3w", "language", "http://what3words.com", feature.Location.Language);

        // Geometry - GML 3 format
        writer.WriteStartElement("w3w", "geometry", "http://what3words.com");
        writer.WriteStartElement("gml", "Point", "http://www.opengis.net/gml");
        writer.WriteAttributeString("srsName", srsName);

        // GML 3 uses <gml:pos> with space-separated lat lon
        writer.WriteElementString("gml", "pos", "http://www.opengis.net/gml",
            $"{coord.Latitude} {coord.Longitude}");

        writer.WriteEndElement(); // Point
        writer.WriteEndElement(); // geometry

        writer.WriteEndElement(); // location
        writer.WriteEndElement(); // featureMember
    }

    private void WriteFeatureWfs20(XmlWriter writer, WfsFeature feature, string srsName)
    {
        var coord = _transformationService.Transform(feature.Coordinate, srsName);

        // WFS 2.0 uses wfs:member instead of gml:featureMember
        writer.WriteStartElement("wfs", "member", "http://www.opengis.net/wfs/2.0");
        writer.WriteStartElement("w3w", "location", "http://what3words.com");
        writer.WriteAttributeString("gml", "id", "http://www.opengis.net/gml", feature.Id);

        // Properties
        writer.WriteElementString("w3w", "words", "http://what3words.com", feature.Location.Words);
        writer.WriteElementString("w3w", "country", "http://what3words.com", feature.Location.Country);

        if (!string.IsNullOrEmpty(feature.Location.NearestPlace))
        {
            writer.WriteElementString("w3w", "nearestPlace", "http://what3words.com", feature.Location.NearestPlace);
        }

        writer.WriteElementString("w3w", "language", "http://what3words.com", feature.Location.Language);

        // Geometry - GML 3 format
        writer.WriteStartElement("w3w", "geometry", "http://what3words.com");
        writer.WriteStartElement("gml", "Point", "http://www.opengis.net/gml");
        writer.WriteAttributeString("srsName", srsName);

        // GML 3 uses <gml:pos> with space-separated lat lon
        writer.WriteElementString("gml", "pos", "http://www.opengis.net/gml",
            $"{coord.Latitude} {coord.Longitude}");

        writer.WriteEndElement(); // Point
        writer.WriteEndElement(); // geometry

        writer.WriteEndElement(); // location
        writer.WriteEndElement(); // wfs:member
    }

    private void WriteElement(XmlWriter writer, string name, string type)
    {
        writer.WriteStartElement("xsd", "element", "http://www.w3.org/2001/XMLSchema");
        writer.WriteAttributeString("name", name);
        writer.WriteAttributeString("type", type);
        writer.WriteAttributeString("minOccurs", "0");
        writer.WriteEndElement();
    }
}
