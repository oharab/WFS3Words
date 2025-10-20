namespace WFS3Words.Core.Configuration;

/// <summary>
/// Configuration options for WFS service metadata.
/// </summary>
public class WfsOptions
{
    /// <summary>
    /// The section name in appsettings.json
    /// </summary>
    public const string SectionName = "WFS";

    /// <summary>
    /// Service title for WFS GetCapabilities
    /// </summary>
    public string ServiceTitle { get; set; } = "What3Words WFS Service";

    /// <summary>
    /// Service abstract/description
    /// </summary>
    public string ServiceAbstract { get; set; } =
        "OGC Web Feature Service providing access to What3Words location data";

    /// <summary>
    /// Service keywords (comma-separated)
    /// </summary>
    public string Keywords { get; set; } = "WFS,What3Words,OGC,Location";

    /// <summary>
    /// Service fees information
    /// </summary>
    public string Fees { get; set; } = "none";

    /// <summary>
    /// Access constraints information
    /// </summary>
    public string AccessConstraints { get; set; } = "none";

    /// <summary>
    /// Provider/organization name
    /// </summary>
    public string ProviderName { get; set; } = "WFS3Words";

    /// <summary>
    /// Provider website URL
    /// </summary>
    public string ProviderSite { get; set; } = string.Empty;

    /// <summary>
    /// Contact person name
    /// </summary>
    public string ContactPerson { get; set; } = string.Empty;

    /// <summary>
    /// Contact email address
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of features to return in a single GetFeature request
    /// </summary>
    public int MaxFeatures { get; set; } = 1000;

    /// <summary>
    /// Default grid density (points per degree) when generating coordinate grid
    /// </summary>
    public double DefaultGridDensity { get; set; } = 0.01; // ~1.1 km at equator

    /// <summary>
    /// Enable response caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache duration in minutes for GetCapabilities responses
    /// </summary>
    public int CapabilitiesCacheDurationMinutes { get; set; } = 60;
}
