namespace WFS3Words.Core.Exceptions;

/// <summary>
/// Exception thrown when What3Words API operations fail.
/// </summary>
public class What3WordsException : Exception
{
    /// <summary>
    /// HTTP status code from the API response (if applicable).
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Error code from the What3Words API (if available).
    /// </summary>
    public string? ErrorCode { get; }

    public What3WordsException(string message)
        : base(message)
    {
    }

    public What3WordsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public What3WordsException(string message, int statusCode, string? errorCode = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
