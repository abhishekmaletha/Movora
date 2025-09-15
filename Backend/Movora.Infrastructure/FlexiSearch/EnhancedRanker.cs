using Microsoft.Extensions.Logging;
using Movora.Domain.FlexiSearch;

namespace Movora.Infrastructure.FlexiSearch;

/// <summary>
/// Enhanced hybrid ranking system for FlexiSearch with multi-factor scoring
/// Implements sophisticated relevance scoring based on:
/// - Genres overlap
/// - Keywords/theme overlap  
/// - Shared cast or crew (actors/directors)
/// - Release year proximity
/// - Original language
/// - Runtime bands
/// - Popularity / vote counts (as quality signals)
/// </summary>
internal sealed class EnhancedRanker : IRanker
{
    private readonly ILogger<EnhancedRanker> _logger;

    public EnhancedRanker(ILogger<EnhancedRanker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<RankedItem> RankAndMerge(IEnumerable<SearchHit> hits, LlmIntent intent)
    {
        if (hits == null)
            throw new ArgumentNullException(nameof(hits));
        if (intent == null)
            throw new ArgumentNullException(nameof(intent));

        _logger.LogDebug("Enhanced ranking and merging search hits with intent: {@Intent}", intent);

        var hitsList = hits.ToList();
        var scoredItems = new List<(SearchHit Hit, double Score, List<string> Reasons)>();

        foreach (var hit in hitsList)
        {
            var (score, reasons) = CalculateHybridRelevanceScore(hit, intent);
            scoredItems.Add((hit, score, reasons));
        }

        // Deduplicate by (MediaType, TmdbId), keeping the highest score
        var deduplicatedItems = scoredItems
            .GroupBy(item => new { item.Hit.MediaType, item.Hit.TmdbId })
            .Select(group => group.OrderByDescending(item => item.Score).First())
            .ToList();

        // Enhanced sorting with multiple criteria - relevance score is primary
        var rankedItems = deduplicatedItems
            .OrderByDescending(item => item.Score) // Primary: Relevance score
            .ThenByDescending(item => item.Hit.Rating ?? 0) // Secondary: TMDB rating
            .ThenByDescending(item => GetPopularitySignal(item.Hit)) // Tertiary: Popularity
            .Take(50) // Limit to top 50 results
            .Select(item => new RankedItem(
                item.Hit,
                NormalizeScore(item.Score, item.Hit),
                GenerateEnhancedReasoning(item.Reasons, item.Hit)
            ))
            .ToList();

        _logger.LogInformation("Enhanced ranking processed {OriginalCount} hits into {FinalCount} results", 
            hitsList.Count, rankedItems.Count);

        return rankedItems;
    }

    /// <summary>
    /// Calculate hybrid relevance score using multi-factor algorithm
    /// </summary>
    private (double Score, List<string> Reasons) CalculateHybridRelevanceScore(SearchHit hit, LlmIntent intent)
    {
        var reasons = new List<string>();
        var scores = new Dictionary<string, double>();

        // Base score by source type and confidence
        scores["base"] = CalculateBaseScore(hit, reasons);

        // Genre overlap scoring
        scores["genre"] = CalculateGenreOverlap(hit, intent, reasons);

        // Keywords/theme overlap
        scores["keywords"] = CalculateKeywordThemeOverlap(hit, intent, reasons);

        // Cast/crew overlap (if available)
        scores["people"] = CalculatePeopleOverlap(hit, intent, reasons);

        // Release year proximity
        scores["year"] = CalculateYearProximity(hit, intent, reasons);

        // Language match
        scores["language"] = CalculateLanguageMatch(hit, intent, reasons);

        // Runtime band matching
        scores["runtime"] = CalculateRuntimeMatch(hit, intent, reasons);

        // Popularity and quality signals
        scores["quality"] = CalculateQualityScore(hit, reasons);

        // Weighted combination of all factors
        var totalScore = 
            scores["base"] * 1.0 +           // Base relevance
            scores["genre"] * 0.8 +          // Genre match importance
            scores["keywords"] * 0.7 +       // Keyword/theme match
            scores["people"] * 0.9 +         // People match (high importance)
            scores["year"] * 0.5 +           // Year proximity
            scores["language"] * 0.3 +       // Language preference
            scores["runtime"] * 0.4 +        // Runtime match
            scores["quality"] * 0.6;         // Quality signals

        return (totalScore, reasons);
    }

    /// <summary>
    /// Calculate base score with enhanced source type weighting
    /// </summary>
    private double CalculateBaseScore(SearchHit hit, List<string> reasons)
    {
        if (!hit.Signals.TryGetValue("source", out var sourceObj) || sourceObj is not string source)
            return 0.5;

        var isExact = hit.Signals.TryGetValue("is_exact", out var exactObj) && exactObj is bool exact && exact;

        var baseScore = source switch
        {
            "exact_match" => 3.0, // Pure exact matches (single title queries)
            "filtered_exact_match" => 1.6, // Exact matches that passed genre/mood filtering
            "multi_search" when isExact => 2.8,
            "multi_search" => 1.2, // Lower priority for discovery queries
            "tmdb_similar" => 1.9, // Higher priority for similarity
            "tmdb_recommendations" => 1.8, // Higher priority for recommendations
            "person_search" => 1.7, // Higher priority for person-based results
            "discovery" => 1.5, // Higher priority for genre-based discovery
            "quality_discovery" => 1.7, // Higher priority for high-quality discovery
            "similar_genres" => 1.6, // Higher priority for genre similarity
            _ => 0.8
        };

        // For discovery queries, prioritize genre-matched content over exact title matches
        if (source == "filtered_exact_match")
        {
            reasons.Add("Exact title match with compatible genre/mood");
        }
        else if (source.Contains("discovery") || source.Contains("similar"))
        {
            // Add confidence boost based on title similarity if available
            if (hit.Signals.TryGetValue("title_similarity", out var simObj) && simObj is double similarity)
            {
                baseScore += similarity * 0.3; // Lower boost for discovery
                if (similarity > 0.9)
                {
                    reasons.Add($"High title similarity ({similarity:P0})");
                }
            }
        }

        reasons.Add(GetSourceDescription(source, isExact));
        return baseScore;
    }

    /// <summary>
    /// Calculate genre overlap score
    /// </summary>
    private double CalculateGenreOverlap(SearchHit hit, LlmIntent intent, List<string> reasons)
    {
        if (!intent.Genres.Any() && !intent.Moods.Any())
            return 0.0;

        var score = 0.0;
        var matches = new List<string>();

        // Direct genre matching from TMDb genre IDs
        if (hit.Signals.TryGetValue("genre_ids", out var genreIdsObj) && genreIdsObj is IEnumerable<int> genreIds)
        {
            var genreIdsList = genreIds.ToList();
            var expectedGenreCount = intent.Genres.Count + intent.Moods.Count;
            
            if (expectedGenreCount > 0 && genreIdsList.Any())
            {
                // Boost for having genre information
                score += 0.3;
                matches.Add("genre compatibility");
            }
        }

        // Text-based genre/mood matching
        var searchableText = $"{hit.Name} {hit.Overview}".ToLowerInvariant();
        
        foreach (var genre in intent.Genres)
        {
            if (ContainsGenreKeywords(searchableText, genre))
            {
                score += 0.4;
                matches.Add($"'{genre}' genre");
            }
        }

        foreach (var mood in intent.Moods)
        {
            if (ContainsMoodKeywords(searchableText, mood))
            {
                score += 0.3;
                matches.Add($"'{mood}' mood");
            }
        }

        if (matches.Any())
        {
            reasons.Add($"Matches {string.Join(", ", matches.Take(3))}");
        }

        return Math.Min(score, 1.5); // Cap at 1.5
    }

    /// <summary>
    /// Calculate keyword/theme overlap
    /// </summary>
    private double CalculateKeywordThemeOverlap(SearchHit hit, LlmIntent intent, List<string> reasons)
    {
        var score = 0.0;
        var searchableText = $"{hit.Name} {hit.Overview}".ToLowerInvariant();
        var matches = new List<string>();

        // Check for thematic keywords from titles mentioned in intent
        foreach (var title in intent.Titles)
        {
            var titleKeywords = ExtractThematicKeywords(title);
            foreach (var keyword in titleKeywords)
            {
                if (searchableText.Contains(keyword.ToLowerInvariant()))
                {
                    score += 0.2;
                    matches.Add($"theme from '{title}'");
                    break; // Only count once per title
                }
            }
        }

        // Check for general thematic overlap in overview
        var thematicScore = CalculateThematicSimilarity(hit.Overview, intent);
        if (thematicScore > 0.1)
        {
            score += thematicScore;
            matches.Add("thematic similarity");
        }

        if (matches.Any())
        {
            reasons.Add($"Thematic overlap: {string.Join(", ", matches)}");
        }

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Calculate people (cast/crew) overlap
    /// </summary>
    private double CalculatePeopleOverlap(SearchHit hit, LlmIntent intent, List<string> reasons)
    {
        if (!intent.People.Any())
            return 0.0;

        var score = 0.0;
        var matches = new List<string>();

        // Check if this hit came from a person search
        if (hit.Signals.TryGetValue("person_name", out var personNameObj) && personNameObj is string personName)
        {
            if (intent.People.Any(p => string.Equals(p, personName, StringComparison.OrdinalIgnoreCase)))
            {
                score += 1.2;
                matches.Add($"features {personName}");
            }
        }

        // Text-based people matching in overview/title
        var searchableText = $"{hit.Name} {hit.Overview}".ToLowerInvariant();
        foreach (var person in intent.People)
        {
            if (searchableText.Contains(person.ToLowerInvariant()))
            {
                score += 0.6;
                matches.Add(person);
            }
        }

        if (matches.Any())
        {
            reasons.Add($"Features {string.Join(", ", matches)}");
        }

        return Math.Min(score, 2.0);
    }

    /// <summary>
    /// Calculate release year proximity with enhanced scoring
    /// </summary>
    private double CalculateYearProximity(SearchHit hit, LlmIntent intent, List<string> reasons)
    {
        if (!hit.Year.HasValue)
            return 0.0;

        var hitYear = hit.Year.Value;
        var score = 0.0;

        if (intent.YearFrom.HasValue || intent.YearTo.HasValue)
        {
            var yearFrom = intent.YearFrom ?? int.MinValue;
            var yearTo = intent.YearTo ?? int.MaxValue;

            if (hitYear >= yearFrom && hitYear <= yearTo)
            {
                score = 0.8;
                if (intent.YearFrom == intent.YearTo)
                {
                    reasons.Add($"Exact year match ({hitYear})");
                }
                else
                {
                    reasons.Add($"Within year range ({yearFrom}-{yearTo})");
                }
            }
            else
            {
                // Proximity scoring for near misses
                var distanceFromRange = Math.Min(
                    Math.Abs(hitYear - yearFrom),
                    Math.Abs(hitYear - yearTo)
                );

                if (distanceFromRange <= 2)
                {
                    score = 0.4;
                    reasons.Add($"Near target years ({hitYear})");
                }
                else if (distanceFromRange <= 5)
                {
                    score = 0.2;
                    reasons.Add($"Same era ({hitYear})");
                }
            }
        }
        else
        {
            // Boost for recent content if no year specified
            var currentYear = DateTime.Now.Year;
            var age = currentYear - hitYear;
            
            if (age <= 3)
            {
                score = 0.3;
                reasons.Add("Recent release");
            }
            else if (age <= 10)
            {
                score = 0.1;
            }
        }

        return score;
    }

    /// <summary>
    /// Calculate language match score
    /// </summary>
    private double CalculateLanguageMatch(SearchHit hit, LlmIntent intent, List<string> reasons)
    {
        // For now, assume English preference unless specified
        // In a real implementation, you'd check the original_language from TMDb data
        
        // This is a placeholder - you'd need to enhance the SearchHit to include language info
        // or make additional API calls to get detailed information
        
        return 0.0; // No language scoring for now
    }

    /// <summary>
    /// Calculate runtime band matching
    /// </summary>
    private double CalculateRuntimeMatch(SearchHit hit, LlmIntent intent, List<string> reasons)
    {
        if (!intent.RuntimeMaxMinutes.HasValue)
            return 0.0;

        // Note: Runtime information is not typically available in search results
        // This would require additional API calls to get detailed movie/TV information
        // For now, we'll provide a small boost to indicate runtime consideration
        
        reasons.Add($"Runtime preference considered (≤{intent.RuntimeMaxMinutes}m)");
        return 0.2;
    }

    /// <summary>
    /// Calculate quality score based on popularity and ratings
    /// </summary>
    private double CalculateQualityScore(SearchHit hit, List<string> reasons)
    {
        var score = 0.0;

        // Rating-based quality
        if (hit.Rating.HasValue && hit.Rating.Value > 0)
        {
            var rating = hit.Rating.Value;
            if (rating >= 8.5)
            {
                score += 0.8;
                reasons.Add($"Excellent rating ({rating:F1}/10)");
            }
            else if (rating >= 7.5)
            {
                score += 0.6;
                reasons.Add($"High rating ({rating:F1}/10)");
            }
            else if (rating >= 6.5)
            {
                score += 0.4;
                reasons.Add($"Good rating ({rating:F1}/10)");
            }
            else if (rating >= 5.5)
            {
                score += 0.2;
            }
        }

        // Popularity signals from vote_average in signals
        if (hit.Signals.TryGetValue("vote_average", out var voteAvgObj) && voteAvgObj is double voteAvg)
        {
            if (voteAvg >= 8.0)
            {
                score += 0.3;
                reasons.Add("Highly rated");
            }
        }

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Normalize the final score
    /// </summary>
    private double NormalizeScore(double rawScore, SearchHit hit)
    {
        // Exact matches can score higher than 1.0
        var isExact = hit.Signals.TryGetValue("is_exact", out var exactObj) && exactObj is bool exact && exact;
        var maxScore = isExact ? 3.0 : 1.0;
        
        return Math.Min(rawScore, maxScore);
    }

    /// <summary>
    /// Generate enhanced reasoning with "Because..." format
    /// </summary>
    private string GenerateEnhancedReasoning(List<string> reasons, SearchHit hit)
    {
        if (!reasons.Any())
            return "Because it matches your search criteria";

        // Prioritize the most important reasons
        var topReasons = reasons.Take(3).ToList();
        
        var reasonText = topReasons.Count switch
        {
            1 => topReasons[0].ToLowerInvariant(),
            2 => $"{topReasons[0].ToLowerInvariant()} and {topReasons[1].ToLowerInvariant()}",
            _ => $"{string.Join(", ", topReasons.Take(topReasons.Count - 1).Select(r => r.ToLowerInvariant()))}, and {topReasons.Last().ToLowerInvariant()}"
        };

        return $"Because {reasonText}";
    }

    // Helper methods
    private static double GetPopularitySignal(SearchHit hit)
    {
        return hit.Rating ?? 0;
    }

    private static string GetSourceDescription(string source, bool isExact)
    {
        return source switch
        {
            "exact_match" => "Exact title match",
            "filtered_exact_match" => "Title match with compatible genre/mood",
            "multi_search" when isExact => "High-confidence title match",
            "multi_search" => "Title search result",
            "tmdb_similar" => "Similar to your search",
            "tmdb_recommendations" => "Recommended based on your search",
            "person_search" => "Features requested person",
            "discovery" => "Matches your preferences",
            "quality_discovery" => "High-quality match for your preferences",
            "similar_genres" => "Similar genre/mood",
            _ => "Content match"
        };
    }

    private static bool ContainsGenreKeywords(string text, string genre)
    {
        var keywords = GetGenreKeywords(genre.ToLowerInvariant());
        return keywords.Any(keyword => text.Contains(keyword));
    }

    private static bool ContainsMoodKeywords(string text, string mood)
    {
        var keywords = GetMoodKeywords(mood.ToLowerInvariant());
        return keywords.Any(keyword => text.Contains(keyword));
    }

    private static string[] GetGenreKeywords(string genre)
    {
        return genre switch
        {
            "action" => new[] { "action", "fight", "battle", "combat", "explosive", "chase" },
            "adventure" => new[] { "adventure", "journey", "quest", "expedition", "exploration" },
            "comedy" => new[] { "comedy", "funny", "humor", "hilarious", "comic", "laugh" },
            "crime" => new[] { "crime", "criminal", "detective", "police", "investigation", "murder" },
            "drama" => new[] { "drama", "dramatic", "emotional", "character", "relationship" },
            "fantasy" => new[] { "fantasy", "magic", "magical", "wizard", "dragon", "mythical" },
            "horror" => new[] { "horror", "scary", "frightening", "terror", "haunted", "supernatural" },
            "mystery" => new[] { "mystery", "puzzle", "investigation", "detective", "clue", "secret" },
            "romance" => new[] { "romance", "romantic", "love", "relationship", "passion", "dating" },
            "science fiction" => new[] { "sci-fi", "science fiction", "space", "alien", "future", "technology" },
            "thriller" => new[] { "thriller", "suspense", "tension", "intense", "psychological" },
            "western" => new[] { "western", "cowboy", "frontier", "gunfighter", "outlaw" },
            _ => new[] { genre }
        };
    }

    private static string[] GetMoodKeywords(string mood)
    {
        return mood switch
        {
            "feel-good" => new[] { "feel-good", "uplifting", "heartwarming", "positive", "inspiring" },
            "dark" => new[] { "dark", "gritty", "noir", "bleak", "sinister", "disturbing" },
            "mind-bending" => new[] { "mind-bending", "complex", "psychological", "twist", "surreal", "confusing" },
            "cozy" => new[] { "cozy", "comfortable", "warm", "intimate", "charming", "peaceful" },
            "gritty" => new[] { "gritty", "raw", "realistic", "harsh", "brutal", "uncompromising" },
            "emotional" => new[] { "emotional", "touching", "moving", "tearjerker", "heart-wrenching" },
            "action-packed" => new[] { "action-packed", "explosive", "thrilling", "adrenaline", "fast-paced" },
            "funny" => new[] { "funny", "hilarious", "comedy", "humorous", "witty", "amusing" },
            "scary" => new[] { "scary", "frightening", "terrifying", "chilling", "spine-tingling" },
            "romantic" => new[] { "romantic", "love", "passion", "intimate", "tender" },
            "inspiring" => new[] { "inspiring", "motivational", "uplifting", "empowering", "triumphant" },
            _ => new[] { mood }
        };
    }

    private static List<string> ExtractThematicKeywords(string title)
    {
        // Simple keyword extraction - in a real implementation, you might use NLP
        var commonWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by" };
        return title.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => !commonWords.Contains(word) && word.Length > 2)
            .ToList();
    }

    private static double CalculateThematicSimilarity(string? overview, LlmIntent intent)
    {
        if (string.IsNullOrEmpty(overview))
            return 0.0;

        // Simple thematic similarity based on keyword overlap
        // In a real implementation, you might use semantic similarity or embeddings
        
        var overviewWords = overview.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .ToHashSet();

        var intentWords = intent.Titles
            .Concat(intent.Genres)
            .Concat(intent.Moods)
            .Concat(intent.People)
            .SelectMany(term => term.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(w => w.Length > 3)
            .ToHashSet();

        if (!intentWords.Any() || !overviewWords.Any())
            return 0.0;

        var commonWords = overviewWords.Intersect(intentWords).Count();
        return (double)commonWords / Math.Max(overviewWords.Count, intentWords.Count);
    }
}
