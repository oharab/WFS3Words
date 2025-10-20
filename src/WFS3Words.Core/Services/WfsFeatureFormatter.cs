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
    /// <inheritdoc />
    public string FormatAsGml(WfsFeatureCollection collection, string version = "2.0.0")
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
        writer.WriteStartElement("wfs", "FeatureCollection", "http://www.opengis.net/wfs");
        writer.WriteAttributeString("xmlns", "gml", null, "http://www.opengis.net/gml");
        writer.WriteAttributeString("xmlns", "w3w", null, "http://what3words.com");
        writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");

        writer.WriteAttributeString("numberOfFeatures", collection.TotalCount.ToString());

        // Bounding box (if provided)
        if (collection.BoundingBox != null)
        {
            WriteBoundingBox(writer, collection.BoundingBox);
        }

        // Features
        foreach (var feature in collection.Features)
        {
            WriteFeature(writer, feature);
        }

        writer.WriteEndElement(); // FeatureCollection
        writer.WriteEndDocument();
        writer.Flush();

        return stringWriter.ToString();
    }

    /// <inheritdoc />
    public string FormatAsGeoJson(WfsFeatureCollection collection)
    {
        var geoJson = new
        {
            type = "FeatureCollection",
            features = collection.Features.Select(f => new
            {
                type = "Feature",
                id = f.Id,
                geometry = new
                {
                    type = "Point",
                    coordinates = new[] { f.Coordinate.Longitude, f.Coordinate.Latitude }
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
            }).ToArray(),
            numberMatched = collection.TotalCount,
            numberReturned = collection.Features.Count
        };

        return JsonSerializer.Serialize(geoJson, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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

    private void WriteBoundingBox(XmlWriter writer, BoundingBox bbox)
    {
        writer.WriteStartElement("gml", "boundedBy", "http://www.opengis.net/gml");
        writer.WriteStartElement("gml", "Envelope", "http://www.opengis.net/gml");
        writer.WriteAttributeString("srsName", "EPSG:4326");

        writer.WriteElementString("gml", "lowerCorner", "http://www.opengis.net/gml",
            $"{bbox.MinLongitude} {bbox.MinLatitude}");
        writer.WriteElementString("gml", "upperCorner", "http://www.opengis.net/gml",
            $"{bbox.MaxLongitude} {bbox.MaxLatitude}");

        writer.WriteEndElement(); // Envelope
        writer.WriteEndElement(); // boundedBy
    }

    private void WriteFeature(XmlWriter writer, WfsFeature feature)
    {
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

        // Geometry
        writer.WriteStartElement("w3w", "geometry", "http://what3words.com");
        writer.WriteStartElement("gml", "Point", "http://www.opengis.net/gml");
        writer.WriteAttributeString("srsName", "EPSG:4326");

        writer.WriteElementString("gml", "pos", "http://www.opengis.net/gml",
            $"{feature.Coordinate.Latitude} {feature.Coordinate.Longitude}");

        writer.WriteEndElement(); // Point
        writer.WriteEndElement(); // geometry

        writer.WriteEndElement(); // location
        writer.WriteEndElement(); // featureMember
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
