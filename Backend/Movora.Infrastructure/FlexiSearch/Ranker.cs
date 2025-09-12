using Microsoft.Extensions.Logging;
using Movora.Domain.FlexiSearch;

namespace Movora.Infrastructure.FlexiSearch;

/// <summary>
/// Ranks and merges search results based on relevance scoring algorithm
/// </summary>
internal sealed class Ranker : IRanker
{
    private readonly ILogger<Ranker> _logger;

    public Ranker(ILogger<Ranker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<RankedItem> RankAndMerge(IEnumerable<SearchHit> hits, LlmIntent intent)
    {
        if (hits == null)
            throw new ArgumentNullException(nameof(hits));
        if (intent == null)
            throw new ArgumentNullException(nameof(intent));

        _logger.LogDebug("Ranking and merging search hits with intent: {@Intent}", intent);

        var hitsList = hits.ToList();
        var scoredItems = new List<(SearchHit Hit, double Score, List<string> Reasons)>();

        foreach (var hit in hitsList)
        {
            var (score, reasons) = CalculateRelevanceScore(hit, intent);
            scoredItems.Add((hit, score, reasons));
        }

        // Deduplicate by (MediaType, TmdbId), keeping the highest score
        var deduplicatedItems = scoredItems
            .GroupBy(item => new { item.Hit.MediaType, item.Hit.TmdbId })
            .Select(group => group.OrderByDescending(item => item.Score).First())
            .ToList();

        // Sort by score descending and take top results
        var rankedItems = deduplicatedItems
            .OrderByDescending(item => item.Score)
            .Take(50) // Limit to top 50 results
            .Select(item => new RankedItem(
                item.Hit,
                // Cap score at 1.0 unless it's an exact match
                IsExactMatch(item.Hit) ? Math.Min(2.5, item.Score) : Math.Min(1.0, item.Score),
                GenerateReasoning(item.Reasons)
            ))
            .ToList();

        _logger.LogInformation("Ranked {OriginalCount} hits into {FinalCount} results", hitsList.Count, rankedItems.Count);

        return rankedItems;
    }

    /// <summary>
    /// Calculates relevance score for a search hit based on extracted intent
    /// 
    /// Scoring Formula:
    /// - Base score by source: exact match (2.0), direct title (1.0), person (0.95), recs/similar (0.85), discover (0.75)
    /// - Genre/mood match boost: +0.05 each
    /// - Year proximity boost: +0.0 to +0.1 based on closeness
    /// - Runtime constraint match: +0.05
    /// - People match boost: +0.07
    /// - Rating normalization: (rating/10) * 0.2 added to score
    /// </summary>
    private (double Score, List<string> Reasons) CalculateRelevanceScore(SearchHit hit, LlmIntent intent)
    {
        var reasons = new List<string>();
        double score = GetBaseScore(hit, reasons);

        // Genre/mood matching boost
        score += CalculateGenreMoodBoost(hit, intent, reasons);

        // Year proximity boost
        score += CalculateYearProximityBoost(hit, intent, reasons);

        // Runtime constraint boost
        score += CalculateRuntimeBoost(hit, intent, reasons);

        // People matching boost
        score += CalculatePeopleBoost(hit, intent, reasons);

        // Rating boost (normalized)
        score += CalculateRatingBoost(hit, reasons);

        return (score, reasons);
    }

    private static double GetBaseScore(SearchHit hit, List<string> reasons)
    {
        if (!hit.Signals.TryGetValue("source", out var sourceObj) || sourceObj is not string source)
            return 0.5; // Default score for unknown source

        return source switch
        {
            "exact_match" => AddReason(reasons, "Exact title match", 2.0),
            "direct_title" => AddReason(reasons, "Direct title match", 1.0),
            "direct_person" => AddReason(reasons, "Person match", 0.95),
            "recommendations" => AddReason(reasons, "Recommended content", 0.85),
            "similar" => AddReason(reasons, "Similar content", 0.85),
            "discovery" => AddReason(reasons, "Genre/mood discovery", 0.75),
            _ => 0.5
        };
    }

    private static double CalculateGenreMoodBoost(SearchHit hit, LlmIntent intent, List<string> reasons)
    {
        double boost = 0.0;
        var matchedCriteria = new List<string>();

        // For discovery results, we can check genre IDs if available
        if (hit.Signals.TryGetValue("genre_ids", out var genreIdsObj) && genreIdsObj is IEnumerable<int> genreIds)
        {
            // This is a simplified approach - in a real implementation, 
            // you'd map the intent genres/moods to TMDb genre IDs
            var hasGenreMatch = intent.Genres.Any() || intent.Moods.Any();
            if (hasGenreMatch)
            {
                boost += 0.05;
                matchedCriteria.Add("genre/mood");
            }
        }

        // Check for genre/mood keywords in overview or title
        var searchableText = $"{hit.Name} {hit.Overview}".ToLowerInvariant();
        
        foreach (var genre in intent.Genres)
        {
            if (searchableText.Contains(genre.ToLowerInvariant()))
            {
                boost += 0.05;
                matchedCriteria.Add($"genre '{genre}'");
            }
        }

        foreach (var mood in intent.Moods)
        {
            if (ContainsMoodKeywords(searchableText, mood))
            {
                boost += 0.05;
                matchedCriteria.Add($"mood '{mood}'");
            }
        }

        if (matchedCriteria.Any())
        {
            reasons.Add($"Matched {string.Join(", ", matchedCriteria)}");
        }

        return Math.Min(boost, 0.3); // Cap boost at 0.3
    }

    private static double CalculateYearProximityBoost(SearchHit hit, LlmIntent intent, List<string> reasons)
    {
        if (!hit.Year.HasValue || (!intent.YearFrom.HasValue && !intent.YearTo.HasValue))
            return 0.0;

        var hitYear = hit.Year.Value;
        double boost = 0.0;

        if (intent.YearFrom.HasValue && intent.YearTo.HasValue)
        {
            // Year range specified
            if (hitYear >= intent.YearFrom.Value && hitYear <= intent.YearTo.Value)
            {
                boost = 0.1;
                if (intent.YearFrom.Value == intent.YearTo.Value)
                {
                    reasons.Add($"Exact year match ({hitYear})");
                }
                else
                {
                    reasons.Add($"Year in range ({intent.YearFrom}-{intent.YearTo})");
                }
            }
            else
            {
                // Close to range
                var distanceFromRange = Math.Min(
                    Math.Abs(hitYear - intent.YearFrom.Value),
                    Math.Abs(hitYear - intent.YearTo.Value)
                );
                
                if (distanceFromRange <= 3)
                {
                    boost = 0.05;
                    reasons.Add($"Near target year range ({hitYear})");
                }
            }
        }
        else
        {
            // Single year boundary
            var targetYear = intent.YearFrom ?? intent.YearTo!.Value;
            var distance = Math.Abs(hitYear - targetYear);
            
            if (distance == 0)
            {
                boost = 0.1;
                reasons.Add($"Exact year match ({hitYear})");
            }
            else if (distance <= 3)
            {
                boost = 0.05;
                reasons.Add($"Near target year ({hitYear})");
            }
        }

        return boost;
    }

    private static double CalculateRuntimeBoost(SearchHit hit, LlmIntent intent, List<string> reasons)
    {
        if (!intent.RuntimeMaxMinutes.HasValue)
            return 0.0;

        // Note: TMDb doesn't always provide runtime in search results
        // This is a placeholder for runtime checking logic
        // In a real implementation, you might need additional API calls or data
        
        // For now, we'll give a small boost to any content when runtime constraint is specified
        // as it indicates the user cares about runtime
        reasons.Add($"Runtime constraint considered (under {intent.RuntimeMaxMinutes}m)");
        return 0.05;
    }

    private static double CalculatePeopleBoost(SearchHit hit, LlmIntent intent, List<string> reasons)
    {
        if (!intent.People.Any())
            return 0.0;

        var searchableText = $"{hit.Name} {hit.Overview}".ToLowerInvariant();
        var matchedPeople = new List<string>();

        foreach (var person in intent.People)
        {
            if (searchableText.Contains(person.ToLowerInvariant()))
            {
                matchedPeople.Add(person);
            }
        }

        if (matchedPeople.Any())
        {
            reasons.Add($"Features {string.Join(", ", matchedPeople)}");
            return 0.07 * matchedPeople.Count; // 0.07 per matched person
        }

        return 0.0;
    }

    private static double CalculateRatingBoost(SearchHit hit, List<string> reasons)
    {
        if (!hit.Rating.HasValue || hit.Rating.Value <= 0)
            return 0.0;

        var normalizedRating = Math.Min(hit.Rating.Value / 10.0, 1.0);
        var boost = normalizedRating * 0.2; // Max 0.2 boost for perfect rating

        if (hit.Rating.Value >= 8.0)
        {
            reasons.Add($"High rating ({hit.Rating.Value:F1}/10)");
        }
        else if (hit.Rating.Value >= 7.0)
        {
            reasons.Add($"Good rating ({hit.Rating.Value:F1}/10)");
        }

        return boost;
    }

    private static bool ContainsMoodKeywords(string text, string mood)
    {
        var moodKeywords = GetMoodKeywords(mood.ToLowerInvariant());
        return moodKeywords.Any(keyword => text.Contains(keyword));
    }

    private static string[] GetMoodKeywords(string mood)
    {
        return mood switch
        {
            "feel-good" => new[] { "feel-good", "uplifting", "heartwarming", "positive", "cheerful" },
            "dark" => new[] { "dark", "gritty", "noir", "sinister", "bleak" },
            "mind-bending" => new[] { "mind-bending", "complex", "psychological", "twist", "surreal" },
            "cozy" => new[] { "cozy", "comfortable", "warm", "intimate", "charming" },
            "gritty" => new[] { "gritty", "raw", "realistic", "harsh", "tough" },
            "emotional" => new[] { "emotional", "touching", "moving", "tearjerker", "dramatic" },
            "action-packed" => new[] { "action", "explosive", "thrilling", "adrenaline", "intense" },
            "funny" => new[] { "funny", "hilarious", "comedy", "humorous", "witty" },
            "scary" => new[] { "scary", "frightening", "horror", "terrifying", "chilling" },
            "romantic" => new[] { "romantic", "love", "romance", "passion", "relationship" },
            "inspiring" => new[] { "inspiring", "motivational", "uplifting", "empowering", "triumphant" },
            _ => new[] { mood }
        };
    }

    private static string GenerateReasoning(List<string> reasons)
    {
        if (!reasons.Any())
            return "General content match";

        // Take the most important reasons and combine them
        var topReasons = reasons.Take(3).ToList();
        
        if (topReasons.Count == 1)
            return topReasons[0];
        
        if (topReasons.Count == 2)
            return $"{topReasons[0]} and {topReasons[1]}";
        
        return $"{string.Join(", ", topReasons.Take(topReasons.Count - 1))}, and {topReasons.Last()}";
    }

    private static double AddReason(List<string> reasons, string reason, double score)
    {
        reasons.Add(reason);
        return score;
    }

    private static bool IsExactMatch(SearchHit hit)
    {
        return hit.Signals.TryGetValue("source", out var sourceObj) && 
               sourceObj is string source && 
               source == "exact_match";
    }
}
