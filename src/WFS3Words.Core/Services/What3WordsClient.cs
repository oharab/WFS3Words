using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WFS3Words.Core.Configuration;
using WFS3Words.Core.Exceptions;
using WFS3Words.Core.Interfaces;
using WFS3Words.Core.Models;

namespace WFS3Words.Core.Services;

/// <summary>
/// HTTP client for the What3Words API.
/// </summary>
public class What3WordsClient : IWhat3WordsClient
{
    private readonly HttpClient _httpClient;
    private readonly What3WordsOptions _options;
    private readonly ILogger<What3WordsClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public What3WordsClient(
        HttpClient httpClient,
        IOptions<What3WordsOptions> options,
        ILogger<What3WordsClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<What3WordsLocation> ConvertToWordsAsync(
        GeoCoordinate coordinate,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        if (!coordinate.IsValid())
        {
            throw new ArgumentException("Invalid coordinate", nameof(coordinate));
        }

        try
        {
            var url = $"convert-to-3wa?coordinates={coordinate.Latitude},{coordinate.Longitude}&language={language}";

            _logger.LogDebug(
                "Requesting What3Words API: GET {Url} for coordinate ({Latitude}, {Longitude})",
                url,
                coordinate.Latitude,
                coordinate.Longitude);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "What3Words API returned {StatusCode}: {Error}",
                    response.StatusCode,
                    errorContent);

                throw new What3WordsException(
                    $"What3Words API request failed with status {response.StatusCode}",
                    (int)response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<What3WordsApiResponse>(content, _jsonOptions);

            if (apiResponse == null || apiResponse.Coordinates == null || apiResponse.Square == null)
            {
                throw new What3WordsException("Invalid response from What3Words API");
            }

            var location = MapToLocation(apiResponse, language);

            _logger.LogDebug(
                "What3Words API response: '{Words}' for coordinate ({Latitude}, {Longitude})",
                location.Words,
                coordinate.Latitude,
                coordinate.Longitude);

            return location;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling What3Words API");
            throw new What3WordsException("Failed to connect to What3Words API", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "What3Words API request timed out");
            throw new What3WordsException("What3Words API request timed out", ex);
        }
        catch (What3WordsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling What3Words API");
            throw new What3WordsException("Unexpected error calling What3Words API", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try a simple request to verify API connectivity
            var testCoordinate = new GeoCoordinate(51.520847, -0.195521); // London
            var response = await _httpClient.GetAsync(
                $"convert-to-3wa?coordinates={testCoordinate.Latitude},{testCoordinate.Longitude}",
                cancellationToken);

            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for What3Words API");
            return false;
        }
    }

    private static What3WordsLocation MapToLocation(What3WordsApiResponse response, string language)
    {
        var coordinates = new GeoCoordinate(
            response.Coordinates!.Lat,
            response.Coordinates.Lng);

        var square = new BoundingBox(
            response.Square!.Southwest!.Lat,
            response.Square.Southwest.Lng,
            response.Square.Northeast!.Lat,
            response.Square.Northeast.Lng);

        return new What3WordsLocation(
            Words: response.Words ?? string.Empty,
            Coordinates: coordinates,
            Country: response.Country ?? string.Empty,
            Square: square,
            NearestPlace: response.NearestPlace,
            Language: language,
            Map: response.Map);
    }
}
