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

        // Root element varies by version
        writer.WriteStartElement("WFS_Capabilities", "http://www.opengis.net/wfs");
        writer.WriteAttributeString("version", version);
        writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
        writer.WriteAttributeString("xmlns", "gml", null, "http://www.opengis.net/gml");

        // Service Identification
        WriteServiceIdentification(writer);

        // Service Provider
        WriteServiceProvider(writer);

        // Operations Metadata
        WriteOperationsMetadata(writer, serviceUrl);

        // Feature Type List
        WriteFeatureTypeList(writer);

        writer.WriteEndElement(); // WFS_Capabilities
        writer.WriteEndDocument();
        writer.Flush();

        return stringWriter.ToString();
    }

    private void WriteServiceIdentification(XmlWriter writer)
    {
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

    private void WriteServiceProvider(XmlWriter writer)
    {
        if (string.IsNullOrEmpty(_options.ProviderName) &&
            string.IsNullOrEmpty(_options.ContactPerson))
        {
            return; // Skip if no provider info configured
        }

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

    private void WriteOperationsMetadata(XmlWriter writer, string serviceUrl)
    {
        writer.WriteStartElement("Capability");
        writer.WriteStartElement("Request");

        // GetCapabilities
        WriteOperation(writer, "GetCapabilities", serviceUrl);

        // DescribeFeatureType
        WriteOperation(writer, "DescribeFeatureType", serviceUrl);

        // GetFeature
        WriteOperation(writer, "GetFeature", serviceUrl);

        writer.WriteEndElement(); // Request
        writer.WriteEndElement(); // Capability
    }

    private void WriteOperation(XmlWriter writer, string operationName, string serviceUrl)
    {
        writer.WriteStartElement(operationName);
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

    private void WriteFeatureTypeList(XmlWriter writer)
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
        writer.WriteElementString("DefaultSRS", "EPSG:4326");

        // List all supported CRS
        foreach (var srs in _transformationService.SupportedEpsgCodes)
        {
            if (srs != "EPSG:4326") // Already listed as DefaultSRS
            {
                writer.WriteElementString("OtherSRS", srs);
            }
        }

        // Bounding box - global coverage
        writer.WriteStartElement("WGS84BoundingBox");
        writer.WriteElementString("LowerCorner", "-180 -90");
        writer.WriteElementString("UpperCorner", "180 90");
        writer.WriteEndElement(); // WGS84BoundingBox

        writer.WriteEndElement(); // FeatureType
        writer.WriteEndElement(); // FeatureTypeList
    }
}
