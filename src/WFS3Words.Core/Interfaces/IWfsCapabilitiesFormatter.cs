namespace WFS3Words.Core.Interfaces;

/// <summary>
/// Formatter for WFS GetCapabilities responses.
/// </summary>
public interface IWfsCapabilitiesFormatter
{
    /// <summary>
    /// Generates a WFS GetCapabilities XML response.
    /// </summary>
    /// <param name="version">WFS version (1.0.0, 1.1.0, or 2.0.0)</param>
    /// <param name="serviceUrl">The base URL of the WFS service</param>
    /// <returns>GetCapabilities XML document as string</returns>
    string GenerateCapabilities(string version, string serviceUrl);
}
