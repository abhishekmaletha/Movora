using MediatR;
using Microsoft.Extensions.Logging;
using Movora.Domain.FlexiSearch;

namespace Movora.Application.UseCases.Write.FlexiSearch;

/// <summary>
/// Enhanced FlexiSearch brain for movies/TV discovery app powered by TMDB
/// Implements sophisticated intent detection and hybrid ranking system
/// </summary>
public sealed class EnhancedFlexiSearchCommandHandler : IRequestHandler<EnhancedFlexiSearchCommand, FlexiSearchResponse>
{
    private readonly ILlmSearch _llm;
    private readonly ITmdbClient _tmdb;
    private readonly IRanker _ranker;
    private readonly ILogger<EnhancedFlexiSearchCommandHandler> _logger;

    public EnhancedFlexiSearchCommandHandler(
        ILlmSearch llm,
        ITmdbClient tmdb,
        IRanker ranker,
        ILogger<EnhancedFlexiSearchCommandHandler> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _tmdb = tmdb ?? throw new ArgumentNullException(nameof(tmdb));
        _ranker = ranker ?? throw new ArgumentNullException(nameof(ranker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FlexiSearchResponse> Handle(EnhancedFlexiSearchCommand request, CancellationToken cancellationToken)
    {
        if (request?.Request == null)
            throw new ArgumentNullException(nameof(request));

        var query = request.Request.Query?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            _logger.LogWarning("FlexiSearch called with empty or null query");
            return new FlexiSearchResponse { Results = Array.Empty<MovieSearchDetails>() };
        }

        _logger.LogInformation("Processing enhanced FlexiSearch query: {Query}", query);

        try
        {
            // Step A: Parse user intent and entities
            var intent = await _llm.ExtractIntentAsync(query, cancellationToken);
            _logger.LogDebug("Extracted intent: {@Intent}", intent);

            // Step B: Resolve IDs using multi-search first
            var (exactMatches, searchCandidates) = await ResolveIdsAsync(intent, query, cancellationToken);

            // Step C: Determine if this is an exact title case
            if (ShouldReturnExactMatch(intent, exactMatches))
            {
                _logger.LogInformation("Returning exact match for high-confidence title lookup");
                return CreateExactMatchResponse(exactMatches.First());
            }

            // Step D: Similar/discovery case - build comprehensive search hits
            var searchHits = new List<SearchHit>();

            // For discovery queries, prioritize genre/mood-based results over exact title matches
            // Only include exact matches if they fit the requested genres/moods
            if (exactMatches.Any())
            {
                // Filter exact matches by genre/mood compatibility if specified
                var filteredExactMatches = FilterExactMatchesByGenreMood(exactMatches, intent);
                if (filteredExactMatches.Any())
                {
                    searchHits.AddRange(ConvertToSearchHits(filteredExactMatches, "filtered_exact_match", isExact: false));
                }
            }

            // Add multi-search candidates (lower priority for discovery)
            searchHits.AddRange(searchCandidates);

            // Execute discovery queries for similarity - this is the main source for discovery
            var discoveryHits = await ExecuteDiscoverySearchAsync(intent, exactMatches, cancellationToken);
            searchHits.AddRange(discoveryHits);

            // Get TMDB built-in similar results if we have exact matches
            var similarHits = await GetTmdbSimilarResultsAsync(exactMatches, cancellationToken);
            searchHits.AddRange(similarHits);

            // Step E: Hybrid ranking and response formatting
            var rankedResults = _ranker.RankAndMerge(searchHits, intent);
            
            // Apply media type filtering based on user request
            var mediaFilteredResults = ApplyMediaTypeFiltering(rankedResults, intent);
            
            // Apply count limit if requested
            var limitedResults = intent.RequestedCount.HasValue 
                ? mediaFilteredResults.Take(intent.RequestedCount.Value) 
                : mediaFilteredResults.Take(12); // Default to top 12 results

            var results = limitedResults.Select(item => new MovieSearchDetails
            {
                TmdbId = item.Hit.TmdbId,
                Name = item.Hit.Name,
                MediaType = item.Hit.MediaType,
                ThumbnailUrl = item.Hit.ThumbnailUrl,
                Rating = item.Hit.Rating,
                Overview = item.Hit.Overview,
                Year = item.Hit.Year,
                RelevanceScore = item.Score,
                Reasoning = item.Reasoning
            }).ToList();

            _logger.LogInformation("Enhanced FlexiSearch completed with {ResultCount} results", results.Count);

            return new FlexiSearchResponse { Results = results };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing enhanced FlexiSearch query: {Query}", query);
            return new FlexiSearchResponse { Results = Array.Empty<MovieSearchDetails>() };
        }
    }

    /// <summary>
    /// Step B: Resolve IDs using multi-search to classify into movie/tv/person
    /// </summary>
    private async Task<(List<ExactMatch> exactMatches, List<SearchHit> candidates)> ResolveIdsAsync(
        LlmIntent intent, 
        string originalQuery, 
        CancellationToken cancellationToken)
    {
        var exactMatches = new List<ExactMatch>();
        var candidates = new List<SearchHit>();

        // Search for specific titles mentioned in intent
        foreach (var title in intent.Titles)
        {
            var multiResult = await _tmdb.SearchMultiAsync(title, cancellationToken);
            var (exact, cands) = AnalyzeMultiSearchResults(multiResult, title, intent);
            exactMatches.AddRange(exact);
            candidates.AddRange(cands);
        }

        // If no specific titles, search the original query
        if (!intent.Titles.Any())
        {
            var multiResult = await _tmdb.SearchMultiAsync(originalQuery, cancellationToken);
            var (exact, cands) = AnalyzeMultiSearchResults(multiResult, originalQuery, intent);
            exactMatches.AddRange(exact);
            candidates.AddRange(cands);
        }

        // Search for people mentioned
        foreach (var person in intent.People)
        {
            var personResult = await _tmdb.SearchPersonAsync(person, cancellationToken);
            candidates.AddRange(ConvertPersonResultToHits(personResult, "person_search"));
        }

        return (exactMatches, candidates);
    }

    /// <summary>
    /// Analyze multi-search results to determine exact matches vs candidates
    /// </summary>
    private (List<ExactMatch>, List<SearchHit>) AnalyzeMultiSearchResults(
        TmdbMultiResult multiResult, 
        string searchTerm, 
        LlmIntent intent)
    {
        var exactMatches = new List<ExactMatch>();
        var candidates = new List<SearchHit>();

        foreach (var item in multiResult.Results)
        {
            var itemTitle = item.Name ?? item.Title ?? "";
            var isExactMatch = IsHighConfidenceExactMatch(itemTitle, searchTerm, item, intent);

            if (isExactMatch)
            {
                exactMatches.Add(new ExactMatch(
                    item.Id,
                    item.MediaType,
                    itemTitle,
                    item.Overview,
                    item.VoteAverage,
                    ExtractYear(item.ReleaseDate ?? item.FirstAirDate),
                    BuildImageUrl(item.PosterPath)
                ));
            }
            else
            {
                candidates.Add(new SearchHit(
                    item.Id,
                    item.MediaType,
                    itemTitle,
                    item.Overview,
                    item.VoteAverage,
                    ExtractYear(item.ReleaseDate ?? item.FirstAirDate),
                    BuildImageUrl(item.PosterPath),
                    new Dictionary<string, object?> 
                    { 
                        ["source"] = "multi_search",
                        ["search_term"] = searchTerm,
                        ["title_similarity"] = CalculateTitleSimilarity(itemTitle, searchTerm)
                    }
                ));
            }
        }

        return (exactMatches, candidates);
    }

    /// <summary>
    /// Determine if this is a high-confidence exact match
    /// </summary>
    private bool IsHighConfidenceExactMatch(string itemTitle, string searchTerm, TmdbMultiItem item, LlmIntent intent)
    {
        // Strong title similarity
        var titleSimilarity = CalculateTitleSimilarity(itemTitle, searchTerm);
        if (titleSimilarity < 0.85) return false;

        // If year is specified in intent, it should match
        if (intent.YearFrom.HasValue || intent.YearTo.HasValue)
        {
            var itemYear = ExtractYear(item.ReleaseDate ?? item.FirstAirDate);
            if (itemYear.HasValue)
            {
                if (intent.YearFrom.HasValue && itemYear < intent.YearFrom) return false;
                if (intent.YearTo.HasValue && itemYear > intent.YearTo) return false;
            }
        }

        // Must be a movie or TV show (not person)
        return item.MediaType == "movie" || item.MediaType == "tv";
    }

    /// <summary>
    /// Calculate string similarity between titles
    /// </summary>
    private double CalculateTitleSimilarity(string title1, string title2)
    {
        if (string.IsNullOrEmpty(title1) || string.IsNullOrEmpty(title2)) return 0;

        // Exact match (case insensitive)
        if (string.Equals(title1.Trim(), title2.Trim(), StringComparison.OrdinalIgnoreCase))
            return 1.0;

        // Simple Levenshtein distance-based similarity
        var distance = LevenshteinDistance(title1.ToLowerInvariant(), title2.ToLowerInvariant());
        var maxLength = Math.Max(title1.Length, title2.Length);
        return 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Simple Levenshtein distance implementation
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++) matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    /// <summary>
    /// Step C: Determine if we should return exact match only
    /// </summary>
    private bool ShouldReturnExactMatch(LlmIntent intent, List<ExactMatch> exactMatches)
    {
        // Must have at least one exact match
        if (!exactMatches.Any()) return false;

        // If user is requesting suggestions/similar content, never return exact match only
        if (intent.IsRequestingSuggestions) return false;

        // If user specified multiple titles, they likely want discovery, not exact matches
        if (intent.Titles.Count > 1) return false;

        // If user specified moods/genres along with titles, they want discovery
        if (intent.Genres.Any() || intent.Moods.Any()) return false;

        // If user specified people along with titles, they want discovery
        if (intent.People.Any()) return false;

        // Only return exact match for single, clear title requests without additional criteria
        if (intent.Titles.Count == 1 && 
            !intent.Genres.Any() && 
            !intent.Moods.Any() && 
            !intent.People.Any())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Create response for exact match case
    /// </summary>
    private FlexiSearchResponse CreateExactMatchResponse(ExactMatch exactMatch)
    {
        var result = new MovieSearchDetails
        {
            TmdbId = exactMatch.Id,
            Name = exactMatch.Name,
            MediaType = exactMatch.MediaType,
            ThumbnailUrl = exactMatch.PosterPath,
            Rating = exactMatch.VoteAverage,
            Overview = exactMatch.Overview,
            Year = exactMatch.Year,
            RelevanceScore = 1.0,
            Reasoning = "Exact title match"
        };

        return new FlexiSearchResponse { Results = new[] { result } };
    }

    /// <summary>
    /// Step D: Execute discovery queries for similarity
    /// </summary>
    private async Task<List<SearchHit>> ExecuteDiscoverySearchAsync(
        LlmIntent intent, 
        List<ExactMatch> exactMatches, 
        CancellationToken cancellationToken)
    {
        var hits = new List<SearchHit>();

        // Build discovery queries based on intent - normalize media types
        var normalizedMediaTypes = GetNormalizedMediaTypes(intent);
        var mediaTypes = normalizedMediaTypes.Any() ? normalizedMediaTypes : new List<string> { "movie", "tv" };

        foreach (var mediaType in mediaTypes)
        {
            try
            {
                // Get genre IDs from intent
                var genreIds = await MapGenresAndMoodsToIds(intent.Genres, intent.Moods, mediaType, cancellationToken);

                // Get person IDs if people are mentioned
                var personIds = await GetPersonIds(intent.People, cancellationToken);

                // Build comprehensive discovery query
                var discoverQuery = new DiscoverQuery(
                    MediaType: mediaType,
                    GenreIds: genreIds.Any() ? genreIds : null,
                    WithPeople: personIds.Any() ? personIds : null,
                    YearFrom: intent.YearFrom,
                    YearTo: intent.YearTo,
                    RuntimeLteMinutes: intent.RuntimeMaxMinutes,
                    VoteCountGte: 200, // Quality filter
                    SortBy: "popularity.desc"
                );

                var result = await _tmdb.DiscoverAsync(discoverQuery, cancellationToken);
                hits.AddRange(ConvertDiscoverResultToHits(result, mediaType, "discovery"));

                // Also try sorting by vote_average for quality results
                if (genreIds.Any() || personIds.Any())
                {
                    var qualityQuery = discoverQuery with { SortBy = "vote_average.desc", VoteCountGte = 500 };
                    var qualityResult = await _tmdb.DiscoverAsync(qualityQuery, cancellationToken);
                    hits.AddRange(ConvertDiscoverResultToHits(qualityResult, mediaType, "quality_discovery"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in discovery search for media type: {MediaType}", mediaType);
            }
        }

        return hits;
    }

    /// <summary>
    /// Get TMDB built-in similar results
    /// </summary>
    private async Task<List<SearchHit>> GetTmdbSimilarResultsAsync(
        List<ExactMatch> exactMatches, 
        CancellationToken cancellationToken)
    {
        var hits = new List<SearchHit>();

        foreach (var match in exactMatches)
        {
            try
            {
                // Get similar content
                var similar = await _tmdb.GetSimilarAsync(match.MediaType, match.Id, cancellationToken);
                hits.AddRange(ConvertSimilarToHits(similar, match.MediaType, "tmdb_similar"));

                // Get recommendations
                var recommendations = await _tmdb.GetRecommendationsAsync(match.MediaType, match.Id, cancellationToken);
                hits.AddRange(ConvertRecommendationsToHits(recommendations, match.MediaType, "tmdb_recommendations"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting TMDB similar results for {MediaType} {Id}", match.MediaType, match.Id);
            }
        }

        return hits;
    }

    /// <summary>
    /// Map genres and moods to TMDB genre IDs
    /// </summary>
    private async Task<List<int>> MapGenresAndMoodsToIds(
        IReadOnlyList<string> genres, 
        IReadOnlyList<string> moods, 
        string mediaType, 
        CancellationToken cancellationToken)
    {
        var genreIds = new List<int>();

        try
        {
            var genreMap = await _tmdb.GetGenreMapAsync(mediaType, cancellationToken);

            // Map genres directly
            foreach (var genre in genres)
            {
                if (genreMap.TryGetValue(genre.ToLowerInvariant(), out var genreId))
                {
                    genreIds.Add(genreId);
                }
            }

            // Map moods to genres
            var moodToGenreMap = GetMoodToGenreMapping();
            foreach (var mood in moods)
            {
                if (moodToGenreMap.TryGetValue(mood.ToLowerInvariant(), out var mappedGenres))
                {
                    foreach (var mappedGenre in mappedGenres)
                    {
                        if (genreMap.TryGetValue(mappedGenre, out var genreId) && !genreIds.Contains(genreId))
                        {
                            genreIds.Add(genreId);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error mapping genres/moods to IDs for media type: {MediaType}", mediaType);
        }

        return genreIds;
    }

    /// <summary>
    /// Get person IDs from names
    /// </summary>
    private async Task<List<int>> GetPersonIds(IReadOnlyList<string> people, CancellationToken cancellationToken)
    {
        var personIds = new List<int>();

        foreach (var person in people)
        {
            try
            {
                var personResult = await _tmdb.SearchPersonAsync(person, cancellationToken);
                var topPerson = personResult.Results.FirstOrDefault();
                if (topPerson != null)
                {
                    personIds.Add(topPerson.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting person ID for: {Person}", person);
            }
        }

        return personIds;
    }

    /// <summary>
    /// Enhanced mood to genre mapping
    /// </summary>
    private static Dictionary<string, string[]> GetMoodToGenreMapping()
    {
        return new Dictionary<string, string[]>
        {
            ["feel-good"] = new[] { "comedy", "family", "music", "romance" },
            ["dark"] = new[] { "thriller", "horror", "crime", "mystery" },
            ["mind-bending"] = new[] { "science fiction", "mystery", "thriller" },
            ["cozy"] = new[] { "family", "comedy", "romance" },
            ["gritty"] = new[] { "crime", "thriller", "drama", "war" },
            ["emotional"] = new[] { "drama", "romance" },
            ["action-packed"] = new[] { "action", "adventure", "thriller" },
            ["funny"] = new[] { "comedy" },
            ["scary"] = new[] { "horror", "thriller" },
            ["romantic"] = new[] { "romance" },
            ["inspiring"] = new[] { "drama", "family" },
            ["epic"] = new[] { "adventure", "fantasy", "war", "history" },
            ["mysterious"] = new[] { "mystery", "thriller", "crime" },
            ["uplifting"] = new[] { "family", "comedy", "music" },
            ["intense"] = new[] { "thriller", "action", "crime" }
        };
    }

    /// <summary>
    /// Filter exact matches by genre/mood compatibility
    /// Only include exact matches that align with the requested genres/moods
    /// </summary>
    private List<ExactMatch> FilterExactMatchesByGenreMood(List<ExactMatch> exactMatches, LlmIntent intent)
    {
        // If no specific genres/moods requested, return all exact matches
        if (!intent.Genres.Any() && !intent.Moods.Any())
            return exactMatches;

        var filteredMatches = new List<ExactMatch>();

        foreach (var match in exactMatches)
        {
            var isCompatible = IsGenreMoodCompatible(match, intent);
            if (isCompatible)
            {
                filteredMatches.Add(match);
            }
            else
            {
                _logger.LogDebug("Filtering out exact match {Title} ({Year}) - not compatible with requested genres/moods", 
                    match.Name, match.Year);
            }
        }

        return filteredMatches;
    }

    /// <summary>
    /// Check if an exact match is compatible with requested genres/moods
    /// </summary>
    private bool IsGenreMoodCompatible(ExactMatch match, LlmIntent intent)
    {
        var searchableText = $"{match.Name} {match.Overview}".ToLowerInvariant();
        
        // Check genre compatibility
        foreach (var genre in intent.Genres)
        {
            var genreKeywords = GetGenreKeywords(genre.ToLowerInvariant());
            if (genreKeywords.Any(keyword => searchableText.Contains(keyword)))
            {
                return true; // Found at least one genre match
            }
        }

        // Check mood compatibility
        foreach (var mood in intent.Moods)
        {
            var moodKeywords = GetMoodKeywords(mood.ToLowerInvariant());
            if (moodKeywords.Any(keyword => searchableText.Contains(keyword)))
            {
                return true; // Found at least one mood match
            }
        }

        // If we have specific genre/mood requirements but no matches found, not compatible
        if (intent.Genres.Any() || intent.Moods.Any())
        {
            return false;
        }

        // If no specific requirements, compatible
        return true;
    }

    /// <summary>
    /// Apply media type filtering based on user request
    /// If user asked for "movies" -> only return movies
    /// If user asked for "tv/series/shows" -> only return TV
    /// Otherwise return both
    /// </summary>
    private IEnumerable<RankedItem> ApplyMediaTypeFiltering(IReadOnlyList<RankedItem> results, LlmIntent intent)
    {
        // If no specific media types requested, return all
        if (!intent.MediaTypes.Any())
            return results;

        var requestedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var mediaType in intent.MediaTypes)
        {
            var normalizedType = NormalizeMediaType(mediaType);
            if (!string.IsNullOrEmpty(normalizedType))
            {
                requestedTypes.Add(normalizedType);
            }
        }

        // If no valid media types found, return all
        if (!requestedTypes.Any())
            return results;

        _logger.LogDebug("Filtering results by media types: {MediaTypes}", string.Join(", ", requestedTypes));

        return results.Where(item => requestedTypes.Contains(item.Hit.MediaType));
    }

    /// <summary>
    /// Normalize media type from user query to TMDB format
    /// </summary>
    private string? NormalizeMediaType(string userMediaType)
    {
        var normalized = userMediaType.ToLowerInvariant().Trim();
        
        return normalized switch
        {
            "movie" or "movies" or "film" or "films" or "cinema" => "movie",
            "tv" or "television" or "series" or "show" or "shows" or "tvshow" or "tv show" or "tv series" => "tv",
            _ => null // Unknown media type
        };
    }

    /// <summary>
    /// Get normalized media types from user intent
    /// </summary>
    private List<string> GetNormalizedMediaTypes(LlmIntent intent)
    {
        var normalizedTypes = new List<string>();
        
        foreach (var mediaType in intent.MediaTypes)
        {
            var normalized = NormalizeMediaType(mediaType);
            if (!string.IsNullOrEmpty(normalized) && !normalizedTypes.Contains(normalized))
            {
                normalizedTypes.Add(normalized);
            }
        }
        
        return normalizedTypes;
    }

    /// <summary>
    /// Get genre keywords for compatibility checking
    /// </summary>
    private static string[] GetGenreKeywords(string genre)
    {
        return genre switch
        {
            "action" => new[] { "action", "fight", "battle", "combat", "explosive", "chase", "adventure" },
            "comedy" => new[] { "comedy", "funny", "humor", "hilarious", "comic", "laugh", "amusing" },
            "drama" => new[] { "drama", "dramatic", "emotional", "character", "relationship", "family" },
            "horror" => new[] { "horror", "scary", "frightening", "terror", "haunted", "supernatural", "evil", "demon", "ghost", "zombie", "killer", "murder", "blood" },
            "thriller" => new[] { "thriller", "suspense", "tension", "intense", "psychological", "mystery", "crime", "investigation" },
            "mystery" => new[] { "mystery", "puzzle", "investigation", "detective", "clue", "secret", "unknown", "hidden" },
            "crime" => new[] { "crime", "criminal", "detective", "police", "investigation", "murder", "robbery", "gang" },
            "romance" => new[] { "romance", "romantic", "love", "relationship", "passion", "dating", "wedding" },
            "science fiction" => new[] { "sci-fi", "science fiction", "space", "alien", "future", "technology", "robot" },
            "fantasy" => new[] { "fantasy", "magic", "magical", "wizard", "dragon", "mythical", "supernatural" },
            "western" => new[] { "western", "cowboy", "frontier", "gunfighter", "outlaw" },
            "war" => new[] { "war", "battle", "military", "soldier", "combat", "conflict" },
            "documentary" => new[] { "documentary", "real", "true", "fact", "history" },
            "animation" => new[] { "animated", "cartoon", "animation" },
            "family" => new[] { "family", "kids", "children", "child" },
            "music" => new[] { "music", "musical", "song", "dance", "band" },
            "sport" => new[] { "sport", "sports", "game", "team", "competition" },
            _ => new[] { genre }
        };
    }

    /// <summary>
    /// Get mood keywords for compatibility checking
    /// </summary>
    private static string[] GetMoodKeywords(string mood)
    {
        return mood switch
        {
            "dark" => new[] { "dark", "gritty", "noir", "bleak", "sinister", "disturbing", "twisted", "evil", "shadow", "nightmare" },
            "mysterious" => new[] { "mysterious", "mystery", "enigma", "secret", "hidden", "unknown", "puzzle", "cryptic", "strange", "unexplained" },
            "sad" => new[] { "sad", "tragic", "melancholy", "depressing", "sorrow", "grief", "heartbreak", "loss", "death", "mourning" },
            "horrifying" => new[] { "horrifying", "terrifying", "frightening", "scary", "horror", "terror", "nightmare", "evil", "demon", "ghost" },
            "scary" => new[] { "scary", "frightening", "terrifying", "horror", "terror", "spine-chilling", "bone-chilling", "creepy", "eerie" },
            "chilling" => new[] { "chilling", "spine-chilling", "bone-chilling", "cold", "freezing", "icy", "haunting", "eerie", "unsettling" },
            "cold" => new[] { "cold", "icy", "frozen", "chilling", "frigid", "emotionless", "distant", "detached" },
            "suspense" => new[] { "suspense", "suspenseful", "tension", "intense", "thriller", "edge", "anticipation", "anxiety" },
            "feel-good" => new[] { "feel-good", "uplifting", "heartwarming", "positive", "inspiring", "cheerful", "happy" },
            "mind-bending" => new[] { "mind-bending", "complex", "psychological", "twist", "surreal", "confusing", "intricate" },
            "cozy" => new[] { "cozy", "comfortable", "warm", "intimate", "charming", "peaceful", "gentle" },
            "gritty" => new[] { "gritty", "raw", "realistic", "harsh", "brutal", "uncompromising", "tough" },
            "emotional" => new[] { "emotional", "touching", "moving", "tearjerker", "heart-wrenching", "dramatic" },
            "action-packed" => new[] { "action-packed", "explosive", "thrilling", "adrenaline", "fast-paced", "intense" },
            "funny" => new[] { "funny", "hilarious", "comedy", "humorous", "witty", "amusing", "laugh" },
            "romantic" => new[] { "romantic", "love", "passion", "intimate", "tender", "relationship" },
            "inspiring" => new[] { "inspiring", "motivational", "uplifting", "empowering", "triumphant", "hope" },
            _ => new[] { mood }
        };
    }

    // Conversion helper methods
    private List<SearchHit> ConvertToSearchHits(List<ExactMatch> exactMatches, string source, bool isExact = false)
    {
        return exactMatches.Select(match => new SearchHit(
            match.Id,
            match.MediaType,
            match.Name,
            match.Overview,
            match.VoteAverage,
            match.Year,
            match.PosterPath,
            new Dictionary<string, object?> { ["source"] = source, ["is_exact"] = isExact }
        )).ToList();
    }

    private List<SearchHit> ConvertPersonResultToHits(TmdbPersonResult result, string source)
    {
        var hits = new List<SearchHit>();
        foreach (var person in result.Results)
        {
            foreach (var knownFor in person.KnownFor)
            {
                hits.Add(new SearchHit(
                    knownFor.Id,
                    knownFor.MediaType,
                    knownFor.Name ?? knownFor.Title ?? "Unknown",
                    knownFor.Overview,
                    knownFor.VoteAverage,
                    ExtractYear(knownFor.ReleaseDate ?? knownFor.FirstAirDate),
                    BuildImageUrl(knownFor.PosterPath),
                    new Dictionary<string, object?> 
                    { 
                        ["source"] = source, 
                        ["person_name"] = person.Name,
                        ["person_id"] = person.Id
                    }
                ));
            }
        }
        return hits;
    }

    private List<SearchHit> ConvertDiscoverResultToHits(TmdbDiscoverResult result, string mediaType, string source)
    {
        return result.Results.Select(item => new SearchHit(
            item.Id,
            mediaType,
            item.Name ?? item.Title ?? "Unknown",
            item.Overview,
            item.VoteAverage,
            ExtractYear(item.ReleaseDate ?? item.FirstAirDate),
            BuildImageUrl(item.PosterPath),
            new Dictionary<string, object?> 
            { 
                ["source"] = source, 
                ["genre_ids"] = item.GenreIds,
                ["vote_average"] = item.VoteAverage
            }
        )).ToList();
    }

    private List<SearchHit> ConvertSimilarToHits(TmdbSimilarResult result, string mediaType, string source)
    {
        return result.Results.Select(item => new SearchHit(
            item.Id,
            mediaType,
            item.Name ?? item.Title ?? "Unknown",
            item.Overview,
            item.VoteAverage,
            ExtractYear(item.ReleaseDate ?? item.FirstAirDate),
            BuildImageUrl(item.PosterPath),
            new Dictionary<string, object?> { ["source"] = source }
        )).ToList();
    }

    private List<SearchHit> ConvertRecommendationsToHits(TmdbRecommendationsResult result, string mediaType, string source)
    {
        return result.Results.Select(item => new SearchHit(
            item.Id,
            mediaType,
            item.Name ?? item.Title ?? "Unknown",
            item.Overview,
            item.VoteAverage,
            ExtractYear(item.ReleaseDate ?? item.FirstAirDate),
            BuildImageUrl(item.PosterPath),
            new Dictionary<string, object?> { ["source"] = source }
        )).ToList();
    }

    private static int? ExtractYear(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString) || dateString.Length < 4)
            return null;

        return int.TryParse(dateString[..4], out var year) ? year : null;
    }

    private static string? BuildImageUrl(string? posterPath)
    {
        return string.IsNullOrEmpty(posterPath) ? null : $"https://image.tmdb.org/t/p/w500{posterPath}";
    }

    /// <summary>
    /// Represents an exact match found during ID resolution
    /// </summary>
    private sealed record ExactMatch(
        int Id,
        string MediaType,
        string Name,
        string? Overview,
        double? VoteAverage,
        int? Year,
        string? PosterPath
    );
}
