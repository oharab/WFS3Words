using System.Xml;
using System.Xml.Schema;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WFS3Words.Tests.Integration;

/// <summary>
/// Integration tests that validate WFS GetCapabilities responses against official OGC XSD schemas.
/// </summary>
public class WfsCapabilitiesXsdValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    // OGC schema URLs
    private const string WFS_100_CAPABILITIES_XSD = "http://schemas.opengis.net/wfs/1.0.0/WFS-capabilities.xsd";
    private const string WFS_200_XSD = "http://schemas.opengis.net/wfs/2.0/wfs.xsd";

    public WfsCapabilitiesXsdValidationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCapabilities_WFS100_ShouldValidateAgainstOfficialXsd()
    {
        // Arrange
        var version = "1.0.0";

        // Act
        var response = await _client.GetAsync($"/wfs?service=WFS&request=GetCapabilities&version={version}");
        response.EnsureSuccessStatusCode();
        var xmlContent = await response.Content.ReadAsStringAsync();

        // Assert
        var validationErrors = await ValidateXmlAgainstXsd(xmlContent, WFS_100_CAPABILITIES_XSD);

        // If validation fails, output detailed errors for debugging
        if (validationErrors.Any())
        {
            var errorMessage = "XSD Validation failed:\n" + string.Join("\n", validationErrors);
            Assert.Fail(errorMessage);
        }
    }

    [Fact]
    public async Task GetCapabilities_WFS200_ShouldValidateAgainstOfficialXsd()
    {
        // Arrange
        var version = "2.0.0";

        // Act
        var response = await _client.GetAsync($"/wfs?service=WFS&request=GetCapabilities&version={version}");
        response.EnsureSuccessStatusCode();
        var xmlContent = await response.Content.ReadAsStringAsync();

        // Assert
        var validationErrors = await ValidateXmlAgainstXsd(xmlContent, WFS_200_XSD);

        // If validation fails, output detailed errors for debugging
        if (validationErrors.Any())
        {
            var errorMessage = "XSD Validation failed:\n" + string.Join("\n", validationErrors);
            Assert.Fail(errorMessage);
        }
    }

    /// <summary>
    /// Validates XML content against an XSD schema from a URL.
    /// </summary>
    /// <param name="xmlContent">The XML content to validate</param>
    /// <param name="schemaUrl">The URL of the XSD schema</param>
    /// <returns>List of validation error messages (empty if valid)</returns>
    private async Task<List<string>> ValidateXmlAgainstXsd(string xmlContent, string schemaUrl)
    {
        var validationErrors = new List<string>();

        try
        {
            // Create XML schema set and add the main schema
            var schemaSet = new XmlSchemaSet();

            // Configure XML resolver to fetch schemas from OGC
            var resolver = new XmlUrlResolver();
            schemaSet.XmlResolver = resolver;

            // Load the main schema from URL
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var schemaXml = await httpClient.GetStringAsync(schemaUrl);
            using var schemaReader = new StringReader(schemaXml);
            using var schemaXmlReader = XmlReader.Create(schemaReader, new XmlReaderSettings { XmlResolver = resolver });

            schemaSet.Add(null, schemaXmlReader);
            schemaSet.Compile();

            // Configure XML reader settings with the schema
            var readerSettings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet,
                XmlResolver = resolver,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                                 XmlSchemaValidationFlags.ProcessSchemaLocation |
                                 XmlSchemaValidationFlags.ReportValidationWarnings
            };

            // Add validation event handler to collect errors
            readerSettings.ValidationEventHandler += (sender, args) =>
            {
                var severity = args.Severity == XmlSeverityType.Warning ? "Warning" : "Error";
                validationErrors.Add($"{severity}: {args.Message} (Line {args.Exception?.LineNumber}, Position {args.Exception?.LinePosition})");
            };

            // Validate the XML
            using var stringReader = new StringReader(xmlContent);
            using var xmlReader = XmlReader.Create(stringReader, readerSettings);

            // Read through entire document to trigger validation
            while (xmlReader.Read())
            {
                // Reading triggers validation
            }
        }
        catch (Exception ex)
        {
            validationErrors.Add($"Validation failed with exception: {ex.Message}");
        }

        return validationErrors;
    }
}
