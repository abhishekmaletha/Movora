namespace Movora.Domain.FlexiSearch;

/// <summary>
/// Interface for TMDb API client operations
/// </summary>
public interface ITmdbClient
{
    /// <summary>
    /// Searches for movies and TV shows using multi-search endpoint
    /// </summary>
    /// <param name="query">Search query string</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Multi-search results from TMDb</returns>
    Task<TmdbMultiResult> SearchMultiAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Discovers content based on filters like genres, year, runtime
    /// </summary>
    /// <param name="query">Discovery query parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Discover results from TMDb</returns>
    Task<TmdbDiscoverResult> DiscoverAsync(DiscoverQuery query, CancellationToken ct = default);

    /// <summary>
    /// Finds exact title match for a given title string
    /// </summary>
    /// <param name="title">Title to search for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Exact title match if found, null otherwise</returns>
    Task<TmdbTitleResult?> FindExactTitleAsync(string title, CancellationToken ct = default);

    /// <summary>
    /// Gets recommendations for a specific movie or TV show
    /// </summary>
    /// <param name="mediaType">Media type ("movie" or "tv")</param>
    /// <param name="tmdbId">TMDb ID of the content</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Recommendations from TMDb</returns>
    Task<TmdbRecommendationsResult> GetRecommendationsAsync(string mediaType, int tmdbId, CancellationToken ct = default);

    /// <summary>
    /// Gets similar content for a specific movie or TV show
    /// </summary>
    /// <param name="mediaType">Media type ("movie" or "tv")</param>
    /// <param name="tmdbId">TMDb ID of the content</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Similar content from TMDb</returns>
    Task<TmdbSimilarResult> GetSimilarAsync(string mediaType, int tmdbId, CancellationToken ct = default);

    /// <summary>
    /// Gets genre mapping for a specific media type
    /// </summary>
    /// <param name="mediaType">Media type ("movie" or "tv")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dictionary mapping genre names to TMDb genre IDs</returns>
    Task<IReadOnlyDictionary<string, int>> GetGenreMapAsync(string mediaType, CancellationToken ct = default);
}

/// <summary>
/// Query parameters for content discovery
/// </summary>
public sealed record DiscoverQuery(
    string MediaType,
    IList<int> GenreIds,
    int? YearFrom,
    int? YearTo,
    int? RuntimeLteMinutes,
    string? SortBy
);

/// <summary>
/// Result from TMDb multi-search endpoint
/// </summary>
public sealed record TmdbMultiResult(
    IReadOnlyList<TmdbMultiItem> Results
);

/// <summary>
/// Individual item from multi-search results
/// </summary>
public sealed record TmdbMultiItem(
    int Id,
    string MediaType,
    string? Name,
    string? Title,
    string? Overview,
    double? VoteAverage,
    string? ReleaseDate,
    string? FirstAirDate,
    string? PosterPath
);

/// <summary>
/// Result from TMDb discover endpoint
/// </summary>
public sealed record TmdbDiscoverResult(
    IReadOnlyList<TmdbDiscoverItem> Results
);

/// <summary>
/// Individual item from discover results
/// </summary>
public sealed record TmdbDiscoverItem(
    int Id,
    string? Name,
    string? Title,
    string? Overview,
    double? VoteAverage,
    string? ReleaseDate,
    string? FirstAirDate,
    string? PosterPath,
    IReadOnlyList<int> GenreIds
);

/// <summary>
/// Result for exact title match
/// </summary>
public sealed record TmdbTitleResult(
    int Id,
    string MediaType,
    string Name,
    string? Overview,
    double? VoteAverage,
    int? Year,
    string? PosterPath
);

/// <summary>
/// Result from recommendations endpoint
/// </summary>
public sealed record TmdbRecommendationsResult(
    IReadOnlyList<TmdbRecommendationItem> Results
);

/// <summary>
/// Individual recommendation item
/// </summary>
public sealed record TmdbRecommendationItem(
    int Id,
    string? Name,
    string? Title,
    string? Overview,
    double? VoteAverage,
    string? ReleaseDate,
    string? FirstAirDate,
    string? PosterPath
);

/// <summary>
/// Result from similar content endpoint
/// </summary>
public sealed record TmdbSimilarResult(
    IReadOnlyList<TmdbSimilarItem> Results
);

/// <summary>
/// Individual similar content item
/// </summary>
public sealed record TmdbSimilarItem(
    int Id,
    string? Name,
    string? Title,
    string? Overview,
    double? VoteAverage,
    string? ReleaseDate,
    string? FirstAirDate,
    string? PosterPath
);
