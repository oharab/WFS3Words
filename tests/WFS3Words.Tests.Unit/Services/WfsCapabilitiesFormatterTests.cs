using Microsoft.Extensions.Options;
using WFS3Words.Core.Configuration;
using WFS3Words.Core.Services;

namespace WFS3Words.Tests.Unit.Services;

public class WfsCapabilitiesFormatterTests
{
    private readonly WfsCapabilitiesFormatter _formatter;
    private readonly WfsOptions _options;

    public WfsCapabilitiesFormatterTests()
    {
        _options = new WfsOptions
        {
            ServiceTitle = "Test WFS Service",
            ServiceAbstract = "Test service for What3Words",
            Keywords = "WFS,Test,What3Words",
            Fees = "none",
            AccessConstraints = "none",
            ProviderName = "Test Provider",
            ContactPerson = "Test Person",
            ContactEmail = "test@example.com"
        };

        var transformationService = new CoordinateTransformationService();
        _formatter = new WfsCapabilitiesFormatter(Options.Create(_options), transformationService);
    }

    [Fact]
    public void GenerateCapabilities_ShouldReturnValidXml_WhenCalled()
    {
        var serviceUrl = "http://localhost/wfs";
        var version = "2.0.0";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("<?xml", result);
        Assert.Contains("WFS_Capabilities", result);
    }

    [Fact]
    public void GenerateCapabilities_ShouldIncludeServiceTitle()
    {
        var serviceUrl = "http://localhost/wfs";
        var version = "2.0.0";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        Assert.Contains("<Title>Test WFS Service</Title>", result);
    }

    [Fact]
    public void GenerateCapabilities_ShouldIncludeVersion()
    {
        var serviceUrl = "http://localhost/wfs";
        var version = "1.0.0";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        Assert.Contains("version=\"1.0.0\"", result);
    }

    [Fact]
    public void GenerateCapabilities_ShouldIncludeAllOperations()
    {
        var serviceUrl = "http://localhost/wfs";
        var version = "2.0.0";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        Assert.Contains("GetCapabilities", result);
        Assert.Contains("DescribeFeatureType", result);
        Assert.Contains("GetFeature", result);
    }

    [Fact]
    public void GenerateCapabilities_ShouldIncludeServiceUrl()
    {
        var serviceUrl = "http://localhost:8080/wfs";
        var version = "2.0.0";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        Assert.Contains(serviceUrl, result);
    }

    [Fact]
    public void GenerateCapabilities_ShouldIncludeFeatureType()
    {
        var serviceUrl = "http://localhost/wfs";
        var version = "2.0.0";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        Assert.Contains("w3w:location", result);
        Assert.Contains("What3Words Location", result);
    }

    [Fact]
    public void GenerateCapabilities_ShouldIncludeDescribeFeatureTypeSupportedFormats()
    {
        var serviceUrl = "http://localhost/wfs";
        var version = "2.0.0";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        Assert.Contains("SchemaDescriptionLanguage", result);
        Assert.Contains("<XMLSCHEMA", result);  // Element format: <XMLSCHEMA/>
    }

    [Fact]
    public void GenerateCapabilities_ShouldIncludeKeywords()
    {
        var serviceUrl = "http://localhost/wfs";
        var version = "2.0.0";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        Assert.Contains("<Keyword>WFS</Keyword>", result);
        Assert.Contains("<Keyword>Test</Keyword>", result);
        Assert.Contains("<Keyword>What3Words</Keyword>", result);
    }

    [Fact]
    public void GenerateCapabilities_ShouldIncludeContactInfo()
    {
        var serviceUrl = "http://localhost/wfs";
        var version = "2.0.0";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        Assert.Contains("Test Provider", result);
        Assert.Contains("Test Person", result);
        Assert.Contains("test@example.com", result);
    }

    [Theory]
    [InlineData("1.0.0", "http://www.opengis.net/wfs")]
    [InlineData("1.1.0", "http://www.opengis.net/wfs")]
    public void GenerateCapabilities_ShouldUseCorrectNamespace_ForWfs1x(string version, string expectedNamespace)
    {
        var serviceUrl = "http://localhost/wfs";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        Assert.Contains($"xmlns=\"{expectedNamespace}\"", result);
        Assert.Contains($"version=\"{version}\"", result);
    }

    [Fact]
    public void GenerateCapabilities_ShouldUseCorrectNamespace_ForWfs20()
    {
        var serviceUrl = "http://localhost/wfs";
        var version = "2.0.0";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        Assert.Contains("xmlns=\"http://www.opengis.net/wfs/2.0\"", result);
        Assert.Contains("version=\"2.0.0\"", result);
    }

    [Fact]
    public void GenerateCapabilities_ShouldNotUseWfs1Namespace_ForWfs20()
    {
        var serviceUrl = "http://localhost/wfs";
        var version = "2.0.0";

        var result = _formatter.GenerateCapabilities(version, serviceUrl);

        // Should NOT contain the WFS 1.x namespace when version is 2.0.0
        Assert.DoesNotContain("xmlns=\"http://www.opengis.net/wfs\"", result);
        // Should contain the WFS 2.0 namespace
        Assert.Contains("xmlns=\"http://www.opengis.net/wfs/2.0\"", result);
    }
}
