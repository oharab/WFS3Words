using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WFS3Words.Core.Configuration;
using WFS3Words.Core.Exceptions;
using WFS3Words.Core.Models;
using WFS3Words.Core.Services;

namespace WFS3Words.Tests.Unit.Services;

public class What3WordsClientTests
{
    private readonly Mock<ILogger<What3WordsClient>> _mockLogger;
    private readonly What3WordsOptions _options;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;

    public What3WordsClientTests()
    {
        _mockLogger = new Mock<ILogger<What3WordsClient>>();
        _options = new What3WordsOptions
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.what3words.com/v3/",
            TimeoutSeconds = 30,
            DefaultLanguage = "en"
        };
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
    }

    [Fact]
    public async Task ConvertToWordsAsync_ShouldReturnWhat3WordsLocation_WhenApiReturnsSuccess()
    {
        var coordinate = new GeoCoordinate(51.520847, -0.195521);
        var apiResponse = new What3WordsApiResponse
        {
            Words = "filled.count.soap",
            Country = "GB",
            Coordinates = new What3WordsCoordinatesDto { Lat = 51.520847, Lng = -0.195521 },
            Square = new What3WordsSquareDto
            {
                Southwest = new What3WordsCoordinatesDto { Lat = 51.520833, Lng = -0.195543 },
                Northeast = new What3WordsCoordinatesDto { Lat = 51.520861, Lng = -0.195499 }
            },
            NearestPlace = "Bayswater, London",
            Language = "en",
            Map = "https://w3w.co/filled.count.soap"
        };

        var jsonResponse = JsonSerializer.Serialize(apiResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        var client = new What3WordsClient(
            httpClient,
            Options.Create(_options),
            _mockLogger.Object);

        var result = await client.ConvertToWordsAsync(coordinate);

        Assert.NotNull(result);
        Assert.Equal("filled.count.soap", result.Words);
        Assert.Equal("GB", result.Country);
        Assert.Equal(51.520847, result.Coordinates.Latitude);
        Assert.Equal(-0.195521, result.Coordinates.Longitude);
        Assert.Equal("Bayswater, London", result.NearestPlace);
        Assert.Equal("en", result.Language);
    }

    [Fact]
    public async Task ConvertToWordsAsync_ShouldThrowWhat3WordsException_WhenApiReturns404()
    {
        var coordinate = new GeoCoordinate(51.520847, -0.195521);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Not Found")
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        var client = new What3WordsClient(
            httpClient,
            Options.Create(_options),
            _mockLogger.Object);

        var exception = await Assert.ThrowsAsync<What3WordsException>(
            () => client.ConvertToWordsAsync(coordinate));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task ConvertToWordsAsync_ShouldThrowArgumentException_WhenCoordinateIsInvalid()
    {
        var invalidCoordinate = new GeoCoordinate(91, 0); // Invalid latitude

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        var client = new What3WordsClient(
            httpClient,
            Options.Create(_options),
            _mockLogger.Object);

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.ConvertToWordsAsync(invalidCoordinate));
    }

    [Fact]
    public async Task ConvertToWordsAsync_ShouldThrowWhat3WordsException_WhenHttpRequestFails()
    {
        var coordinate = new GeoCoordinate(51.520847, -0.195521);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        var client = new What3WordsClient(
            httpClient,
            Options.Create(_options),
            _mockLogger.Object);

        var exception = await Assert.ThrowsAsync<What3WordsException>(
            () => client.ConvertToWordsAsync(coordinate));

        Assert.Contains("Failed to connect", exception.Message);
    }

    [Fact]
    public async Task ConvertToWordsAsync_ShouldThrowWhat3WordsException_WhenResponseIsInvalid()
    {
        var coordinate = new GeoCoordinate(51.520847, -0.195521);
        var invalidJson = "{\"invalid\": \"response\"}";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(invalidJson)
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        var client = new What3WordsClient(
            httpClient,
            Options.Create(_options),
            _mockLogger.Object);

        var exception = await Assert.ThrowsAsync<What3WordsException>(
            () => client.ConvertToWordsAsync(coordinate));

        Assert.Contains("Invalid response", exception.Message);
    }

    [Fact]
    public async Task IsHealthyAsync_ShouldReturnTrue_WhenApiIsAccessible()
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        var client = new What3WordsClient(
            httpClient,
            Options.Create(_options),
            _mockLogger.Object);

        var result = await client.IsHealthyAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsHealthyAsync_ShouldReturnFalse_WhenApiIsNotAccessible()
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        var client = new What3WordsClient(
            httpClient,
            Options.Create(_options),
            _mockLogger.Object);

        var result = await client.IsHealthyAsync();

        Assert.False(result);
    }
}
