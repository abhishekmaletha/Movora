using System.Net;
using System.Text.Json.Serialization;

namespace Core.HttpClient.Models;

/// <summary>
/// Represents a response from an API call
/// </summary>
/// <typeparam name="T">The type of data returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Creates a new ApiResponse instance
    /// </summary>
    /// <param name="data">The response data</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="isSuccess">Whether the request was successful</param>
    /// <param name="reasonPhrase">HTTP reason phrase</param>
    /// <param name="errorMessage">Error message if any</param>
    public ApiResponse(
        T? data,
        HttpStatusCode statusCode,
        bool isSuccess,
        string? reasonPhrase = null,
        string? errorMessage = null)
    {
        Data = data;
        StatusCode = statusCode;
        IsSuccess = isSuccess;
        ReasonPhrase = reasonPhrase;
        ErrorMessage = errorMessage;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// The response data
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// HTTP reason phrase
    /// </summary>
    public string? ReasonPhrase { get; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Timestamp when the response was created
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Whether the request failed
    /// </summary>
    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the numeric status code
    /// </summary>
    [JsonIgnore]
    public int StatusCodeValue => (int)StatusCode;

    /// <summary>
    /// Creates a successful response
    /// </summary>
    /// <param name="data">The response data</param>
    /// <param name="statusCode">HTTP status code (default: OK)</param>
    /// <returns>Successful API response</returns>
    public static ApiResponse<T> Success(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new ApiResponse<T>(data, statusCode, true);
    }

    /// <summary>
    /// Creates a failed response
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="reasonPhrase">HTTP reason phrase</param>
    /// <returns>Failed API response</returns>
    public static ApiResponse<T> Failure(
        HttpStatusCode statusCode,
        string? errorMessage = null,
        string? reasonPhrase = null)
    {
        return new ApiResponse<T>(default, statusCode, false, reasonPhrase, errorMessage);
    }

    /// <summary>
    /// Creates a response from an exception
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="statusCode">HTTP status code (default: InternalServerError)</param>
    /// <returns>Failed API response</returns>
    public static ApiResponse<T> FromException(
        Exception exception,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        return new ApiResponse<T>(
            data: default,
            statusCode: statusCode,
            isSuccess: false,
            reasonPhrase: "Exception occurred",
            errorMessage: exception.Message);
    }

    /// <summary>
    /// Creates a not found response
    /// </summary>
    /// <param name="message">Optional error message</param>
    /// <returns>Not found API response</returns>
    public static ApiResponse<T> NotFound(string? message = null)
    {
        return new ApiResponse<T>(
            data: default,
            statusCode: HttpStatusCode.NotFound,
            isSuccess: false,
            reasonPhrase: "Not Found",
            errorMessage: message ?? "The requested resource was not found");
    }

    /// <summary>
    /// Creates a bad request response
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>Bad request API response</returns>
    public static ApiResponse<T> BadRequest(string message)
    {
        return new ApiResponse<T>(
            data: default,
            statusCode: HttpStatusCode.BadRequest,
            isSuccess: false,
            reasonPhrase: "Bad Request",
            errorMessage: message);
    }

    /// <summary>
    /// Creates an unauthorized response
    /// </summary>
    /// <param name="message">Optional error message</param>
    /// <returns>Unauthorized API response</returns>
    public static ApiResponse<T> Unauthorized(string? message = null)
    {
        return new ApiResponse<T>(
            data: default,
            statusCode: HttpStatusCode.Unauthorized,
            isSuccess: false,
            reasonPhrase: "Unauthorized",
            errorMessage: message ?? "Authentication is required");
    }

    /// <summary>
    /// Creates a forbidden response
    /// </summary>
    /// <param name="message">Optional error message</param>
    /// <returns>Forbidden API response</returns>
    public static ApiResponse<T> Forbidden(string? message = null)
    {
        return new ApiResponse<T>(
            data: default,
            statusCode: HttpStatusCode.Forbidden,
            isSuccess: false,
            reasonPhrase: "Forbidden",
            errorMessage: message ?? "Access to this resource is forbidden");
    }

    /// <summary>
    /// Creates a server error response
    /// </summary>
    /// <param name="message">Optional error message</param>
    /// <returns>Server error API response</returns>
    public static ApiResponse<T> ServerError(string? message = null)
    {
        return new ApiResponse<T>(
            data: default,
            statusCode: HttpStatusCode.InternalServerError,
            isSuccess: false,
            reasonPhrase: "Internal Server Error",
            errorMessage: message ?? "An internal server error occurred");
    }

    /// <summary>
    /// Creates a service unavailable response
    /// </summary>
    /// <param name="message">Optional error message</param>
    /// <returns>Service unavailable API response</returns>
    public static ApiResponse<T> ServiceUnavailable(string? message = null)
    {
        return new ApiResponse<T>(
            data: default,
            statusCode: HttpStatusCode.ServiceUnavailable,
            isSuccess: false,
            reasonPhrase: "Service Unavailable",
            errorMessage: message ?? "The service is temporarily unavailable");
    }

    /// <summary>
    /// Returns a string representation of the API response
    /// </summary>
    public override string ToString()
    {
        var status = IsSuccess ? "Success" : "Failure";
        return $"{status}: {StatusCode} ({StatusCodeValue}) - {ReasonPhrase}";
    }
}

/// <summary>
/// Non-generic version of ApiResponse for operations that don't return data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public ApiResponse(
        HttpStatusCode statusCode,
        bool isSuccess,
        string? reasonPhrase = null,
        string? errorMessage = null)
        : base(null, statusCode, isSuccess, reasonPhrase, errorMessage)
    {
    }

    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    /// <param name="statusCode">HTTP status code (default: OK)</param>
    /// <returns>Successful API response</returns>
    public static ApiResponse Success(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new ApiResponse(statusCode, true);
    }

    /// <summary>
    /// Creates a failed response without data
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="reasonPhrase">HTTP reason phrase</param>
    /// <returns>Failed API response</returns>
    public static ApiResponse Failure(
        HttpStatusCode statusCode,
        string? errorMessage = null,
        string? reasonPhrase = null)
    {
        return new ApiResponse(statusCode, false, reasonPhrase, errorMessage);
    }

    /// <summary>
    /// Creates a response from an exception
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="statusCode">HTTP status code (default: InternalServerError)</param>
    /// <returns>Failed API response</returns>
    public static ApiResponse FromException(
        Exception exception,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        return new ApiResponse(statusCode, false, "Exception occurred", exception.Message);
    }
}
