using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WFS3Words.Core.Configuration;
using WFS3Words.Core.Interfaces;
using WFS3Words.Core.Models;
using WFS3Words.Core.Services;

namespace WFS3Words.Api.Controllers;

/// <summary>
/// WFS 1.0/2.0 endpoint controller.
/// </summary>
[ApiController]
[Route("wfs")]
public class WfsController : ControllerBase
{
    private readonly IWhat3WordsClient _what3WordsClient;
    private readonly ICoordinateGridService _gridService;
    private readonly IWfsCapabilitiesFormatter _capabilitiesFormatter;
    private readonly IWfsFeatureFormatter _featureFormatter;
    private readonly WfsQueryParser _queryParser;
    private readonly WfsOptions _wfsOptions;
    private readonly ILogger<WfsController> _logger;

    public WfsController(
        IWhat3WordsClient what3WordsClient,
        ICoordinateGridService gridService,
        IWfsCapabilitiesFormatter capabilitiesFormatter,
        IWfsFeatureFormatter featureFormatter,
        WfsQueryParser queryParser,
        IOptions<WfsOptions> wfsOptions,
        ILogger<WfsController> logger)
    {
        _what3WordsClient = what3WordsClient;
        _gridService = gridService;
        _capabilitiesFormatter = capabilitiesFormatter;
        _featureFormatter = featureFormatter;
        _queryParser = queryParser;
        _wfsOptions = wfsOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Main WFS endpoint - handles all WFS requests via query parameters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> HandleRequest()
    {
        var queryDict = Request.Query.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value);
        var wfsRequest = _queryParser.Parse(queryDict);

        _logger.LogInformation(
            "WFS {Request} request (version {Version}) from {RemoteIp}",
            wfsRequest.Request ?? "UNKNOWN",
            wfsRequest.Version ?? "not specified",
            Request.HttpContext.Connection.RemoteIpAddress);

        return wfsRequest.Request?.ToUpperInvariant() switch
        {
            "GETCAPABILITIES" => HandleGetCapabilities(wfsRequest),
            "DESCRIBEFEATURETYPE" => HandleDescribeFeatureType(wfsRequest),
            "GETFEATURE" => await HandleGetFeature(wfsRequest),
            _ => BadRequest(new { error = "Invalid or missing REQUEST parameter" })
        };
    }

    private IActionResult HandleGetCapabilities(WfsRequest request)
    {
        var version = request.Version ?? "2.0.0";
        var serviceUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";

        _logger.LogDebug("Generating GetCapabilities response for version {Version}", version);

        var xml = _capabilitiesFormatter.GenerateCapabilities(version, serviceUrl);

        _logger.LogDebug("GetCapabilities response generated ({Bytes} bytes)", xml.Length);

        return Content(xml, "application/xml");
    }

    private IActionResult HandleDescribeFeatureType(WfsRequest request)
    {
        var version = request.Version ?? "2.0.0";

        _logger.LogDebug(
            "DescribeFeatureType request: version={Version}, outputFormat={OutputFormat}",
            version,
            request.OutputFormat ?? "default");

        // Validate outputFormat if provided
        var outputFormat = request.OutputFormat?.ToUpperInvariant();
        if (!string.IsNullOrEmpty(outputFormat))
        {
            // Supported formats: XMLSCHEMA, text/xml variants
            var supportedFormats = new[] { "XMLSCHEMA", "TEXT/XML", "APPLICATION/XML" };
            var isSupported = supportedFormats.Any(f => outputFormat.Contains(f));

            if (!isSupported)
            {
                _logger.LogWarning("Unsupported outputFormat requested: {OutputFormat}", request.OutputFormat);
                return BadRequest(new
                {
                    error = $"OutputFormat '{request.OutputFormat}' is not supported. Supported formats: XMLSCHEMA, text/xml; subtype=gml/3.1.1, text/xml; subtype=gml/3.2.0"
                });
            }
        }

        var xml = _featureFormatter.GenerateFeatureTypeDescription(version);

        return Content(xml, "application/xml");
    }

    private async Task<IActionResult> HandleGetFeature(WfsRequest request)
    {
        if (request.BBox == null)
        {
            _logger.LogWarning("GetFeature request missing required BBOX parameter");
            return BadRequest(new { error = "BBOX parameter is required for GetFeature requests" });
        }

        // Validate BBOX has proper min/max ordering
        if (request.BBox.MinLongitude >= request.BBox.MaxLongitude ||
            request.BBox.MinLatitude >= request.BBox.MaxLatitude)
        {
            _logger.LogWarning("GetFeature request with invalid BBOX (min >= max): {BBox}", request.BBox);
            return BadRequest(new { error = "Invalid BBOX parameter: minimum values must be less than maximum values" });
        }

        // Note: We don't validate coordinate ranges here because the BBOX might be in a projected CRS
        // (e.g., EPSG:27700 uses meters, not degrees). Coordinate transformation will handle this.

        _logger.LogInformation(
            "GetFeature request: BBOX=[{MinLon},{MinLat},{MaxLon},{MaxLat}], SRS={Srs}, OutputFormat={OutputFormat}",
            request.BBox.MinLongitude,
            request.BBox.MinLatitude,
            request.BBox.MaxLongitude,
            request.BBox.MaxLatitude,
            request.SrsName ?? "EPSG:4326",
            request.OutputFormat ?? "GML");

        try
        {
            // Check if coordinates are in WGS84 range (What3Words only supports WGS84)
            if (!request.BBox.IsValid())
            {
                _logger.LogWarning(
                    "GetFeature request with non-WGS84 coordinates. CRS transformation not yet supported. BBOX: {BBox}, SRS: {Srs}",
                    request.BBox,
                    request.SrsName);
                return BadRequest(new
                {
                    error = "Coordinates must be in WGS84 (EPSG:4326). " +
                           "The requested CRS is not currently supported. " +
                           $"Requested SRS: {request.SrsName ?? "not specified"}. " +
                           "Please provide coordinates in decimal degrees (latitude/longitude)."
                });
            }

            var maxFeatures = request.MaxFeatures ?? _wfsOptions.MaxFeatures;
            var gridDensity = _wfsOptions.DefaultGridDensity;

            // Generate coordinate grid
            var coordinates = _gridService.GenerateGrid(
                request.BBox,
                gridDensity,
                maxFeatures).ToList();

            _logger.LogDebug(
                "Generated {Count} coordinates for BBOX [{MinLon},{MinLat},{MaxLon},{MaxLat}]",
                coordinates.Count,
                request.BBox.MinLongitude,
                request.BBox.MinLatitude,
                request.BBox.MaxLongitude,
                request.BBox.MaxLatitude);

            // Fetch What3Words data for each coordinate
            var features = new List<WfsFeature>();
            var featureId = 1;

            foreach (var coordinate in coordinates)
            {
                try
                {
                    var location = await _what3WordsClient.ConvertToWordsAsync(coordinate);
                    var feature = new WfsFeature(
                        $"location.{featureId}",
                        coordinate,
                        location);

                    features.Add(feature);
                    featureId++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to get What3Words data for coordinate {Coordinate}",
                        coordinate);
                    // Continue with next coordinate
                }
            }

            var collection = new WfsFeatureCollection(
                features,
                features.Count,
                request.BBox);

            _logger.LogInformation(
                "Returning {Count} features for GetFeature request",
                features.Count);

            // Determine output format
            var outputFormat = request.OutputFormat?.ToLowerInvariant();
            if (outputFormat?.Contains("json") == true || outputFormat?.Contains("geojson") == true)
            {
                var json = _featureFormatter.FormatAsGeoJson(collection, request.SrsName);
                return Content(json, "application/geo+json");
            }
            else
            {
                var version = request.Version ?? "2.0.0";
                var xml = _featureFormatter.FormatAsGml(collection, version, request.SrsName);
                return Content(xml, "application/gml+xml");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetFeature request");
            return StatusCode(500, new { error = "Internal server error processing WFS request" });
        }
    }
}
