using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WFS3Words.Api.Middleware;
using Xunit;

namespace WFS3Words.Tests.Unit.Middleware;

public class RequestLoggingMiddlewareTests
{
    private readonly Mock<ILogger<RequestLoggingMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNext;

    public RequestLoggingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogRequestStart_WhenRequestReceived()
    {
        // Arrange
        var middleware = new RequestLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CreateHttpContext("GET", "/test");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogRequestCompletion_WhenRequestCompletes()
    {
        // Arrange
        var middleware = new RequestLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CreateHttpContext("GET", "/test");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogElapsedTime_WhenRequestCompletes()
    {
        // Arrange
        var middleware = new RequestLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CreateHttpContext("GET", "/test");

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .Returns(async () =>
            {
                await Task.Delay(10);
            });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogPostBody_WhenRequestIsPost()
    {
        // Arrange
        var postBody = "<xml>test content</xml>";
        var middleware = new RequestLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CreateHttpContext("POST", "/wfs", postBody);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(postBody)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotLogPostBody_WhenRequestIsGet()
    {
        // Arrange
        var middleware = new RequestLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CreateHttpContext("GET", "/test");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("POST Body")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldTruncateLargePostBody_WhenBodyExceeds4000Bytes()
    {
        // Arrange
        var largeBody = new string('x', 5000);
        var middleware = new RequestLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CreateHttpContext("POST", "/wfs", largeBody);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("truncated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextDelegate_WhenInvoked()
    {
        // Arrange
        var middleware = new RequestLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CreateHttpContext("GET", "/test");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogStatusCode_WhenRequestCompletes()
    {
        // Arrange
        var middleware = new RequestLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CreateHttpContext("GET", "/test");
        context.Response.StatusCode = 200;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("200")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static DefaultHttpContext CreateHttpContext(string method, string path, string? body = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString("?test=value");

        if (body != null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
            context.Request.ContentType = "application/xml";
        }

        context.Response.Body = new MemoryStream();

        return context;
    }
}
