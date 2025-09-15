using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Movora.Domain.FlexiSearch;

namespace Movora.Infrastructure.FlexiSearch;

/// <summary>
/// TMDb API client implementation with rate limiting and error handling
/// </summary>
internal sealed class TmdbClient : ITmdbClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TmdbClient> _logger;
    private readonly string _apiKey;
    private readonly SemaphoreSlim _rateLimitSemaphore;

    public TmdbClient(HttpClient httpClient, IConfiguration configuration, ILogger<TmdbClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["TMDb:ApiKey"] ?? throw new InvalidOperationException("TMDb:ApiKey not configured");
        _httpClient.BaseAddress = new Uri("https://api.themoviedb.org/3/");
        _rateLimitSemaphore = new SemaphoreSlim(40, 40); // TMDb allows 40 requests per 10 seconds
    }

    public async Task<TmdbMultiResult> SearchMultiAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty", nameof(query));

        var endpoint = $"search/multi?api_key={_apiKey}&query={Uri.EscapeDataString(query)}";
        
        try
        {
            var response = await ExecuteWithRateLimitAsync<TmdbMultiSearchApiResponse>(endpoint, ct);
            
            return new TmdbMultiResult(
                response.Results?.Select(r => new TmdbMultiItem(
                    r.Id,
                    r.MediaType ?? "unknown",
                    r.Name,
                    r.Title,
                    r.Overview,
                    r.VoteAverage,
                    r.ReleaseDate,
                    r.FirstAirDate,
                    r.PosterPath
                )).ToList() ?? new List<TmdbMultiItem>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching TMDb multi for query: {Query}", query);
            return new TmdbMultiResult(new List<TmdbMultiItem>());
        }
    }

    public async Task<TmdbDiscoverResult> DiscoverAsync(DiscoverQuery query, CancellationToken ct = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var endpoint = BuildDiscoverEndpoint(query);
        
        try
        {
            var response = await ExecuteWithRateLimitAsync<TmdbDiscoverApiResponse>(endpoint, ct);
            
            return new TmdbDiscoverResult(
                response.Results?.Select(r => new TmdbDiscoverItem(
                    r.Id,
                    r.Name,
                    r.Title,
                    r.Overview,
                    r.VoteAverage,
                    r.ReleaseDate,
                    r.FirstAirDate,
                    r.PosterPath,
                    r.GenreIds?.ToList() ?? new List<int>()
                )).ToList() ?? new List<TmdbDiscoverItem>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering TMDb content with query: {@Query}", query);
            return new TmdbDiscoverResult(new List<TmdbDiscoverItem>());
        }
    }

    public async Task<TmdbTitleResult?> FindExactTitleAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        try
        {
            var multiResult = await SearchMultiAsync(title, ct);
            
            // Look for exact case-insensitive matches
            var exactMatch = multiResult.Results.FirstOrDefault(r =>
                string.Equals(r.Name, title, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.Title, title, StringComparison.OrdinalIgnoreCase));

            if (exactMatch == null)
                return null;

            var year = ExtractYear(exactMatch.ReleaseDate ?? exactMatch.FirstAirDate);
            
            return new TmdbTitleResult(
                exactMatch.Id,
                exactMatch.MediaType,
                exactMatch.Name ?? exactMatch.Title ?? "Unknown",
                exactMatch.Overview,
                exactMatch.VoteAverage,
                year,
                exactMatch.PosterPath
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding exact title match for: {Title}", title);
            return null;
        }
    }

    public async Task<TmdbRecommendationsResult> GetRecommendationsAsync(string mediaType, int tmdbId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
            throw new ArgumentException("Media type cannot be null or empty", nameof(mediaType));

        var endpoint = $"{mediaType}/{tmdbId}/recommendations?api_key={_apiKey}";
        
        try
        {
            var response = await ExecuteWithRateLimitAsync<TmdbRecommendationsApiResponse>(endpoint, ct);
            
            return new TmdbRecommendationsResult(
                response.Results?.Select(r => new TmdbRecommendationItem(
                    r.Id,
                    r.Name,
                    r.Title,
                    r.Overview,
                    r.VoteAverage,
                    r.ReleaseDate,
                    r.FirstAirDate,
                    r.PosterPath
                )).ToList() ?? new List<TmdbRecommendationItem>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for {MediaType} {Id}", mediaType, tmdbId);
            return new TmdbRecommendationsResult(new List<TmdbRecommendationItem>());
        }
    }

    public async Task<TmdbSimilarResult> GetSimilarAsync(string mediaType, int tmdbId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
            throw new ArgumentException("Media type cannot be null or empty", nameof(mediaType));

        var endpoint = $"{mediaType}/{tmdbId}/similar?api_key={_apiKey}";
        
        try
        {
            var response = await ExecuteWithRateLimitAsync<TmdbSimilarApiResponse>(endpoint, ct);
            
            return new TmdbSimilarResult(
                response.Results?.Select(r => new TmdbSimilarItem(
                    r.Id,
                    r.Name,
                    r.Title,
                    r.Overview,
                    r.VoteAverage,
                    r.ReleaseDate,
                    r.FirstAirDate,
                    r.PosterPath
                )).ToList() ?? new List<TmdbSimilarItem>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting similar content for {MediaType} {Id}", mediaType, tmdbId);
            return new TmdbSimilarResult(new List<TmdbSimilarItem>());
        }
    }

    public async Task<IReadOnlyDictionary<string, int>> GetGenreMapAsync(string mediaType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
            throw new ArgumentException("Media type cannot be null or empty", nameof(mediaType));

        var endpoint = $"genre/{mediaType}/list?api_key={_apiKey}";
        
        try
        {
            var response = await ExecuteWithRateLimitAsync<TmdbGenreApiResponse>(endpoint, ct);
            
            return response.Genres?.ToDictionary(
                g => g.Name.ToLowerInvariant(),
                g => g.Id,
                StringComparer.OrdinalIgnoreCase
            ) ?? new Dictionary<string, int>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting genre map for media type: {MediaType}", mediaType);
            return new Dictionary<string, int>();
        }
    }

    private async Task<T> ExecuteWithRateLimitAsync<T>(string endpoint, CancellationToken ct) where T : class
    {
        await _rateLimitSemaphore.WaitAsync(ct);
        
        try
        {
            var response = await _httpClient.GetAsync(endpoint, ct);
            
            // Handle rate limiting
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(10);
                _logger.LogWarning("TMDb rate limit hit, waiting {RetryAfter} seconds", retryAfter.TotalSeconds);
                await Task.Delay(retryAfter, ct);
                
                // Retry once
                response = await _httpClient.GetAsync(endpoint, ct);
            }
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            
            return result ?? throw new InvalidOperationException($"Failed to deserialize TMDb response for endpoint: {endpoint}");
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    private string BuildDiscoverEndpoint(DiscoverQuery query)
    {
        var endpoint = $"discover/{query.MediaType}?api_key={_apiKey}";
        
        if (query.GenreIds?.Any() == true)
        {
            endpoint += $"&with_genres={string.Join(",", query.GenreIds)}";
        }

        if (query.KeywordIds?.Any() == true)
        {
            endpoint += $"&with_keywords={string.Join(",", query.KeywordIds)}";
        }

        if (query.WithCast?.Any() == true)
        {
            endpoint += $"&with_cast={string.Join(",", query.WithCast)}";
        }

        if (query.WithCrew?.Any() == true)
        {
            endpoint += $"&with_crew={string.Join(",", query.WithCrew)}";
        }

        if (query.WithPeople?.Any() == true)
        {
            endpoint += $"&with_people={string.Join(",", query.WithPeople)}";
        }
        
        if (query.YearFrom.HasValue)
        {
            var yearParam = query.MediaType == "movie" ? "primary_release_date.gte" : "first_air_date.gte";
            endpoint += $"&{yearParam}={query.YearFrom.Value}-01-01";
        }
        
        if (query.YearTo.HasValue)
        {
            var yearParam = query.MediaType == "movie" ? "primary_release_date.lte" : "first_air_date.lte";
            endpoint += $"&{yearParam}={query.YearTo.Value}-12-31";
        }
        
        if (query.RuntimeLteMinutes.HasValue && query.MediaType == "movie")
        {
            endpoint += $"&with_runtime.lte={query.RuntimeLteMinutes.Value}";
        }

        if (query.RuntimeGteMinutes.HasValue && query.MediaType == "movie")
        {
            endpoint += $"&with_runtime.gte={query.RuntimeGteMinutes.Value}";
        }

        if (!string.IsNullOrEmpty(query.WithOriginalLanguage))
        {
            endpoint += $"&with_original_language={query.WithOriginalLanguage}";
        }

        if (query.VoteCountGte.HasValue)
        {
            endpoint += $"&vote_count.gte={query.VoteCountGte.Value}";
        }
        
        if (!string.IsNullOrEmpty(query.SortBy))
        {
            endpoint += $"&sort_by={query.SortBy}";
        }
        
        return endpoint;
    }

    private static int? ExtractYear(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString) || dateString.Length < 4)
            return null;

        return int.TryParse(dateString[..4], out var year) ? year : null;
    }

    public async Task<TmdbPersonResult> SearchPersonAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty", nameof(query));

        var endpoint = $"search/person?api_key={_apiKey}&query={Uri.EscapeDataString(query)}";
        
        try
        {
            var response = await ExecuteWithRateLimitAsync<TmdbPersonApiResponse>(endpoint, ct);
            
            return new TmdbPersonResult(
                response.Results?.Select(r => new TmdbPersonItem(
                    r.Id,
                    r.Name ?? "Unknown",
                    r.ProfilePath,
                    r.KnownFor?.Select(kf => new TmdbPersonKnownFor(
                        kf.Id,
                        kf.MediaType ?? "unknown",
                        kf.Name,
                        kf.Title,
                        kf.Overview,
                        kf.VoteAverage,
                        kf.ReleaseDate,
                        kf.FirstAirDate,
                        kf.PosterPath
                    )).ToList() ?? new List<TmdbPersonKnownFor>()
                )).ToList() ?? new List<TmdbPersonItem>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching TMDb person for query: {Query}", query);
            return new TmdbPersonResult(new List<TmdbPersonItem>());
        }
    }

    public async Task<TmdbMovieResult> SearchMovieAsync(string query, int? year = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty", nameof(query));

        var endpoint = $"search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(query)}";
        if (year.HasValue)
        {
            endpoint += $"&year={year.Value}";
        }
        
        try
        {
            var response = await ExecuteWithRateLimitAsync<TmdbMovieApiResponse>(endpoint, ct);
            
            return new TmdbMovieResult(
                response.Results?.Select(r => new TmdbMovieItem(
                    r.Id,
                    r.Title ?? "Unknown",
                    r.Overview,
                    r.VoteAverage,
                    r.ReleaseDate,
                    r.PosterPath,
                    r.GenreIds?.ToList() ?? new List<int>()
                )).ToList() ?? new List<TmdbMovieItem>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching TMDb movie for query: {Query}", query);
            return new TmdbMovieResult(new List<TmdbMovieItem>());
        }
    }

    public async Task<TmdbTvResult> SearchTvAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty", nameof(query));

        var endpoint = $"search/tv?api_key={_apiKey}&query={Uri.EscapeDataString(query)}";
        
        try
        {
            var response = await ExecuteWithRateLimitAsync<TmdbTvApiResponse>(endpoint, ct);
            
            return new TmdbTvResult(
                response.Results?.Select(r => new TmdbTvItem(
                    r.Id,
                    r.Name ?? "Unknown",
                    r.Overview,
                    r.VoteAverage,
                    r.FirstAirDate,
                    r.PosterPath,
                    r.GenreIds?.ToList() ?? new List<int>()
                )).ToList() ?? new List<TmdbTvItem>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching TMDb TV for query: {Query}", query);
            return new TmdbTvResult(new List<TmdbTvItem>());
        }
    }

    public async Task<TmdbMovieDetails?> GetMovieDetailsAsync(int movieId, CancellationToken ct = default)
    {
        var endpoint = $"movie/{movieId}?api_key={_apiKey}";
        
        try
        {
            var response = await ExecuteWithRateLimitAsync<TmdbMovieDetailsApiResponse>(endpoint, ct);
            
            if (response == null)
                return null;

            return new TmdbMovieDetails(
                response.Id,
                response.Title ?? "Unknown",
                response.Overview,
                response.VoteAverage,
                response.ReleaseDate,
                response.PosterPath,
                response.Genres?.Select(g => new TmdbGenre(g.Id, g.Name ?? "Unknown")).ToList() ?? new List<TmdbGenre>(),
                response.Runtime,
                response.Status,
                response.Tagline
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movie details for ID: {MovieId}", movieId);
            return null;
        }
    }

    public async Task<TmdbTvDetails?> GetTvDetailsAsync(int tvId, CancellationToken ct = default)
    {
        var endpoint = $"tv/{tvId}?api_key={_apiKey}";
        
        try
        {
            var response = await ExecuteWithRateLimitAsync<TmdbTvDetailsApiResponse>(endpoint, ct);
            
            if (response == null)
                return null;

            return new TmdbTvDetails(
                response.Id,
                response.Name ?? "Unknown",
                response.Overview,
                response.VoteAverage,
                response.FirstAirDate,
                response.PosterPath,
                response.Genres?.Select(g => new TmdbGenre(g.Id, g.Name ?? "Unknown")).ToList() ?? new List<TmdbGenre>(),
                response.Status,
                response.Type,
                response.NumberOfSeasons,
                response.NumberOfEpisodes
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TV details for ID: {TvId}", tvId);
            return null;
        }
    }

    // TMDb API response DTOs
    private sealed record TmdbMultiSearchApiResponse(TmdbMultiSearchItem[]? Results);
    private sealed record TmdbMultiSearchItem(int Id, string? MediaType, string? Name, string? Title, string? Overview, double? VoteAverage, string? ReleaseDate, string? FirstAirDate, string? PosterPath);
    
    private sealed record TmdbDiscoverApiResponse(TmdbDiscoverApiItem[]? Results);
    private sealed record TmdbDiscoverApiItem(int Id, string? Name, string? Title, string? Overview, double? VoteAverage, string? ReleaseDate, string? FirstAirDate, string? PosterPath, int[]? GenreIds);
    
    private sealed record TmdbRecommendationsApiResponse(TmdbRecommendationApiItem[]? Results);
    private sealed record TmdbRecommendationApiItem(int Id, string? Name, string? Title, string? Overview, double? VoteAverage, string? ReleaseDate, string? FirstAirDate, string? PosterPath);
    
    private sealed record TmdbSimilarApiResponse(TmdbSimilarApiItem[]? Results);
    private sealed record TmdbSimilarApiItem(int Id, string? Name, string? Title, string? Overview, double? VoteAverage, string? ReleaseDate, string? FirstAirDate, string? PosterPath);
    
    private sealed record TmdbGenreApiResponse(TmdbGenreApiItem[]? Genres);
    private sealed record TmdbGenreApiItem(int Id, string Name);

    private sealed record TmdbPersonApiResponse(TmdbPersonApiItem[]? Results);
    private sealed record TmdbPersonApiItem(int Id, string? Name, string? ProfilePath, TmdbPersonKnownForApiItem[]? KnownFor);
    private sealed record TmdbPersonKnownForApiItem(int Id, string? MediaType, string? Name, string? Title, string? Overview, double? VoteAverage, string? ReleaseDate, string? FirstAirDate, string? PosterPath);

    private sealed record TmdbMovieApiResponse(TmdbMovieApiItem[]? Results);
    private sealed record TmdbMovieApiItem(int Id, string? Title, string? Overview, double? VoteAverage, string? ReleaseDate, string? PosterPath, int[]? GenreIds);

    private sealed record TmdbTvApiResponse(TmdbTvApiItem[]? Results);
    private sealed record TmdbTvApiItem(int Id, string? Name, string? Overview, double? VoteAverage, string? FirstAirDate, string? PosterPath, int[]? GenreIds);

    private sealed record TmdbMovieDetailsApiResponse(
        int Id, 
        string? Title, 
        string? Overview, 
        double? VoteAverage, 
        string? ReleaseDate, 
        string? PosterPath,
        TmdbGenreApiItem[]? Genres,
        int? Runtime,
        string? Status,
        string? Tagline
    );

    private sealed record TmdbTvDetailsApiResponse(
        int Id,
        string? Name,
        string? Overview,
        double? VoteAverage,
        string? FirstAirDate,
        string? PosterPath,
        TmdbGenreApiItem[]? Genres,
        string? Status,
        string? Type,
        int? NumberOfSeasons,
        int? NumberOfEpisodes
    );
}
