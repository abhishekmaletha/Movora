using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Core.HttpClient.Models;

namespace Core.HttpClient.Extensions;

/// <summary>
/// Extension methods for HttpClient to simplify common HTTP operations
/// </summary>
public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    #region GET Methods

    /// <summary>
    /// Sends a GET request and returns the response as a string
    /// </summary>
    /// <param name="httpClient">The HttpClient instance</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response wrapped in ApiResponse</returns>
    public static async Task<ApiResponse<string>> GetAsync(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(requestUri, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ApiResponse<string>(
                data: content,
                statusCode: response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                reasonPhrase: response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.FromException(ex);
        }
    }

    /// <summary>
    /// Sends a GET request and deserializes the response to the specified type
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to</typeparam>
    /// <param name="httpClient">The HttpClient instance</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="jsonOptions">Optional JSON serializer options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response wrapped in ApiResponse</returns>
    public static async Task<ApiResponse<T>> GetAsync<T>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(requestUri, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ApiResponse<T>(
                    data: default,
                    statusCode: response.StatusCode,
                    isSuccess: false,
                    reasonPhrase: response.ReasonPhrase,
                    errorMessage: errorContent);
            }

            var options = jsonOptions ?? DefaultJsonOptions;
            var data = await response.Content.ReadFromJsonAsync<T>(options, cancellationToken);

            return new ApiResponse<T>(
                data: data,
                statusCode: response.StatusCode,
                isSuccess: true,
                reasonPhrase: response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.FromException(ex);
        }
    }

    #endregion

    #region POST Methods

    /// <summary>
    /// Sends a POST request with no content
    /// </summary>
    /// <param name="httpClient">The HttpClient instance</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response wrapped in ApiResponse</returns>
    public static async Task<ApiResponse<string>> PostAsync(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsync(requestUri, null, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ApiResponse<string>(
                data: content,
                statusCode: response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                reasonPhrase: response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.FromException(ex);
        }
    }

    /// <summary>
    /// Sends a POST request with JSON content
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object</typeparam>
    /// <param name="httpClient">The HttpClient instance</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="content">The request content</param>
    /// <param name="jsonOptions">Optional JSON serializer options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response wrapped in ApiResponse</returns>
    public static async Task<ApiResponse<string>> PostAsync<TRequest>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        TRequest content,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = jsonOptions ?? DefaultJsonOptions;
            var response = await httpClient.PostAsJsonAsync(requestUri, content, options, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ApiResponse<string>(
                data: responseContent,
                statusCode: response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                reasonPhrase: response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.FromException(ex);
        }
    }

    /// <summary>
    /// Sends a POST request with JSON content and deserializes the response
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object</typeparam>
    /// <typeparam name="TResponse">The type of the response object</typeparam>
    /// <param name="httpClient">The HttpClient instance</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="content">The request content</param>
    /// <param name="jsonOptions">Optional JSON serializer options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response wrapped in ApiResponse</returns>
    public static async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        TRequest content,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = jsonOptions ?? DefaultJsonOptions;
            var response = await httpClient.PostAsJsonAsync(requestUri, content, options, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ApiResponse<TResponse>(
                    data: default,
                    statusCode: response.StatusCode,
                    isSuccess: false,
                    reasonPhrase: response.ReasonPhrase,
                    errorMessage: errorContent);
            }

            var data = await response.Content.ReadFromJsonAsync<TResponse>(options, cancellationToken);

            return new ApiResponse<TResponse>(
                data: data,
                statusCode: response.StatusCode,
                isSuccess: true,
                reasonPhrase: response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return ApiResponse<TResponse>.FromException(ex);
        }
    }

    #endregion

    #region PUT Methods

    /// <summary>
    /// Sends a PUT request with JSON content
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object</typeparam>
    /// <param name="httpClient">The HttpClient instance</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="content">The request content</param>
    /// <param name="jsonOptions">Optional JSON serializer options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response wrapped in ApiResponse</returns>
    public static async Task<ApiResponse<string>> PutAsync<TRequest>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        TRequest content,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = jsonOptions ?? DefaultJsonOptions;
            var response = await httpClient.PutAsJsonAsync(requestUri, content, options, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ApiResponse<string>(
                data: responseContent,
                statusCode: response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                reasonPhrase: response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.FromException(ex);
        }
    }

    /// <summary>
    /// Sends a PUT request with JSON content and deserializes the response
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object</typeparam>
    /// <typeparam name="TResponse">The type of the response object</typeparam>
    /// <param name="httpClient">The HttpClient instance</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="content">The request content</param>
    /// <param name="jsonOptions">Optional JSON serializer options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response wrapped in ApiResponse</returns>
    public static async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        TRequest content,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = jsonOptions ?? DefaultJsonOptions;
            var response = await httpClient.PutAsJsonAsync(requestUri, content, options, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ApiResponse<TResponse>(
                    data: default,
                    statusCode: response.StatusCode,
                    isSuccess: false,
                    reasonPhrase: response.ReasonPhrase,
                    errorMessage: errorContent);
            }

            var data = await response.Content.ReadFromJsonAsync<TResponse>(options, cancellationToken);

            return new ApiResponse<TResponse>(
                data: data,
                statusCode: response.StatusCode,
                isSuccess: true,
                reasonPhrase: response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return ApiResponse<TResponse>.FromException(ex);
        }
    }

    #endregion

    #region PATCH Methods

    /// <summary>
    /// Sends a PATCH request with JSON content
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object</typeparam>
    /// <param name="httpClient">The HttpClient instance</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="content">The request content</param>
    /// <param name="jsonOptions">Optional JSON serializer options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response wrapped in ApiResponse</returns>
    public static async Task<ApiResponse<string>> PatchAsync<TRequest>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        TRequest content,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = jsonOptions ?? DefaultJsonOptions;
            var json = JsonSerializer.Serialize(content, options);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PatchAsync(requestUri, httpContent, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ApiResponse<string>(
                data: responseContent,
                statusCode: response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                reasonPhrase: response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.FromException(ex);
        }
    }

    /// <summary>
    /// Sends a PATCH request with JSON content and deserializes the response
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object</typeparam>
    /// <typeparam name="TResponse">The type of the response object</typeparam>
    /// <param name="httpClient">The HttpClient instance</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="content">The request content</param>
    /// <param name="jsonOptions">Optional JSON serializer options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response wrapped in ApiResponse</returns>
    public static async Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        TRequest content,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = jsonOptions ?? DefaultJsonOptions;
            var json = JsonSerializer.Serialize(content, options);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PatchAsync(requestUri, httpContent, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ApiResponse<TResponse>(
                    data: default,
                    statusCode: response.StatusCode,
                    isSuccess: false,
                    reasonPhrase: response.ReasonPhrase,
                    errorMessage: errorContent);
            }

            var data = await response.Content.ReadFromJsonAsync<TResponse>(options, cancellationToken);

            return new ApiResponse<TResponse>(
                data: data,
                statusCode: response.StatusCode,
                isSuccess: true,
                reasonPhrase: response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return ApiResponse<TResponse>.FromException(ex);
        }
    }

    #endregion

    #region DELETE Methods

    /// <summary>
    /// Sends a DELETE request
    /// </summary>
    /// <param name="httpClient">The HttpClient instance</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response wrapped in ApiResponse</returns>
    public static async Task<ApiResponse<string>> DeleteAsync(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync(requestUri, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ApiResponse<string>(
                data: content,
                statusCode: response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                reasonPhrase: response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.FromException(ex);
        }
    }

    /// <summary>
    /// Sends a DELETE request and deserializes the response
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to</typeparam>
    /// <param name="httpClient">The HttpClient instance</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="jsonOptions">Optional JSON serializer options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response wrapped in ApiResponse</returns>
    public static async Task<ApiResponse<T>> DeleteAsync<T>(
        this System.Net.Http.HttpClient httpClient,
        string requestUri,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync(requestUri, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ApiResponse<T>(
                    data: default,
                    statusCode: response.StatusCode,
                    isSuccess: false,
                    reasonPhrase: response.ReasonPhrase,
                    errorMessage: errorContent);
            }

            var options = jsonOptions ?? DefaultJsonOptions;
            var data = await response.Content.ReadFromJsonAsync<T>(options, cancellationToken);

            return new ApiResponse<T>(
                data: data,
                statusCode: response.StatusCode,
                isSuccess: true,
                reasonPhrase: response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.FromException(ex);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Checks if the response status code indicates success
    /// </summary>
    /// <param name="response">The API response</param>
    /// <returns>True if the response is successful</returns>
    public static bool IsSuccess<T>(this ApiResponse<T> response)
    {
        return response.IsSuccess;
    }

    /// <summary>
    /// Gets the error message from the response if it failed
    /// </summary>
    /// <param name="response">The API response</param>
    /// <returns>Error message or null if successful</returns>
    public static string? GetErrorMessage<T>(this ApiResponse<T> response)
    {
        return response.IsSuccess ? null : response.ErrorMessage ?? response.ReasonPhrase;
    }

    #endregion
}
