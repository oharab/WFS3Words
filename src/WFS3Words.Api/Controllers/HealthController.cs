using Microsoft.AspNetCore.Mvc;
using WFS3Words.Core.Interfaces;

namespace WFS3Words.Api.Controllers;

/// <summary>
/// Health check endpoint.
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IWhat3WordsClient _what3WordsClient;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IWhat3WordsClient what3WordsClient,
        ILogger<HealthController> logger)
    {
        _what3WordsClient = what3WordsClient;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint to verify service and What3Words API connectivity.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckHealth()
    {
        _logger.LogDebug("Health check requested");

        var isHealthy = await _what3WordsClient.IsHealthyAsync();

        if (isHealthy)
        {
            _logger.LogInformation("Health check succeeded - service is healthy");
            return Ok(new
            {
                status = "healthy",
                service = "WFS3Words",
                what3words = "connected"
            });
        }
        else
        {
            _logger.LogWarning("Health check failed - What3Words API not accessible");
            return StatusCode(503, new
            {
                status = "unhealthy",
                service = "WFS3Words",
                what3words = "disconnected"
            });
        }
    }
}
