namespace Movora.Domain.FlexiSearch;

/// <summary>
/// Interface for ranking and merging search results based on intent
/// </summary>
public interface IRanker
{
    /// <summary>
    /// Ranks and merges search hits based on extracted intent, removing duplicates
    /// </summary>
    /// <param name="hits">Raw search hits from various sources</param>
    /// <param name="intent">Extracted intent from user query</param>
    /// <returns>Ranked and deduplicated results with reasoning</returns>
    IReadOnlyList<RankedItem> RankAndMerge(IEnumerable<SearchHit> hits, LlmIntent intent);
}

/// <summary>
/// Represents a raw search hit from TMDb or similar sources
/// </summary>
public sealed record SearchHit(
    int TmdbId,
    string MediaType,
    string Name,
    string? Overview,
    double? Rating,
    int? Year,
    string? ThumbnailUrl,
    IDictionary<string, object?> Signals
);

/// <summary>
/// Represents a ranked search result with score and reasoning
/// </summary>
public sealed record RankedItem(
    SearchHit Hit,
    double Score,
    string Reasoning
);
