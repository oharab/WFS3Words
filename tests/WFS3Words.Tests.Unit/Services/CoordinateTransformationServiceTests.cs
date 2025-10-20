using WFS3Words.Core.Models;
using WFS3Words.Core.Services;

namespace WFS3Words.Tests.Unit.Services;

public class CoordinateTransformationServiceTests
{
    private readonly CoordinateTransformationService _service;

    public CoordinateTransformationServiceTests()
    {
        _service = new CoordinateTransformationService();
    }

    [Fact]
    public void SupportedEpsgCodes_ShouldReturnListOfCodes()
    {
        var codes = _service.SupportedEpsgCodes;

        Assert.NotNull(codes);
        Assert.NotEmpty(codes);
        Assert.Contains("EPSG:4326", codes);
        Assert.Contains("EPSG:3857", codes);
        Assert.Contains("EPSG:27700", codes);
    }

    [Fact]
    public void Transform_ShouldReturnSameCoordinate_WhenTargetIsWGS84()
    {
        var london = new GeoCoordinate(51.5074, -0.1278);

        var result = _service.Transform(london, "EPSG:4326");

        Assert.Equal(london.Latitude, result.Latitude, precision: 6);
        Assert.Equal(london.Longitude, result.Longitude, precision: 6);
    }

    [Fact]
    public void Transform_ShouldConvertToWebMercator()
    {
        // London: 51.5074°N, 0.1278°W
        var london = new GeoCoordinate(51.5074, -0.1278);

        var result = _service.Transform(london, "EPSG:3857");

        // Web Mercator should give us meter coordinates
        // Verify the transformation produces reasonable values for London
        Assert.True(result.Longitude < 0, "London longitude (easting) should be negative (west of prime meridian)");
        Assert.True(Math.Abs(result.Longitude) < 50000, "London easting should be within 50km of prime meridian");
        Assert.True(result.Latitude > 6600000 && result.Latitude < 6750000, "London northing should be between 6.6M and 6.75M meters");
    }

    [Fact]
    public void Transform_ShouldConvertToBritishNationalGrid()
    {
        // Trafalgar Square, London: 51.5081°N, 0.1281°W
        var trafalgarSquare = new GeoCoordinate(51.5081, -0.1281);

        var result = _service.Transform(trafalgarSquare, "EPSG:27700");

        // British National Grid coordinates for Trafalgar Square are approximately:
        // Easting: 530000, Northing: 180000
        Assert.True(Math.Abs(result.Longitude - 530000) < 1000); // Easting
        Assert.True(Math.Abs(result.Latitude - 180000) < 1000);  // Northing
    }

    [Fact]
    public void Transform_ShouldConvertToUTM()
    {
        // London is in UTM zone 30N or 31N
        var london = new GeoCoordinate(51.5074, -0.1278);

        var result = _service.Transform(london, "EPSG:32630");

        // UTM coordinates should be in meters, positive values
        Assert.True(result.Longitude > 0);  // Easting
        Assert.True(result.Latitude > 0);   // Northing
        Assert.True(result.Longitude < 1000000); // Reasonable UTM easting
        Assert.True(result.Latitude > 5000000 && result.Latitude < 10000000); // Reasonable northern hemisphere northing
    }

    [Fact]
    public void Transform_ShouldThrowArgumentException_WhenCoordinateIsInvalid()
    {
        var invalidCoord = new GeoCoordinate(91, 0); // Invalid latitude

        Assert.Throws<ArgumentException>(() =>
            _service.Transform(invalidCoord, "EPSG:3857"));
    }

    [Fact]
    public void Transform_ShouldThrowArgumentException_WhenTargetCrsIsUnsupported()
    {
        var london = new GeoCoordinate(51.5074, -0.1278);

        Assert.Throws<ArgumentException>(() =>
            _service.Transform(london, "EPSG:99999"));
    }

    [Theory]
    [InlineData("EPSG:4326", true)]
    [InlineData("EPSG:3857", true)]
    [InlineData("EPSG:27700", true)]
    [InlineData("EPSG:4258", true)]
    [InlineData("EPSG:32630", true)]
    [InlineData("EPSG:99999", false)]
    [InlineData("INVALID", false)]
    public void IsSupported_ShouldReturnCorrectValue(string epsgCode, bool expectedSupported)
    {
        var result = _service.IsSupported(epsgCode);

        Assert.Equal(expectedSupported, result);
    }

    [Theory]
    [InlineData("EPSG:4326", "EPSG:4326")]
    [InlineData("4326", "EPSG:4326")]
    [InlineData("epsg:4326", "EPSG:4326")]
    [InlineData("  EPSG:4326  ", "EPSG:4326")]
    [InlineData("urn:ogc:def:crs:EPSG::4326", "EPSG:4326")]
    [InlineData("http://www.opengis.net/def/crs/EPSG/0/4326", "EPSG:4326")]
    [InlineData("", "EPSG:4326")] // Default
    [InlineData(null, "EPSG:4326")] // Default
    public void NormalizeEpsgCode_ShouldReturnNormalizedCode(string input, string expected)
    {
        var result = _service.NormalizeEpsgCode(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Transform_ShouldBeConsistent_WhenTransformingMultipleTimes()
    {
        var coordinate = new GeoCoordinate(51.5074, -0.1278);

        var result1 = _service.Transform(coordinate, "EPSG:3857");
        var result2 = _service.Transform(coordinate, "EPSG:3857");

        Assert.Equal(result1.Latitude, result2.Latitude, precision: 6);
        Assert.Equal(result1.Longitude, result2.Longitude, precision: 6);
    }

    [Fact]
    public void Transform_ShouldWorkForAllSupportedCRS()
    {
        var london = new GeoCoordinate(51.5074, -0.1278);

        foreach (var epsgCode in _service.SupportedEpsgCodes)
        {
            // Should not throw
            var result = _service.Transform(london, epsgCode);

            Assert.NotNull(result);
        }
    }

    [Fact]
    public void Transform_ShouldHandleCaseInsensitiveEpsgCodes()
    {
        var london = new GeoCoordinate(51.5074, -0.1278);

        var result1 = _service.Transform(london, "EPSG:3857");
        var result2 = _service.Transform(london, "epsg:3857");
        var result3 = _service.Transform(london, "3857");

        Assert.Equal(result1.Latitude, result2.Latitude, precision: 6);
        Assert.Equal(result1.Longitude, result2.Longitude, precision: 6);
        Assert.Equal(result1.Latitude, result3.Latitude, precision: 6);
        Assert.Equal(result1.Longitude, result3.Longitude, precision: 6);
    }

    [Fact]
    public void Transform_ShouldConvertToFrenchLambert93()
    {
        // Paris coordinates: 48.8566°N, 2.3522°E
        var paris = new GeoCoordinate(48.8566, 2.3522);

        var result = _service.Transform(paris, "EPSG:2154");

        // Lambert-93 coordinates for Paris should be approximately:
        // X: 652000, Y: 6862000
        Assert.True(Math.Abs(result.Longitude - 652000) < 5000); // X
        Assert.True(Math.Abs(result.Latitude - 6862000) < 5000); // Y
    }

    [Fact]
    public void Transform_ShouldConvertToETRS89()
    {
        // ETRS89 is very close to WGS84 for most practical purposes
        var berlin = new GeoCoordinate(52.5200, 13.4050);

        var result = _service.Transform(berlin, "EPSG:4258");

        // Should be very close to original WGS84 coordinates
        Assert.Equal(berlin.Latitude, result.Latitude, precision: 4);
        Assert.Equal(berlin.Longitude, result.Longitude, precision: 4);
    }
}
