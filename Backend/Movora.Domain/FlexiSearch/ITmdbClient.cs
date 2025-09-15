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

    /// <summary>
    /// Searches for people (actors, directors, etc.)
    /// </summary>
    /// <param name="query">Person name to search for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Person search results from TMDb</returns>
    Task<TmdbPersonResult> SearchPersonAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Searches for movies by title
    /// </summary>
    /// <param name="query">Movie title to search for</param>
    /// <param name="year">Optional release year filter</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Movie search results from TMDb</returns>
    Task<TmdbMovieResult> SearchMovieAsync(string query, int? year = null, CancellationToken ct = default);

    /// <summary>
    /// Searches for TV series by title
    /// </summary>
    /// <param name="query">TV series title to search for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>TV search results from TMDb</returns>
    Task<TmdbTvResult> SearchTvAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Gets detailed information for a specific movie by TMDb ID
    /// </summary>
    /// <param name="movieId">TMDb movie ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detailed movie information including genres</returns>
    Task<TmdbMovieDetails?> GetMovieDetailsAsync(int movieId, CancellationToken ct = default);

    /// <summary>
    /// Gets detailed information for a specific TV series by TMDb ID
    /// </summary>
    /// <param name="tvId">TMDb TV series ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detailed TV series information including genres</returns>
    Task<TmdbTvDetails?> GetTvDetailsAsync(int tvId, CancellationToken ct = default);
}

/// <summary>
/// Query parameters for content discovery
/// </summary>
public sealed record DiscoverQuery(
    string MediaType,
    IList<int>? GenreIds = null,
    IList<int>? KeywordIds = null,
    IList<int>? WithCast = null,
    IList<int>? WithCrew = null,
    IList<int>? WithPeople = null,
    int? YearFrom = null,
    int? YearTo = null,
    int? RuntimeLteMinutes = null,
    int? RuntimeGteMinutes = null,
    string? WithOriginalLanguage = null,
    int? VoteCountGte = null,
    string? SortBy = null
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

/// <summary>
/// Result from TMDb person search endpoint
/// </summary>
public sealed record TmdbPersonResult(
    IReadOnlyList<TmdbPersonItem> Results
);

/// <summary>
/// Individual person search result
/// </summary>
public sealed record TmdbPersonItem(
    int Id,
    string Name,
    string? ProfilePath,
    IReadOnlyList<TmdbPersonKnownFor> KnownFor
);

/// <summary>
/// Known for item in person search results
/// </summary>
public sealed record TmdbPersonKnownFor(
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
/// Result from TMDb movie search endpoint
/// </summary>
public sealed record TmdbMovieResult(
    IReadOnlyList<TmdbMovieItem> Results
);

/// <summary>
/// Individual movie search result
/// </summary>
public sealed record TmdbMovieItem(
    int Id,
    string Title,
    string? Overview,
    double? VoteAverage,
    string? ReleaseDate,
    string? PosterPath,
    IReadOnlyList<int> GenreIds
);

/// <summary>
/// Result from TMDb TV search endpoint
/// </summary>
public sealed record TmdbTvResult(
    IReadOnlyList<TmdbTvItem> Results
);

/// <summary>
/// Individual TV search result
/// </summary>
public sealed record TmdbTvItem(
    int Id,
    string Name,
    string? Overview,
    double? VoteAverage,
    string? FirstAirDate,
    string? PosterPath,
    IReadOnlyList<int> GenreIds
);

/// <summary>
/// Detailed movie information from TMDb details endpoint
/// </summary>
public sealed record TmdbMovieDetails(
    int Id,
    string Title,
    string? Overview,
    double? VoteAverage,
    string? ReleaseDate,
    string? PosterPath,
    IReadOnlyList<TmdbGenre> Genres,
    int? Runtime,
    string? Status,
    string? Tagline
);

/// <summary>
/// Detailed TV series information from TMDb details endpoint
/// </summary>
public sealed record TmdbTvDetails(
    int Id,
    string Name,
    string? Overview,
    double? VoteAverage,
    string? FirstAirDate,
    string? PosterPath,
    IReadOnlyList<TmdbGenre> Genres,
    string? Status,
    string? Type,
    int? NumberOfSeasons,
    int? NumberOfEpisodes
);

/// <summary>
/// Genre information from TMDb
/// </summary>
public sealed record TmdbGenre(
    int Id,
    string Name
);
