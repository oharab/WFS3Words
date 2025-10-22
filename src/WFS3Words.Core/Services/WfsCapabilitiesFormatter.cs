using System.Text;
using System.Xml;
using Microsoft.Extensions.Options;
using WFS3Words.Core.Configuration;
using WFS3Words.Core.Interfaces;

namespace WFS3Words.Core.Services;

/// <summary>
/// Formatter for WFS GetCapabilities responses.
/// </summary>
public class WfsCapabilitiesFormatter : IWfsCapabilitiesFormatter
{
    private readonly WfsOptions _options;
    private readonly ICoordinateTransformationService _transformationService;

    public WfsCapabilitiesFormatter(IOptions<WfsOptions> options, ICoordinateTransformationService transformationService)
    {
        _options = options.Value;
        _transformationService = transformationService;
    }

    /// <inheritdoc />
    public string GenerateCapabilities(string version, string serviceUrl)
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

        // Root element namespace varies by version
        // WFS 1.0.0 and 1.1.0 use http://www.opengis.net/wfs
        // WFS 2.0.0 uses http://www.opengis.net/wfs/2.0
        var wfsNamespace = version.StartsWith("2.")
            ? "http://www.opengis.net/wfs/2.0"
            : "http://www.opengis.net/wfs";

        writer.WriteStartElement("WFS_Capabilities", wfsNamespace);
        writer.WriteAttributeString("version", version);
        writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
        writer.WriteAttributeString("xmlns", "gml", null, "http://www.opengis.net/gml");
        writer.WriteAttributeString("xmlns", "ogc", null, "http://www.opengis.net/ogc");

        // WFS 2.0.0 requires OWS and xlink namespaces, plus w3w for feature type names
        if (version.StartsWith("2."))
        {
            writer.WriteAttributeString("xmlns", "ows", null, "http://www.opengis.net/ows/1.1");
            writer.WriteAttributeString("xmlns", "xlink", null, "http://www.w3.org/1999/xlink");
            writer.WriteAttributeString("xmlns", "w3w", null, "http://what3words.com/ns");
        }

        // Service Identification
        WriteServiceIdentification(writer, version);

        // Service Provider
        WriteServiceProvider(writer, version);

        // Operations Metadata
        WriteOperationsMetadata(writer, serviceUrl, version);

        // Feature Type List
        WriteFeatureTypeList(writer, version);

        // Filter Capabilities (required by WFS 1.0.0 schema)
        if (version == "1.0.0")
        {
            WriteFilterCapabilities(writer);
        }

        writer.WriteEndElement(); // WFS_Capabilities
        writer.WriteEndDocument();
        writer.Flush();

        return stringWriter.ToString();
    }

    private void WriteServiceIdentification(XmlWriter writer, string version)
    {
        if (version.StartsWith("2."))
        {
            // WFS 2.0.0: Use ows:ServiceIdentification
            writer.WriteStartElement("ows", "ServiceIdentification", "http://www.opengis.net/ows/1.1");

            writer.WriteElementString("ows", "Title", "http://www.opengis.net/ows/1.1", _options.ServiceTitle);
            writer.WriteElementString("ows", "Abstract", "http://www.opengis.net/ows/1.1", _options.ServiceAbstract);

            // Keywords
            writer.WriteStartElement("ows", "Keywords", "http://www.opengis.net/ows/1.1");
            foreach (var keyword in _options.Keywords.Split(','))
            {
                writer.WriteElementString("ows", "Keyword", "http://www.opengis.net/ows/1.1", keyword.Trim());
            }
            writer.WriteEndElement(); // ows:Keywords

            writer.WriteElementString("ows", "ServiceType", "http://www.opengis.net/ows/1.1", "WFS");
            writer.WriteElementString("ows", "ServiceTypeVersion", "http://www.opengis.net/ows/1.1", version);
            writer.WriteElementString("ows", "Fees", "http://www.opengis.net/ows/1.1", _options.Fees);
            writer.WriteElementString("ows", "AccessConstraints", "http://www.opengis.net/ows/1.1", _options.AccessConstraints);

            writer.WriteEndElement(); // ows:ServiceIdentification
        }
        else
        {
            // WFS 1.0.0: Use Service element
            writer.WriteStartElement("Service");

            writer.WriteElementString("Name", "WFS");
            writer.WriteElementString("Title", _options.ServiceTitle);
            writer.WriteElementString("Abstract", _options.ServiceAbstract);

            // Keywords - WFS 1.0.0 expects comma-separated text, not child elements
            writer.WriteElementString("Keywords", _options.Keywords);

            // OnlineResource - required by WFS 1.0.0 schema
            writer.WriteStartElement("OnlineResource");
            writer.WriteString("http://www.opengis.net/wfs");
            writer.WriteEndElement(); // OnlineResource

            writer.WriteElementString("Fees", _options.Fees);
            writer.WriteElementString("AccessConstraints", _options.AccessConstraints);

            writer.WriteEndElement(); // Service
        }
    }

    private void WriteServiceProvider(XmlWriter writer, string version)
    {
        // ServiceProvider is only supported in WFS 2.0.0 (not in WFS 1.0.0)
        if (!version.StartsWith("2."))
        {
            return; // WFS 1.0.0 does not have ServiceProvider element
        }

        // ServiceProvider requires at least ProviderSite OR ServiceContact to be valid
        // (ProviderName alone is not sufficient according to OWS schema)
        var hasProviderSite = !string.IsNullOrEmpty(_options.ProviderSite);
        var hasServiceContact = !string.IsNullOrEmpty(_options.ContactPerson) || !string.IsNullOrEmpty(_options.ContactEmail);

        if (!hasProviderSite && !hasServiceContact)
        {
            return; // Skip if no valid provider structure can be created
        }

        // WFS 2.0.0: Use ows:ServiceProvider
        writer.WriteStartElement("ows", "ServiceProvider", "http://www.opengis.net/ows/1.1");

        if (!string.IsNullOrEmpty(_options.ProviderName))
        {
            writer.WriteElementString("ows", "ProviderName", "http://www.opengis.net/ows/1.1", _options.ProviderName);
        }

        if (!string.IsNullOrEmpty(_options.ProviderSite))
        {
            writer.WriteStartElement("ows", "ProviderSite", "http://www.opengis.net/ows/1.1");
            writer.WriteAttributeString("xlink", "href", "http://www.w3.org/1999/xlink", _options.ProviderSite);
            writer.WriteEndElement();
        }

        if (!string.IsNullOrEmpty(_options.ContactPerson) ||
            !string.IsNullOrEmpty(_options.ContactEmail))
        {
            writer.WriteStartElement("ows", "ServiceContact", "http://www.opengis.net/ows/1.1");

            if (!string.IsNullOrEmpty(_options.ContactPerson))
            {
                writer.WriteElementString("ows", "IndividualName", "http://www.opengis.net/ows/1.1", _options.ContactPerson);
            }

            if (!string.IsNullOrEmpty(_options.ContactEmail))
            {
                writer.WriteStartElement("ows", "ContactInfo", "http://www.opengis.net/ows/1.1");
                writer.WriteStartElement("ows", "Address", "http://www.opengis.net/ows/1.1");
                writer.WriteElementString("ows", "ElectronicMailAddress", "http://www.opengis.net/ows/1.1", _options.ContactEmail);
                writer.WriteEndElement(); // ows:Address
                writer.WriteEndElement(); // ows:ContactInfo
            }

            writer.WriteEndElement(); // ows:ServiceContact
        }

        writer.WriteEndElement(); // ows:ServiceProvider
    }

    private void WriteOperationsMetadata(XmlWriter writer, string serviceUrl, string version)
    {
        if (version.StartsWith("2."))
        {
            // WFS 2.0.0: Use ows:OperationsMetadata
            writer.WriteStartElement("ows", "OperationsMetadata", "http://www.opengis.net/ows/1.1");

            // GetCapabilities
            WriteOwsOperation(writer, "GetCapabilities", serviceUrl);

            // DescribeFeatureType
            WriteOwsOperation(writer, "DescribeFeatureType", serviceUrl);

            // GetFeature
            WriteOwsOperation(writer, "GetFeature", serviceUrl);

            writer.WriteEndElement(); // ows:OperationsMetadata
        }
        else
        {
            // WFS 1.0.0: Use Capability/Request structure
            writer.WriteStartElement("Capability");
            writer.WriteStartElement("Request");

            // GetCapabilities
            WriteOperation(writer, "GetCapabilities", serviceUrl, null, null);

            // DescribeFeatureType - include supported output formats
            var describeFormats = new[] { "XMLSCHEMA" };
            WriteOperation(writer, "DescribeFeatureType", serviceUrl, describeFormats, "SchemaDescriptionLanguage");

            // GetFeature - include supported result formats based on version
            var getFeatureFormats = new[] { "GML2" };
            WriteOperation(writer, "GetFeature", serviceUrl, getFeatureFormats, "ResultFormat");

            writer.WriteEndElement(); // Request
            writer.WriteEndElement(); // Capability
        }
    }

    private void WriteOperation(XmlWriter writer, string operationName, string serviceUrl, string[]? formats, string? formatContainerName)
    {
        writer.WriteStartElement(operationName);

        // Write supported formats if provided
        // DescribeFeatureType uses SchemaDescriptionLanguage
        // GetFeature uses ResultFormat
        if (formats != null && formats.Length > 0 && !string.IsNullOrEmpty(formatContainerName))
        {
            writer.WriteStartElement(formatContainerName);
            foreach (var format in formats)
            {
                // Write as empty element: <GML2/>, <GML3/>, <XMLSCHEMA/>, etc.
                writer.WriteStartElement(format);
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); // formatContainerName (SchemaDescriptionLanguage or ResultFormat)
        }

        writer.WriteStartElement("DCPType");
        writer.WriteStartElement("HTTP");

        writer.WriteStartElement("Get");
        writer.WriteAttributeString("onlineResource", serviceUrl);
        writer.WriteEndElement(); // Get

        writer.WriteStartElement("Post");
        writer.WriteAttributeString("onlineResource", serviceUrl);
        writer.WriteEndElement(); // Post

        writer.WriteEndElement(); // HTTP
        writer.WriteEndElement(); // DCPType
        writer.WriteEndElement(); // Operation
    }

    private void WriteOwsOperation(XmlWriter writer, string operationName, string serviceUrl)
    {
        writer.WriteStartElement("ows", "Operation", "http://www.opengis.net/ows/1.1");
        writer.WriteAttributeString("name", operationName);

        writer.WriteStartElement("ows", "DCP", "http://www.opengis.net/ows/1.1");
        writer.WriteStartElement("ows", "HTTP", "http://www.opengis.net/ows/1.1");

        writer.WriteStartElement("ows", "Get", "http://www.opengis.net/ows/1.1");
        writer.WriteAttributeString("xlink", "href", "http://www.w3.org/1999/xlink", serviceUrl);
        writer.WriteEndElement(); // ows:Get

        writer.WriteEndElement(); // ows:HTTP
        writer.WriteEndElement(); // ows:DCP

        writer.WriteEndElement(); // ows:Operation
    }

    private void WriteFilterCapabilities(XmlWriter writer)
    {
        // WFS 1.0.0 requires Filter_Capabilities element in ogc namespace
        writer.WriteStartElement("ogc", "Filter_Capabilities", "http://www.opengis.net/ogc");

        // Spatial Capabilities
        writer.WriteStartElement("ogc", "Spatial_Capabilities", "http://www.opengis.net/ogc");
        writer.WriteStartElement("ogc", "Spatial_Operators", "http://www.opengis.net/ogc");
        writer.WriteStartElement("ogc", "BBOX", "http://www.opengis.net/ogc");
        writer.WriteEndElement(); // BBOX
        writer.WriteEndElement(); // Spatial_Operators
        writer.WriteEndElement(); // Spatial_Capabilities

        // Scalar Capabilities
        writer.WriteStartElement("ogc", "Scalar_Capabilities", "http://www.opengis.net/ogc");
        writer.WriteStartElement("ogc", "Logical_Operators", "http://www.opengis.net/ogc");
        writer.WriteEndElement(); // Logical_Operators (empty)
        writer.WriteEndElement(); // Scalar_Capabilities

        writer.WriteEndElement(); // ogc:Filter_Capabilities
    }

    private void WriteFeatureTypeList(XmlWriter writer, string version)
    {
        writer.WriteStartElement("FeatureTypeList");

        // WFS 1.0.0 Operations expects individual operation elements like <Query/>, <Insert/>, etc.
        // WFS 2.0.0 doesn't have Operations in FeatureTypeList
        if (version == "1.0.0")
        {
            writer.WriteStartElement("Operations");
            writer.WriteStartElement("Query");
            writer.WriteEndElement(); // Query (empty element)
            writer.WriteEndElement(); // Operations
        }

        // Define the What3Words location feature type
        writer.WriteStartElement("FeatureType");

        // WFS 1.0.0 uses unprefixed name, WFS 2.0.0 can use prefixed name
        var featureTypeName = version == "1.0.0" ? "location" : "w3w:location";
        writer.WriteElementString("Name", featureTypeName);
        writer.WriteElementString("Title", "What3Words Location");
        writer.WriteElementString("Abstract", "Geographic location with What3Words 3-word address");

        // WFS 1.0.0 uses <SRS>, WFS 2.0.0 uses <DefaultCRS> and <OtherCRS>
        if (version == "1.0.0")
        {
            // WFS 1.0.0: Use single <SRS> element
            writer.WriteElementString("SRS", "EPSG:4326");
        }
        else
        {
            // WFS 2.0.0: Use <DefaultCRS> and <OtherCRS> (not DefaultSRS/OtherSRS)
            writer.WriteElementString("DefaultCRS", "EPSG:4326");

            // List all supported CRS
            foreach (var srs in _transformationService.SupportedEpsgCodes)
            {
                if (srs != "EPSG:4326") // Already listed as DefaultCRS
                {
                    writer.WriteElementString("OtherCRS", srs);
                }
            }
        }

        // WFS 1.0.0 uses <LatLongBoundingBox>, WFS 2.0.0 uses <ows:WGS84BoundingBox>
        if (version == "1.0.0")
        {
            // WFS 1.0.0: Use <LatLongBoundingBox> with attributes
            writer.WriteStartElement("LatLongBoundingBox");
            writer.WriteAttributeString("minx", "-180");
            writer.WriteAttributeString("miny", "-90");
            writer.WriteAttributeString("maxx", "180");
            writer.WriteAttributeString("maxy", "90");
            writer.WriteEndElement(); // LatLongBoundingBox
        }
        else
        {
            // WFS 2.0.0: Use <ows:WGS84BoundingBox> in ows namespace with child elements
            writer.WriteStartElement("ows", "WGS84BoundingBox", "http://www.opengis.net/ows/1.1");
            writer.WriteElementString("ows", "LowerCorner", "http://www.opengis.net/ows/1.1", "-180 -90");
            writer.WriteElementString("ows", "UpperCorner", "http://www.opengis.net/ows/1.1", "180 90");
            writer.WriteEndElement(); // ows:WGS84BoundingBox
        }

        writer.WriteEndElement(); // FeatureType
        writer.WriteEndElement(); // FeatureTypeList
    }
}
