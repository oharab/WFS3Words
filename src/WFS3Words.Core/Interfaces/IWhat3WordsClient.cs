using WFS3Words.Core.Models;

namespace WFS3Words.Core.Interfaces;

/// <summary>
/// Client interface for interacting with the What3Words API.
/// </summary>
public interface IWhat3WordsClient
{
    /// <summary>
    /// Converts geographic coordinates to a What3Words 3-word address.
    /// </summary>
    /// <param name="coordinate">The geographic coordinate to convert</param>
    /// <param name="language">Language code for the 3-word address (default: en)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>What3Words location information</returns>
    /// <exception cref="What3WordsException">Thrown when the API request fails</exception>
    Task<What3WordsLocation> ConvertToWordsAsync(
        GeoCoordinate coordinate,
        string language = "en",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the What3Words API is accessible.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the API is accessible, false otherwise</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
