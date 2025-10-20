using WFS3Words.Core.Models;
using WFS3Words.Core.Services;

namespace WFS3Words.Tests.Unit.Services;

public class CoordinateGridServiceTests
{
    private readonly CoordinateGridService _service;

    public CoordinateGridServiceTests()
    {
        _service = new CoordinateGridService();
    }

    [Fact]
    public void GenerateGrid_ShouldReturnCoordinates_WhenValidBoundingBoxProvided()
    {
        var bbox = new BoundingBox(51.0, -1.0, 52.0, 0.0);
        var gridDensity = 1.0; // 1 point per degree

        var result = _service.GenerateGrid(bbox, gridDensity).ToList();

        Assert.NotEmpty(result);
        Assert.All(result, coord => Assert.True(bbox.Contains(coord)));
    }

    [Fact]
    public void GenerateGrid_ShouldGenerateCorrectNumberOfPoints_ForSimpleGrid()
    {
        var bbox = new BoundingBox(0.0, 0.0, 2.0, 2.0);
        var gridDensity = 1.0; // 1 point per degree

        var result = _service.GenerateGrid(bbox, gridDensity, maxPoints: 100).ToList();

        // For a 2x2 degree box with 1 point per degree, expect 9 points (3x3 grid including boundaries)
        Assert.Equal(9, result.Count);
    }

    [Fact]
    public void GenerateGrid_ShouldRespectMaxPoints_WhenGridWouldExceedLimit()
    {
        var bbox = new BoundingBox(0.0, 0.0, 10.0, 10.0);
        var gridDensity = 1.0;
        var maxPoints = 50;

        var result = _service.GenerateGrid(bbox, gridDensity, maxPoints).ToList();

        Assert.True(result.Count <= maxPoints);
        Assert.Equal(maxPoints, result.Count);
    }

    [Fact]
    public void GenerateGrid_ShouldGenerateFewerPoints_WithLowerDensity()
    {
        var bbox = new BoundingBox(0.0, 0.0, 10.0, 10.0);

        var lowDensityResult = _service.GenerateGrid(bbox, gridDensity: 0.1, maxPoints: 1000).ToList();
        var highDensityResult = _service.GenerateGrid(bbox, gridDensity: 1.0, maxPoints: 1000).ToList();

        Assert.True(lowDensityResult.Count < highDensityResult.Count);
    }

    [Fact]
    public void GenerateGrid_ShouldGenerateAllPointsWithinBounds()
    {
        var bbox = new BoundingBox(51.0, -1.0, 52.0, 0.0);
        var gridDensity = 0.1;

        var result = _service.GenerateGrid(bbox, gridDensity, maxPoints: 5000).ToList();

        Assert.All(result, coord =>
        {
            Assert.True(coord.Latitude >= bbox.MinLatitude);
            Assert.True(coord.Latitude <= bbox.MaxLatitude);
            Assert.True(coord.Longitude >= bbox.MinLongitude);
            Assert.True(coord.Longitude <= bbox.MaxLongitude);
        });
    }

    [Fact]
    public void GenerateGrid_ShouldIncludeCornerPoints_WhenAligned()
    {
        var bbox = new BoundingBox(0.0, 0.0, 1.0, 1.0);
        var gridDensity = 1.0;

        var result = _service.GenerateGrid(bbox, gridDensity).ToList();

        // Should include all four corners for a 1x1 degree box
        Assert.Contains(result, c => c.Latitude == 0.0 && c.Longitude == 0.0);
        Assert.Contains(result, c => c.Latitude == 0.0 && c.Longitude == 1.0);
        Assert.Contains(result, c => c.Latitude == 1.0 && c.Longitude == 0.0);
        Assert.Contains(result, c => c.Latitude == 1.0 && c.Longitude == 1.0);
    }

    [Fact]
    public void GenerateGrid_ShouldThrowArgumentException_WhenBoundingBoxIsInvalid()
    {
        var invalidBbox = new BoundingBox(52.0, -1.0, 51.0, 0.0); // Max < Min

        Assert.Throws<ArgumentException>(() =>
            _service.GenerateGrid(invalidBbox).ToList());
    }

    [Fact]
    public void GenerateGrid_ShouldThrowArgumentException_WhenGridDensityIsZero()
    {
        var bbox = new BoundingBox(51.0, -1.0, 52.0, 0.0);

        Assert.Throws<ArgumentException>(() =>
            _service.GenerateGrid(bbox, gridDensity: 0).ToList());
    }

    [Fact]
    public void GenerateGrid_ShouldThrowArgumentException_WhenGridDensityIsNegative()
    {
        var bbox = new BoundingBox(51.0, -1.0, 52.0, 0.0);

        Assert.Throws<ArgumentException>(() =>
            _service.GenerateGrid(bbox, gridDensity: -1).ToList());
    }

    [Fact]
    public void GenerateGrid_ShouldThrowArgumentException_WhenMaxPointsIsZero()
    {
        var bbox = new BoundingBox(51.0, -1.0, 52.0, 0.0);

        Assert.Throws<ArgumentException>(() =>
            _service.GenerateGrid(bbox, maxPoints: 0).ToList());
    }

    [Fact]
    public void GenerateGrid_ShouldThrowArgumentException_WhenMaxPointsIsNegative()
    {
        var bbox = new BoundingBox(51.0, -1.0, 52.0, 0.0);

        Assert.Throws<ArgumentException>(() =>
            _service.GenerateGrid(bbox, maxPoints: -1).ToList());
    }

    [Fact]
    public void GenerateGrid_ShouldWorkForSmallBoundingBox()
    {
        // Test with a very small area (100m x 100m roughly)
        var bbox = new BoundingBox(51.5, -0.1, 51.501, -0.099);
        var gridDensity = 10.0; // 10 points per degree

        var result = _service.GenerateGrid(bbox, gridDensity, maxPoints: 100).ToList();

        Assert.NotEmpty(result);
        Assert.All(result, coord => Assert.True(bbox.Contains(coord)));
    }

    [Fact]
    public void GenerateGrid_ShouldReturnAtLeastOnePoint_ForTinyBoundingBox()
    {
        // Even for a very small box, should return at least one point
        var bbox = new BoundingBox(51.5, -0.1, 51.5001, -0.0999);
        var gridDensity = 1.0;

        var result = _service.GenerateGrid(bbox, gridDensity, maxPoints: 100).ToList();

        Assert.NotEmpty(result);
    }
}
