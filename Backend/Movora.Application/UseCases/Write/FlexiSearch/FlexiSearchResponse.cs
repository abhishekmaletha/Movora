namespace Movora.Application.UseCases.Write.FlexiSearch;

/// <summary>
/// Response DTO for flexible search functionality
/// </summary>
public sealed record FlexiSearchResponse
{
    /// <summary>
    /// List of movie/TV search results with details and relevance scoring
    /// </summary>
    public required IReadOnlyList<MovieSearchDetails> Results { get; init; }

    /// <summary>
    /// Trace identifier for request tracking and debugging
    /// </summary>
    public string? TraceId { get; init; }
}

/// <summary>
/// Detailed information about a movie or TV show search result
/// </summary>
public sealed record MovieSearchDetails
{
    /// <summary>
    /// TMDb identifier for the content
    /// </summary>
    public required int TmdbId { get; init; }

    /// <summary>
    /// Name or title of the content
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Media type - "movie" or "tv"
    /// </summary>
    public required string MediaType { get; init; }

    /// <summary>
    /// URL to thumbnail/poster image
    /// </summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>
    /// Average rating (0-10 scale from TMDb)
    /// </summary>
    public double? Rating { get; init; }

    /// <summary>
    /// Plot summary or overview of the content
    /// </summary>
    public string? Overview { get; init; }

    /// <summary>
    /// Release year of the content
    /// </summary>
    public int? Year { get; init; }

    /// <summary>
    /// Relevance score based on query matching (0-1 scale)
    /// </summary>
    public required double RelevanceScore { get; init; }

    /// <summary>
    /// Human-readable explanation of why this result matches the query
    /// </summary>
    public required string Reasoning { get; init; }
}
