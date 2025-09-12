namespace Movora.Domain.FlexiSearch;

/// <summary>
/// Represents the structured intent extracted from user's natural language query by LLM
/// </summary>
public sealed record LlmIntent
{
    /// <summary>
    /// Specific titles mentioned in the query (e.g., "Inception", "Stranger Things")
    /// </summary>
    public IReadOnlyList<string> Titles { get; init; } = new List<string>();

    /// <summary>
    /// People mentioned (actors, directors) in the query
    /// </summary>
    public IReadOnlyList<string> People { get; init; } = new List<string>();

    /// <summary>
    /// Genre keywords extracted from the query (e.g., "sci-fi", "comedy", "thriller")
    /// </summary>
    public IReadOnlyList<string> Genres { get; init; } = new List<string>();

    /// <summary>
    /// Mood/tone descriptors extracted (e.g., "feel-good", "dark", "mind-bending")
    /// </summary>
    public IReadOnlyList<string> Moods { get; init; } = new List<string>();

    /// <summary>
    /// Start year for filtering content (inclusive)
    /// </summary>
    public int? YearFrom { get; init; }

    /// <summary>
    /// End year for filtering content (inclusive)
    /// </summary>
    public int? YearTo { get; init; }

    /// <summary>
    /// Maximum runtime in minutes for filtering content
    /// </summary>
    public int? RuntimeMaxMinutes { get; init; }

    /// <summary>
    /// Media types to search (e.g., "movie", "tv")
    /// </summary>
    public IReadOnlyList<string> MediaTypes { get; init; } = new List<string>();

    /// <summary>
    /// Requested number of results (e.g., "top 5", "best 10")
    /// If null, return all relevant results
    /// </summary>
    public int? RequestedCount { get; init; }

    /// <summary>
    /// Whether the user is asking for suggestions, recommendations, or similar content
    /// If false, prioritize exact matches only
    /// </summary>
    public bool IsRequestingSuggestions { get; init; }
}
