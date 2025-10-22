using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WFS3Words.Tests.Integration;

public class WfsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WfsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        // Disable automatic redirect following so we can test redirect responses
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetCapabilities_ShouldReturnXml_WhenRequested()
    {
        var response = await _client.GetAsync("/wfs?service=WFS&request=GetCapabilities&version=2.0.0");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("WFS_Capabilities", content);
        Assert.Contains("version=\"2.0.0\"", content);
    }

    [Fact]
    public async Task GetCapabilities_ShouldWorkWithDifferentVersions()
    {
        var response = await _client.GetAsync("/wfs?service=WFS&request=GetCapabilities&version=1.0.0");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("version=\"1.0.0\"", content);
    }

    [Fact]
    public async Task GetCapabilities_ShouldBeCaseInsensitive()
    {
        var response = await _client.GetAsync("/wfs?SERVICE=WFS&REQUEST=GETCAPABILITIES&VERSION=2.0.0");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("WFS_Capabilities", content);
    }

    [Fact]
    public async Task DescribeFeatureType_ShouldReturnXmlSchema()
    {
        var response = await _client.GetAsync("/wfs?service=WFS&request=DescribeFeatureType&version=2.0.0");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("schema", content);
        Assert.Contains("locationType", content);
    }

    [Fact]
    public async Task DescribeFeatureType_ShouldReturnXmlSchema_WhenOutputFormatIsXMLSCHEMA()
    {
        var response = await _client.GetAsync("/wfs?service=WFS&request=DescribeFeatureType&version=2.0.0&outputFormat=XMLSCHEMA");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("schema", content);
        Assert.Contains("locationType", content);
    }

    [Fact]
    public async Task DescribeFeatureType_ShouldReturnXmlSchema_WhenOutputFormatIsTextXml()
    {
        var response = await _client.GetAsync("/wfs?service=WFS&request=DescribeFeatureType&version=2.0.0&outputFormat=text/xml;%20subtype=gml/3.1.1");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("schema", content);
    }

    [Fact]
    public async Task DescribeFeatureType_ShouldReturnBadRequest_WhenOutputFormatUnsupported()
    {
        var response = await _client.GetAsync("/wfs?service=WFS&request=DescribeFeatureType&version=2.0.0&outputFormat=application/json");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not supported", content);
    }

    [Fact]
    public async Task GetFeature_ShouldReturnBadRequest_WhenBBoxMissing()
    {
        var response = await _client.GetAsync("/wfs?service=WFS&request=GetFeature&version=2.0.0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetFeature_ShouldReturnBadRequest_WhenBBoxInvalid()
    {
        var response = await _client.GetAsync("/wfs?service=WFS&request=GetFeature&bbox=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRequest_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/wfs?service=WFS&request=InvalidRequest");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingRequest_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/wfs?service=WFS");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health");

        // May return 200 or 503 depending on W3W API availability
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task RootEndpoint_ShouldRedirectToHealth()
    {
        var response = await _client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead);

        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.MovedPermanently ||
            response.Headers.Location?.ToString().Contains("health") == true);
    }
}
