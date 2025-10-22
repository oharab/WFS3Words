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

        // WFS 2.0.0 requires OWS and xlink namespaces
        if (version.StartsWith("2."))
        {
            writer.WriteAttributeString("xmlns", "ows", null, "http://www.opengis.net/ows/1.1");
            writer.WriteAttributeString("xmlns", "xlink", null, "http://www.w3.org/1999/xlink");
        }

        // Service Identification
        WriteServiceIdentification(writer, version);

        // Service Provider
        WriteServiceProvider(writer, version);

        // Operations Metadata
        WriteOperationsMetadata(writer, serviceUrl, version);

        // Feature Type List
        WriteFeatureTypeList(writer, version);

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

            // Keywords
            writer.WriteStartElement("Keywords");
            foreach (var keyword in _options.Keywords.Split(','))
            {
                writer.WriteElementString("Keyword", keyword.Trim());
            }
            writer.WriteEndElement(); // Keywords

            writer.WriteElementString("Fees", _options.Fees);
            writer.WriteElementString("AccessConstraints", _options.AccessConstraints);

            writer.WriteEndElement(); // Service
        }
    }

    private void WriteServiceProvider(XmlWriter writer, string version)
    {
        if (string.IsNullOrEmpty(_options.ProviderName) &&
            string.IsNullOrEmpty(_options.ContactPerson))
        {
            return; // Skip if no provider info configured
        }

        if (version.StartsWith("2."))
        {
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
        else
        {
            // WFS 1.0.0: Use ServiceProvider element (no ows prefix)
            writer.WriteStartElement("ServiceProvider");

            if (!string.IsNullOrEmpty(_options.ProviderName))
            {
                writer.WriteElementString("ProviderName", _options.ProviderName);
            }

            if (!string.IsNullOrEmpty(_options.ProviderSite))
            {
                writer.WriteStartElement("ProviderSite");
                writer.WriteAttributeString("href", _options.ProviderSite);
                writer.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(_options.ContactPerson) ||
                !string.IsNullOrEmpty(_options.ContactEmail))
            {
                writer.WriteStartElement("ServiceContact");

                if (!string.IsNullOrEmpty(_options.ContactPerson))
                {
                    writer.WriteElementString("IndividualName", _options.ContactPerson);
                }

                if (!string.IsNullOrEmpty(_options.ContactEmail))
                {
                    writer.WriteStartElement("ContactInfo");
                    writer.WriteStartElement("Address");
                    writer.WriteElementString("ElectronicMailAddress", _options.ContactEmail);
                    writer.WriteEndElement(); // Address
                    writer.WriteEndElement(); // ContactInfo
                }

                writer.WriteEndElement(); // ServiceContact
            }

            writer.WriteEndElement(); // ServiceProvider
        }
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

    private void WriteFeatureTypeList(XmlWriter writer, string version)
    {
        writer.WriteStartElement("FeatureTypeList");
        writer.WriteStartElement("Operations");
        writer.WriteElementString("Operation", "Query");
        writer.WriteEndElement(); // Operations

        // Define the What3Words location feature type
        writer.WriteStartElement("FeatureType");
        writer.WriteElementString("Name", "w3w:location");
        writer.WriteElementString("Title", "What3Words Location");
        writer.WriteElementString("Abstract", "Geographic location with What3Words 3-word address");

        // WFS 1.0.0 uses <SRS>, WFS 2.0.0+ uses <DefaultSRS> and <OtherSRS>
        if (version == "1.0.0")
        {
            // WFS 1.0.0: Use single <SRS> element
            writer.WriteElementString("SRS", "EPSG:4326");
        }
        else
        {
            // WFS 2.0.0+: Use <DefaultSRS> and <OtherSRS>
            writer.WriteElementString("DefaultSRS", "EPSG:4326");

            // List all supported CRS
            foreach (var srs in _transformationService.SupportedEpsgCodes)
            {
                if (srs != "EPSG:4326") // Already listed as DefaultSRS
                {
                    writer.WriteElementString("OtherSRS", srs);
                }
            }
        }

        // WFS 1.0.0 uses <LatLongBoundingBox>, WFS 2.0.0+ uses <WGS84BoundingBox>
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
            // WFS 2.0.0+: Use <WGS84BoundingBox> with child elements
            writer.WriteStartElement("WGS84BoundingBox");
            writer.WriteElementString("LowerCorner", "-180 -90");
            writer.WriteElementString("UpperCorner", "180 90");
            writer.WriteEndElement(); // WGS84BoundingBox
        }

        writer.WriteEndElement(); // FeatureType
        writer.WriteEndElement(); // FeatureTypeList
    }
}
