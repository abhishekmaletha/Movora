namespace Movora.Domain.FlexiSearch;

/// <summary>
/// Interface for LLM-based intent extraction from natural language queries
/// </summary>
public interface ILlmSearch
{
    /// <summary>
    /// Extracts structured intent from a natural language search query
    /// </summary>
    /// <param name="query">The user's natural language query</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Structured intent containing extracted search parameters</returns>
    Task<LlmIntent> ExtractIntentAsync(string query, CancellationToken ct = default);
}
