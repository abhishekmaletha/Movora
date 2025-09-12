using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Movora.Domain.FlexiSearch;

namespace Movora.Infrastructure.FlexiSearch;

/// <summary>
/// Groq implementation of LLM-based search intent extraction
/// </summary>
internal sealed class GroqLlmSearch : ILlmSearch
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqLlmSearch> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    private const string SystemPrompt = """
        You are a movie & TV intent extraction assistant.
        Extract a structured JSON object capturing the user's search intent for TMDb queries.
        Respond with ONLY JSON. Do NOT include markdown fences or prose.
        Schema:
        {
          "titles": string[],
          "people": string[],
          "genres": string[],
          "moods": string[],
          "yearFrom": number|null,
          "yearTo": number|null,
          "runtimeMaxMinutes": number|null,
          "mediaTypes": string[], // allowed: "movie","tv"
          "requestedCount": number|null,
          "isRequestingSuggestions": boolean
        }
        Rules:
        * Infer moods (e.g., feel-good, dark, mind-bending, cozy, gritty).
        * If user says "under 2h" set "runtimeMaxMinutes": 120.
        * If query contains a single exact title, include it in "titles".
        * Extract people (actors/directors) if present.
        * Media type detection: If the user explicitly says 'movies' or 'films', set mediaTypes to only "movie". If they explicitly say 'TV shows' or 'series', set it to only "tv". If not specified, include both "movie" and "tv".
        * If a single year given, set both yearFrom/yearTo to that year.
        * Extract number from phrases like "top 5", "best 10", "give me 3" and set "requestedCount".
        * If no specific count requested, set "requestedCount": null.
        * Set "isRequestingSuggestions": true if the user asks for suggestions, recommendations, similar content, or uses phrases like "like", "similar to", "recommend", "suggest", "what should I watch", "movies like". Set to false for direct title searches.
        * Keep arrays unique and lower-case for genres/moods/mediaTypes, preserve case for titles/people.
        """;

    public GroqLlmSearch(HttpClient httpClient, IConfiguration configuration, ILogger<GroqLlmSearch> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey not configured");
        _model = configuration["Groq:Model"] ?? "llama-3.1-70b-versatile";

        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<LlmIntent> ExtractIntentAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty", nameof(query));

        _logger.LogDebug("Extracting intent using Groq for query: {Query}", query);

        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = $"UserQuery: \"{query}\"\nReturn ONLY JSON following the schema above." }
                },
                temperature = 0.1,
                max_tokens = 500
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", content, ct);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var chatResponse = JsonSerializer.Deserialize<GroqChatResponse>(responseContent);

            if (chatResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
            {
                _logger.LogWarning("Groq returned no content for query: {Query}", query);
                return CreateFallbackIntent(query);
            }

            var intentJson = chatResponse.Choices!.First().Message!.Content!.Trim();
            return ParseLlmIntent(intentJson, query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting intent from Groq for query: {Query}", query);
            return CreateFallbackIntent(query);
        }
    }

    private LlmIntent ParseLlmIntent(string json, string originalQuery)
    {
        try
        {
            // Clean up any potential markdown fences or extra content
            var cleanJson = CleanJsonResponse(json);
            
            var intentData = JsonSerializer.Deserialize<LlmIntentDto>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (intentData == null)
            {
                _logger.LogWarning("Failed to deserialize intent JSON, using fallback for query: {Query}", originalQuery);
                return CreateFallbackIntent(originalQuery);
            }

            return new LlmIntent
            {
                Titles = (intentData.Titles ?? Array.Empty<string>()).ToList(),
                People = (intentData.People ?? Array.Empty<string>()).ToList(),
                Genres = (intentData.Genres ?? Array.Empty<string>()).ToList(),
                Moods = (intentData.Moods ?? Array.Empty<string>()).ToList(),
                YearFrom = intentData.YearFrom,
                YearTo = intentData.YearTo,
                RuntimeMaxMinutes = intentData.RuntimeMaxMinutes,
                MediaTypes = (intentData.MediaTypes ?? new[] { "movie", "tv" }).ToList(),
                RequestedCount = intentData.RequestedCount,
                IsRequestingSuggestions = intentData.IsRequestingSuggestions ?? false
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM intent JSON: {Json}, using fallback for query: {Query}", json, originalQuery);
            return CreateFallbackIntent(originalQuery);
        }
    }

    private static string CleanJsonResponse(string json)
    {
        // Remove markdown code fences if present
        json = json.Replace("```json", "").Replace("```", "").Trim();
        
        // Find the first { and last } to extract just the JSON object
        var firstBrace = json.IndexOf('{');
        var lastBrace = json.LastIndexOf('}');
        
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            json = json.Substring(firstBrace, lastBrace - firstBrace + 1);
        }
        
        return json;
    }

    private static LlmIntent CreateFallbackIntent(string query)
    {
        // Simple fallback: treat the entire query as a title search for both movies and TV
        // Try to extract count from query using simple pattern matching
        int? requestedCount = ExtractCountFromQuery(query);
        
        return new LlmIntent
        {
            Titles = new List<string> { query },
            People = new List<string>(),
            Genres = new List<string>(),
            Moods = new List<string>(),
            YearFrom = null,
            YearTo = null,
            RuntimeMaxMinutes = null,
            MediaTypes = new List<string> { "movie", "tv" },
            RequestedCount = requestedCount,
            IsRequestingSuggestions = false // Default to exact search in fallback
        };
    }

    private static int? ExtractCountFromQuery(string query)
    {
        // Simple regex patterns to extract count from common phrases
        var patterns = new[]
        {
            @"top\s+(\d+)",
            @"best\s+(\d+)",
            @"give\s+me\s+(\d+)",
            @"show\s+me\s+(\d+)",
            @"find\s+(\d+)",
            @"(\d+)\s+movies?",
            @"(\d+)\s+shows?"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(query, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var count) && count > 0 && count <= 100)
            {
                return count;
            }
        }

        return null;
    }

    private sealed record GroqChatResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("object")] string? Object,
        [property: JsonPropertyName("created")] long Created,
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("choices")] GroqChoice[]? Choices,
        [property: JsonPropertyName("usage")] GroqUsage? Usage,
        [property: JsonPropertyName("usage_breakdown")] object? UsageBreakdown,
        [property: JsonPropertyName("system_fingerprint")] string? SystemFingerprint,
        [property: JsonPropertyName("x_groq")] object? XGroq,
        [property: JsonPropertyName("service_tier")] string? ServiceTier
    );

    private sealed record GroqChoice(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("message")] GroqMessage? Message,
        [property: JsonPropertyName("logprobs")] object? Logprobs,
        [property: JsonPropertyName("finish_reason")] string? FinishReason
    );

    private sealed record GroqMessage(
        [property: JsonPropertyName("role")] string? Role,
        [property: JsonPropertyName("content")] string? Content
    );

    private sealed record GroqUsage(
        [property: JsonPropertyName("queue_time")] double QueueTime,
        [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
        [property: JsonPropertyName("prompt_time")] double PromptTime,
        [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
        [property: JsonPropertyName("completion_time")] double CompletionTime,
        [property: JsonPropertyName("total_tokens")] int TotalTokens,
        [property: JsonPropertyName("total_time")] double TotalTime
    );

    private sealed record LlmIntentDto(
        string[]? Titles,
        string[]? People,
        string[]? Genres,
        string[]? Moods,
        int? YearFrom,
        int? YearTo,
        int? RuntimeMaxMinutes,
        string[]? MediaTypes,
        int? RequestedCount,
        bool? IsRequestingSuggestions
    );
}
