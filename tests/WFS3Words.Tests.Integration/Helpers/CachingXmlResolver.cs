using System.Xml;

namespace WFS3Words.Tests.Integration.Helpers;

/// <summary>
/// Custom XmlResolver that caches downloaded schemas locally for faster validation.
/// Downloads OGC schemas on first use and caches them in the Schemas directory.
/// </summary>
public class CachingXmlResolver : XmlUrlResolver
{
    private readonly string _cacheDirectory;
    private readonly HttpClient _httpClient;

    public CachingXmlResolver(string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Ensure cache directory exists
        Directory.CreateDirectory(_cacheDirectory);
    }

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        if (absoluteUri.Scheme != "http" && absoluteUri.Scheme != "https")
        {
            return base.GetEntity(absoluteUri, role, ofObjectToReturn);
        }

        try
        {
            // Create a cache file path based on the URI
            var cacheFileName = GetCacheFileName(absoluteUri);
            var cacheFilePath = Path.Combine(_cacheDirectory, cacheFileName);

            // If cached, return from cache
            if (File.Exists(cacheFilePath))
            {
                return new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            // Download and cache
            var content = _httpClient.GetStringAsync(absoluteUri).GetAwaiter().GetResult();
            File.WriteAllText(cacheFilePath, content);

            return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        }
        catch (Exception ex)
        {
            // Fallback to base resolver
            Console.WriteLine($"Warning: Failed to download/cache schema from {absoluteUri}: {ex.Message}");
            return base.GetEntity(absoluteUri, role, ofObjectToReturn);
        }
    }

    private string GetCacheFileName(Uri uri)
    {
        // Create a safe file name from the URI
        // e.g., http://schemas.opengis.net/gml/3.2.1/gml.xsd -> schemas.opengis.net_gml_3.2.1_gml.xsd
        var path = uri.Host + uri.AbsolutePath;
        var safeFileName = path.Replace("/", "_").Replace("\\", "_").Replace(":", "_");

        // Ensure it has .xsd extension
        if (!safeFileName.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase))
        {
            safeFileName += ".xsd";
        }

        return safeFileName;
    }
}
