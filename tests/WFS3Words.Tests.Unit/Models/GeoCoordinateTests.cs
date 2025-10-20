using WFS3Words.Core.Models;

namespace WFS3Words.Tests.Unit.Models;

public class GeoCoordinateTests
{
    [Fact]
    public void IsValid_ShouldReturnTrue_WhenCoordinatesAreWithinValidRange()
    {
        var coordinate = new GeoCoordinate(51.520847, -0.195521);

        var result = coordinate.IsValid();

        Assert.True(result);
    }

    [Theory]
    [InlineData(-90, -180)] // Min valid values
    [InlineData(90, 180)]   // Max valid values
    [InlineData(0, 0)]      // Equator, Prime Meridian
    [InlineData(51.5, -0.2)] // London
    public void IsValid_ShouldReturnTrue_WhenCoordinatesAreBoundaryOrValid(double lat, double lon)
    {
        var coordinate = new GeoCoordinate(lat, lon);

        var result = coordinate.IsValid();

        Assert.True(result);
    }

    [Theory]
    [InlineData(-91, 0)]    // Latitude too small
    [InlineData(91, 0)]     // Latitude too large
    [InlineData(0, -181)]   // Longitude too small
    [InlineData(0, 181)]    // Longitude too large
    [InlineData(100, 200)]  // Both invalid
    public void IsValid_ShouldReturnFalse_WhenCoordinatesAreOutOfRange(double lat, double lon)
    {
        var coordinate = new GeoCoordinate(lat, lon);

        var result = coordinate.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void ToString_ShouldReturnCommaSeparatedLatLon()
    {
        var coordinate = new GeoCoordinate(51.520847, -0.195521);

        var result = coordinate.ToString();

        Assert.Equal("51.520847,-0.195521", result);
    }
}
