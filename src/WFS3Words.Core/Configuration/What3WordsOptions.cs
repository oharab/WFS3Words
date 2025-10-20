namespace WFS3Words.Core.Configuration;

/// <summary>
/// Configuration options for the What3Words API client.
/// </summary>
public class What3WordsOptions
{
    /// <summary>
    /// The section name in appsettings.json
    /// </summary>
    public const string SectionName = "What3Words";

    /// <summary>
    /// What3Words API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the What3Words API (default: https://api.what3words.com/v3/)
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.what3words.com/v3/";

    /// <summary>
    /// Request timeout in seconds (default: 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Default language for 3-word addresses (default: en)
    /// </summary>
    public string DefaultLanguage { get; set; } = "en";
}
