using Microsoft.Extensions.Primitives;
using WFS3Words.Core.Models;

namespace WFS3Words.Core.Services;

/// <summary>
/// Parser for WFS query string parameters.
/// </summary>
public class WfsQueryParser
{
    /// <summary>
    /// Parses WFS query parameters from a dictionary.
    /// </summary>
    /// <param name="queryParams">Query parameter dictionary (case-insensitive)</param>
    /// <returns>Parsed WFS request</returns>
    public WfsRequest Parse(IDictionary<string, StringValues> queryParams)
    {
        var caseInsensitiveParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in queryParams)
        {
            caseInsensitiveParams[kvp.Key] = kvp.Value.ToString();
        }

        return new WfsRequest
        {
            Service = GetValue(caseInsensitiveParams, "service"),
            Version = GetValue(caseInsensitiveParams, "version"),
            Request = GetValue(caseInsensitiveParams, "request"),
            TypeName = GetValue(caseInsensitiveParams, "typename") ??
                      GetValue(caseInsensitiveParams, "typenames"),
            BBox = ParseBoundingBox(GetValue(caseInsensitiveParams, "bbox")),
            MaxFeatures = ParseInt(GetValue(caseInsensitiveParams, "maxfeatures") ??
                                  GetValue(caseInsensitiveParams, "count")),
            OutputFormat = GetValue(caseInsensitiveParams, "outputformat"),
            SrsName = GetValue(caseInsensitiveParams, "srsname") ??
                     GetValue(caseInsensitiveParams, "srs")
        };
    }

    private string? GetValue(Dictionary<string, string> parameters, string key)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private BoundingBox? ParseBoundingBox(string? bboxString)
    {
        if (string.IsNullOrWhiteSpace(bboxString))
            return null;

        return BoundingBox.Parse(bboxString);
    }

    private int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value, out var result) ? result : null;
    }
}
