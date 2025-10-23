using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WFS3Words.Core.Interfaces;
using WFS3Words.Core.Models;

namespace WFS3Words.Tests.Integration;

/// <summary>
/// Integration tests for WFS Filter parameter support (GitHub Issue #6).
/// </summary>
public class WfsFilterTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WfsFilterTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetFeature_ShouldExtractBBoxFromFilter_WithWgs84Coordinates()
    {
        // Arrange - Filter XML with WGS84 coordinates (which the service supports)
        var mockWhat3Words = new Mock<IWhat3WordsClient>();
        mockWhat3Words
            .Setup(x => x.ConvertToWordsAsync(It.IsAny<GeoCoordinate>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new What3WordsLocation(
                Words: "filled.count.soap",
                Coordinates: new GeoCoordinate(51.520847, -0.195521),
                Country: "GB",
                Square: new BoundingBox(51.520833, -0.195542, 51.520861, -0.195500),
                NearestPlace: "London",
                Language: "en",
                Map: null));

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IWhat3WordsClient));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(mockWhat3Words.Object);
            });
        });

        var client = factory.CreateClient();

        // Filter XML with WGS84 coordinates
        var filterXml = System.Web.HttpUtility.UrlEncode(@"<Filter xmlns=""http://www.opengis.net/ogc"">
            <BBOX>
                <PropertyName>geometry</PropertyName>
                <gml:Box xmlns:gml=""http://www.opengis.net/gml"" srsName=""EPSG:4326"">
                    <gml:coordinates>-1,51 0,52</gml:coordinates>
                </gml:Box>
            </BBOX>
        </Filter>");

        // Act
        var response = await client.GetAsync($"/wfs?request=GetFeature&version=1.0.0&service=WFS&typename=location&filter={filterXml}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("FeatureCollection", content);
        Assert.DoesNotContain("BBOX parameter is required", content);
    }

    [Fact]
    public async Task GetFeature_ShouldReturnError_ForProjectedCoordinates()
    {
        // Arrange - This tests the actual GitHub issue #6 scenario
        // The client provided EPSG:27700 (British National Grid) coordinates
        // which are in meters, not degrees. This is not yet supported.
        var client = _factory.CreateClient();

        // URL-encoded Filter XML from the actual GitHub issue (EPSG:27700 coordinates)
        var filterParam = "%3cFilter+xmlns%3d%22http%3a%2f%2fwww.opengis.net%2fogc%22%3e%3cBBOX%3e%3cPropertyName%3egeometry%3c%2fPropertyName%3e%3cgml%3aBox+xmlns%3agml%3d%22http%3a%2f%2fwww.opengis.net%2fgml%22+srsName%3d%22EPSG%3a27700%22%3e%3cgml%3acoordinates%3e313713.8465717831%2c385205.1723566118+469814.4853875725%2c483863.37235661177%3c%2fgml%3acoordinates%3e%3c%2fgml%3aBox%3e%3c%2fBBOX%3e%3c%2fFilter%3e";

        // Act
        var response = await client.GetAsync($"/wfs?request=GetFeature&version=1.0.0&service=WFS&typename=location&filter={filterParam}");

        // Assert - Should now successfully parse the Filter but reject non-WGS84 coordinates
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        // Should NOT complain about missing BBOX (it was parsed from Filter)
        Assert.DoesNotContain("BBOX parameter is required", content);
        // Should explain that projected coordinates aren't supported yet
        Assert.Contains("WGS84", content);
        Assert.Contains("EPSG:4326", content);
    }

    [Fact]
    public async Task GetFeature_ShouldAcceptFilterWithGml3Envelope()
    {
        // Arrange
        var mockWhat3Words = new Mock<IWhat3WordsClient>();
        mockWhat3Words
            .Setup(x => x.ConvertToWordsAsync(It.IsAny<GeoCoordinate>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new What3WordsLocation(
                Words: "index.home.raft",
                Coordinates: new GeoCoordinate(51.5, -0.1),
                Country: "GB",
                Square: new BoundingBox(51.499, -0.101, 51.501, -0.099),
                NearestPlace: "London",
                Language: "en",
                Map: null));

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IWhat3WordsClient));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(mockWhat3Words.Object);
            });
        });

        var client = factory.CreateClient();

        // GML 3.x format with Envelope
        var filterXml = System.Web.HttpUtility.UrlEncode(@"<Filter xmlns=""http://www.opengis.net/ogc"">
            <BBOX>
                <PropertyName>geometry</PropertyName>
                <gml:Envelope xmlns:gml=""http://www.opengis.net/gml"" srsName=""EPSG:4326"">
                    <gml:lowerCorner>-1.0 51.0</gml:lowerCorner>
                    <gml:upperCorner>0.0 52.0</gml:upperCorner>
                </gml:Envelope>
            </BBOX>
        </Filter>");

        // Act
        var response = await client.GetAsync($"/wfs?request=GetFeature&version=2.0.0&service=WFS&typename=w3w:location&filter={filterXml}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("FeatureCollection", content);
    }

    [Fact]
    public async Task GetFeature_ShouldPreferDirectBBox_OverFilterBBox()
    {
        // Arrange
        var mockWhat3Words = new Mock<IWhat3WordsClient>();
        mockWhat3Words
            .Setup(x => x.ConvertToWordsAsync(It.IsAny<GeoCoordinate>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new What3WordsLocation(
                Words: "test.location.here",
                Coordinates: new GeoCoordinate(51.5, -0.5),
                Country: "GB",
                Square: new BoundingBox(51.499, -0.501, 51.501, -0.499),
                NearestPlace: "Test",
                Language: "en",
                Map: null));

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IWhat3WordsClient));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(mockWhat3Words.Object);
            });
        });

        var client = factory.CreateClient();

        var filterXml = System.Web.HttpUtility.UrlEncode(@"<Filter xmlns=""http://www.opengis.net/ogc"">
            <BBOX>
                <gml:Box xmlns:gml=""http://www.opengis.net/gml"">
                    <gml:coordinates>0,0 1,1</gml:coordinates>
                </gml:Box>
            </BBOX>
        </Filter>");

        // Act - Provide both bbox and filter parameters
        var response = await client.GetAsync($"/wfs?request=GetFeature&service=WFS&typename=w3w:location&bbox=-1,51,0,52&filter={filterXml}");

        // Assert - Should use direct BBOX, not filter
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
