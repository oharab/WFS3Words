using WFS3Words.Core.Models;

namespace WFS3Words.Tests.Unit.Models;

public class BoundingBoxTests
{
    private readonly double minLat = 51.0;
    private readonly double minLon = -1.0;
    private readonly double maxLat = 52.0;
    private readonly double maxLon = 0.0;

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenBoundingBoxIsValid()
    {
        var bbox = new BoundingBox(minLat, minLon, maxLat, maxLon);

        var result = bbox.IsValid();

        Assert.True(result);
    }

    [Theory]
    [InlineData(-90, -180, 90, 180)]  // World extent
    [InlineData(0, 0, 1, 1)]          // Small valid box
    [InlineData(51.0, -1.0, 51.0, -1.0)] // Single point (valid)
    public void IsValid_ShouldReturnTrue_WhenBoundaryConditionsAreMet(
        double minLat, double minLon, double maxLat, double maxLon)
    {
        var bbox = new BoundingBox(minLat, minLon, maxLat, maxLon);

        var result = bbox.IsValid();

        Assert.True(result);
    }

    [Theory]
    [InlineData(52.0, -1.0, 51.0, 0.0)]  // Max lat < min lat
    [InlineData(51.0, 0.0, 52.0, -1.0)]  // Max lon < min lon
    [InlineData(-91, 0, 90, 180)]        // Min lat out of range
    [InlineData(-90, 0, 91, 180)]        // Max lat out of range
    [InlineData(-90, -181, 90, 180)]     // Min lon out of range
    [InlineData(-90, -180, 90, 181)]     // Max lon out of range
    public void IsValid_ShouldReturnFalse_WhenBoundingBoxIsInvalid(
        double minLat, double minLon, double maxLat, double maxLon)
    {
        var bbox = new BoundingBox(minLat, minLon, maxLat, maxLon);

        var result = bbox.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void SouthWest_ShouldReturnCorrectCoordinate()
    {
        var bbox = new BoundingBox(minLat, minLon, maxLat, maxLon);

        var sw = bbox.SouthWest;

        Assert.Equal(minLat, sw.Latitude);
        Assert.Equal(minLon, sw.Longitude);
    }

    [Fact]
    public void NorthEast_ShouldReturnCorrectCoordinate()
    {
        var bbox = new BoundingBox(minLat, minLon, maxLat, maxLon);

        var ne = bbox.NorthEast;

        Assert.Equal(maxLat, ne.Latitude);
        Assert.Equal(maxLon, ne.Longitude);
    }

    [Fact]
    public void Width_ShouldReturnDifferenceInLongitude()
    {
        var bbox = new BoundingBox(minLat, minLon, maxLat, maxLon);

        var width = bbox.Width;

        Assert.Equal(1.0, width);
    }

    [Fact]
    public void Height_ShouldReturnDifferenceInLatitude()
    {
        var bbox = new BoundingBox(minLat, minLon, maxLat, maxLon);

        var height = bbox.Height;

        Assert.Equal(1.0, height);
    }

    [Theory]
    [InlineData(51.5, -0.5, true)]   // Inside
    [InlineData(51.0, -1.0, true)]   // On southwest corner
    [InlineData(52.0, 0.0, true)]    // On northeast corner
    [InlineData(50.0, -0.5, false)]  // South of box
    [InlineData(53.0, -0.5, false)]  // North of box
    [InlineData(51.5, -2.0, false)]  // West of box
    [InlineData(51.5, 1.0, false)]   // East of box
    public void Contains_ShouldCorrectlyDetermineIfCoordinateIsInside(
        double lat, double lon, bool expectedResult)
    {
        var bbox = new BoundingBox(minLat, minLon, maxLat, maxLon);
        var coordinate = new GeoCoordinate(lat, lon);

        var result = bbox.Contains(coordinate);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void ToString_ShouldReturnWfsFormattedString()
    {
        var bbox = new BoundingBox(minLat, minLon, maxLat, maxLon);

        var result = bbox.ToString();

        Assert.Equal("-1,51,0,52", result);
    }

    [Fact]
    public void Parse_ShouldReturnBoundingBox_WhenValidStringProvided()
    {
        var bboxString = "-1,51,0,52";

        var bbox = BoundingBox.Parse(bboxString);

        Assert.NotNull(bbox);
        Assert.Equal(51.0, bbox.MinLatitude);
        Assert.Equal(-1.0, bbox.MinLongitude);
        Assert.Equal(52.0, bbox.MaxLatitude);
        Assert.Equal(0.0, bbox.MaxLongitude);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not,enough,values")]
    [InlineData("too,many,values,here,extra")]
    [InlineData("not,numbers,at,all")]
    [InlineData("-181,51,0,52")]  // Invalid min lon
    [InlineData("-1,91,0,92")]    // Invalid lat
    public void Parse_ShouldReturnNull_WhenInvalidStringProvided(string bboxString)
    {
        var bbox = BoundingBox.Parse(bboxString);

        Assert.Null(bbox);
    }
}
