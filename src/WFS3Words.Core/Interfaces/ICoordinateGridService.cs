using WFS3Words.Core.Models;

namespace WFS3Words.Core.Interfaces;

/// <summary>
/// Service for generating coordinate grids within bounding boxes.
/// </summary>
public interface ICoordinateGridService
{
    /// <summary>
    /// Generates a grid of coordinates within the specified bounding box.
    /// </summary>
    /// <param name="bbox">The bounding box to fill with coordinates</param>
    /// <param name="gridDensity">Grid density in points per degree (default: 0.01, ~1.1km at equator)</param>
    /// <param name="maxPoints">Maximum number of points to generate (safety limit)</param>
    /// <returns>Collection of coordinates within the bounding box</returns>
    IEnumerable<GeoCoordinate> GenerateGrid(
        BoundingBox bbox,
        double gridDensity = 0.01,
        int maxPoints = 1000);
}
