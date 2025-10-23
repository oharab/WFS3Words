using WFS3Words.Core.Interfaces;
using WFS3Words.Core.Models;

namespace WFS3Words.Core.Services;

/// <summary>
/// Service for generating coordinate grids within bounding boxes.
/// </summary>
public class CoordinateGridService : ICoordinateGridService
{
    /// <inheritdoc />
    public IEnumerable<GeoCoordinate> GenerateGrid(
        BoundingBox bbox,
        double gridDensity = 0.01,
        int maxPoints = 1000)
    {
        if (!bbox.IsValid())
        {
            throw new ArgumentException("Invalid bounding box - coordinates must be in WGS84 (EPSG:4326). " +
                "Projected coordinate systems are not currently supported.", nameof(bbox));
        }

        if (gridDensity <= 0)
        {
            throw new ArgumentException("Grid density must be positive", nameof(gridDensity));
        }

        if (maxPoints <= 0)
        {
            throw new ArgumentException("Max points must be positive", nameof(maxPoints));
        }

        var points = new List<GeoCoordinate>();
        var pointCount = 0;

        // Calculate step size based on grid density
        var latStep = 1.0 / gridDensity;
        var lonStep = 1.0 / gridDensity;

        // Generate grid points
        for (var lat = bbox.MinLatitude; lat <= bbox.MaxLatitude; lat += latStep)
        {
            for (var lon = bbox.MinLongitude; lon <= bbox.MaxLongitude; lon += lonStep)
            {
                if (pointCount >= maxPoints)
                {
                    return points;
                }

                var coordinate = new GeoCoordinate(lat, lon);

                // Ensure coordinate is within bounds and valid
                if (coordinate.IsValid() && bbox.Contains(coordinate))
                {
                    points.Add(coordinate);
                    pointCount++;
                }
            }
        }

        return points;
    }
}
