using MediatR;
using Microsoft.Extensions.Logging;
using Movora.Domain.FlexiSearch;

namespace Movora.Application.UseCases.Write.FlexiSearch;

/// <summary>
/// Handles FlexiSearch commands by orchestrating LLM intent extraction, TMDb queries, and result ranking
/// </summary>
public sealed class FlexiSearchCommandHandler : IRequestHandler<FlexiSearchCommand, FlexiSearchResponse>
{
    private readonly ILlmSearch _llm;
    private readonly ITmdbClient _tmdb;
    private readonly IRanker _ranker;
    private readonly ILogger<FlexiSearchCommandHandler> _logger;

    public FlexiSearchCommandHandler(
        ILlmSearch llm,
        ITmdbClient tmdb,
        IRanker ranker,
        ILogger<FlexiSearchCommandHandler> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _tmdb = tmdb ?? throw new ArgumentNullException(nameof(tmdb));
        _ranker = ranker ?? throw new ArgumentNullException(nameof(ranker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FlexiSearchResponse> Handle(FlexiSearchCommand request, CancellationToken cancellationToken)
    {
        if (request?.Request == null)
            throw new ArgumentNullException(nameof(request));

        var query = request.Request.Query?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            _logger.LogWarning("FlexiSearch called with empty or null query");
            return new FlexiSearchResponse { Results = Array.Empty<MovieSearchDetails>() };
        }

        _logger.LogInformation("Processing FlexiSearch query: {Query}", query);

        try
        {
            // Step 1: Extract intent using LLM
            var intent = await _llm.ExtractIntentAsync(query, cancellationToken);
            _logger.LogDebug("Extracted intent: {@Intent}", intent);

            // Step 2: Execute targeted sequential search strategy
            var searchHits = new List<SearchHit>();

            if (intent.Titles.Any())
            {
                // Strategy A: User provided specific titles
                searchHits = await ExecuteTargetedTitleSearchAsync(intent, cancellationToken);
            }
            else if (intent.Genres.Any() || intent.Moods.Any() || intent.People.Any())
            {
                // Strategy B: User provided genres/moods/people but no specific titles
                searchHits = await ExecuteGenreMoodSearchAsync(intent, cancellationToken);
            }
            else
            {
                // Strategy C: Fallback for vague queries
                _logger.LogWarning("No specific titles, genres, moods, or people found in query");
                return new FlexiSearchResponse { Results = Array.Empty<MovieSearchDetails>() };
            }

            // Step 3: Rank and merge results
            var rankedItems = _ranker.RankAndMerge(searchHits, intent);

            // Step 4: Apply requested count limit if specified
            var limitedResults = intent.RequestedCount.HasValue 
                ? rankedItems.Take(intent.RequestedCount.Value) 
                : rankedItems;

            // Step 5: Apply media type filtering and convert to response format
            var mediaTypeFilteredResults = ApplyMediaTypeFilter(limitedResults, intent.MediaTypes);
            
            var results = mediaTypeFilteredResults.Select(item => new MovieSearchDetails
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

            var totalFound = rankedItems.Count;
            var returnedCount = results.Count;
            
            if (intent.RequestedCount.HasValue && totalFound > returnedCount)
            {
                _logger.LogInformation("FlexiSearch found {TotalFound} results, returning top {ReturnedCount} as requested", 
                    totalFound, returnedCount);
            }
            else
            {
                _logger.LogInformation("FlexiSearch completed with {ResultCount} results", results.Count);
            }

            return new FlexiSearchResponse
            {
                Results = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FlexiSearch query: {Query}", query);
            
            // Return empty results rather than throwing to provide graceful degradation
            return new FlexiSearchResponse { Results = Array.Empty<MovieSearchDetails>() };
        }
    }

    private async Task<IEnumerable<SearchHit>> ExecuteDirectSearchAsync(LlmIntent intent, CancellationToken cancellationToken)
    {
        var hits = new List<SearchHit>();

        // Search for titles
        foreach (var title in intent.Titles)
        {
            try
            {
                var result = await _tmdb.SearchMultiAsync(title, cancellationToken);
                hits.AddRange(ConvertMultiResultToHits(result, "direct_title"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error searching for title: {Title}", title);
            }
        }

        // Search for people
        foreach (var person in intent.People)
        {
            try
            {
                var result = await _tmdb.SearchMultiAsync(person, cancellationToken);
                hits.AddRange(ConvertMultiResultToHits(result, "direct_person"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error searching for person: {Person}", person);
            }
        }

        return hits;
    }

    private async Task<IEnumerable<SearchHit>> ExecuteDiscoverySearchAsync(LlmIntent intent, CancellationToken cancellationToken)
    {
        var hits = new List<SearchHit>();

        foreach (var mediaType in intent.MediaTypes.Any() ? intent.MediaTypes : new[] { "movie", "tv" })
        {
            try
            {
                var genreIds = await MapGenresAndMoodsToIds(intent.Genres, intent.Moods, mediaType, cancellationToken);
                
                var discoverQuery = new DiscoverQuery(
                    mediaType,
                    genreIds,
                    intent.YearFrom,
                    intent.YearTo,
                    intent.RuntimeMaxMinutes,
                    "popularity.desc"
                );

                var result = await _tmdb.DiscoverAsync(discoverQuery, cancellationToken);
                hits.AddRange(ConvertDiscoverResultToHits(result, mediaType, "discovery"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during discovery search for media type: {MediaType}", mediaType);
            }
        }

        return hits;
    }

    private async Task<IEnumerable<TmdbTitleResult>> FindExactTitleMatchesAsync(IReadOnlyList<string> titles, CancellationToken cancellationToken)
    {
        var exactMatches = new List<TmdbTitleResult>();

        foreach (var title in titles)
        {
            try
            {
                var exactMatch = await _tmdb.FindExactTitleAsync(title, cancellationToken);
                if (exactMatch != null)
                {
                    exactMatches.Add(exactMatch);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding exact match for title: {Title}", title);
            }
        }

        return exactMatches;
    }

    private async Task<IEnumerable<SearchHit>> ExecuteRelatedSearchAsync(IEnumerable<TmdbTitleResult> exactMatches, CancellationToken cancellationToken)
    {
        var hits = new List<SearchHit>();

        foreach (var match in exactMatches)
        {
            try
            {
                // Get recommendations
                var recommendations = await _tmdb.GetRecommendationsAsync(match.MediaType, match.Id, cancellationToken);
                hits.AddRange(ConvertRecommendationsToHits(recommendations, match.MediaType, "recommendations"));

                // Get similar content
                var similar = await _tmdb.GetSimilarAsync(match.MediaType, match.Id, cancellationToken);
                hits.AddRange(ConvertSimilarToHits(similar, match.MediaType, "similar"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting related content for {MediaType} {Id}", match.MediaType, match.Id);
            }
        }

        return hits;
    }

    private async Task<List<int>> MapGenresAndMoodsToIds(IReadOnlyList<string> genres, IReadOnlyList<string> moods, string mediaType, CancellationToken cancellationToken)
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

            // Map moods to genres using heuristics
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

    private static Dictionary<string, string[]> GetMoodToGenreMapping()
    {
        return new Dictionary<string, string[]>
        {
            ["feel-good"] = new[] { "comedy", "family", "music" },
            ["dark"] = new[] { "thriller", "horror", "crime" },
            ["mind-bending"] = new[] { "science fiction", "mystery", "thriller" },
            ["cozy"] = new[] { "family", "comedy", "romance" },
            ["gritty"] = new[] { "crime", "thriller", "drama" },
            ["emotional"] = new[] { "drama", "romance" },
            ["action-packed"] = new[] { "action", "adventure" },
            ["funny"] = new[] { "comedy" },
            ["scary"] = new[] { "horror", "thriller" },
            ["romantic"] = new[] { "romance" },
            ["inspiring"] = new[] { "drama", "family" }
        };
    }

    private static IEnumerable<SearchHit> ConvertMultiResultToHits(TmdbMultiResult result, string source)
    {
        return result.Results.Select(item => new SearchHit(
            item.Id,
            item.MediaType,
            item.Name ?? item.Title ?? "Unknown",
            item.Overview,
            item.VoteAverage,
            ExtractYear(item.ReleaseDate ?? item.FirstAirDate),
            BuildImageUrl(item.PosterPath),
            new Dictionary<string, object?> { ["source"] = source }
        ));
    }

    private static IEnumerable<SearchHit> ConvertDiscoverResultToHits(TmdbDiscoverResult result, string mediaType, string source)
    {
        return result.Results.Select(item => new SearchHit(
            item.Id,
            mediaType,
            item.Name ?? item.Title ?? "Unknown",
            item.Overview,
            item.VoteAverage,
            ExtractYear(item.ReleaseDate ?? item.FirstAirDate),
            BuildImageUrl(item.PosterPath),
            new Dictionary<string, object?> { ["source"] = source, ["genre_ids"] = item.GenreIds }
        ));
    }

    private static IEnumerable<SearchHit> ConvertRecommendationsToHits(TmdbRecommendationsResult result, string mediaType, string source)
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
        ));
    }

    private static IEnumerable<SearchHit> ConvertSimilarToHits(TmdbSimilarResult result, string mediaType, string source)
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
        ));
    }

    private static IEnumerable<SearchHit> ConvertExactMatchesToHits(IEnumerable<TmdbTitleResult> exactMatches)
    {
        return exactMatches.Select(match => new SearchHit(
            match.Id,
            match.MediaType,
            match.Name,
            match.Overview,
            match.VoteAverage,
            match.Year,
            BuildImageUrl(match.PosterPath),
            new Dictionary<string, object?> { ["source"] = "exact_match", ["is_exact"] = true }
        ));
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

    private static IEnumerable<RankedItem> ApplyMediaTypeFilter(IEnumerable<RankedItem> items, IReadOnlyList<string> requestedMediaTypes)
    {
        if (!requestedMediaTypes.Any())
            return items; // No specific media type requested, return all

        return items.Where(item => requestedMediaTypes.Contains(item.Hit.MediaType, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Targeted search strategy when user provides specific titles
    /// 1. Find exact matches first
    /// 2. If requesting suggestions, extract genres from exact matches and find similar content
    /// 3. If no exact matches or requesting suggestions, do broader search
    /// </summary>
    private async Task<List<SearchHit>> ExecuteTargetedTitleSearchAsync(LlmIntent intent, CancellationToken cancellationToken)
    {
        var searchHits = new List<SearchHit>();
        
        // Step 1: Find exact title matches
        var exactMatches = await FindExactTitleMatchesAsync(intent.Titles, cancellationToken);
        
        if (exactMatches.Any())
        {
            // Add exact matches with highest priority
            var exactHits = ConvertExactMatchesToHits(exactMatches);
            searchHits.AddRange(exactHits);
            
            _logger.LogInformation("Found {Count} exact title matches", exactMatches.Count());
            
            // Step 2: If requesting suggestions, find similar content based on exact matches
            if (intent.IsRequestingSuggestions)
            {
                _logger.LogInformation("Finding similar content based on exact matches");
                
                // Extract genres from exact matches and find similar content
                var genresFromMatches = await ExtractGenresFromMatches(exactMatches, cancellationToken);
                var moods = intent.Moods.ToList(); // Use user-provided moods
                
                // Search for similar content using extracted genres + user moods
                var similarHits = await SearchBySimilarGenres(genresFromMatches, moods, intent.MediaTypes, cancellationToken);
                searchHits.AddRange(similarHits);
                
                // Also get recommendations and similar content from TMDb
                var relatedHits = await ExecuteRelatedSearchAsync(exactMatches, cancellationToken);
                searchHits.AddRange(relatedHits);
            }
            else
            {
                _logger.LogInformation("User not requesting suggestions, returning only exact matches");
            }
        }
        else
        {
            // Step 3: No exact matches found, do broader search
            _logger.LogInformation("No exact matches found, performing broader title search");
            var directHits = await ExecuteDirectSearchAsync(intent, cancellationToken);
            searchHits.AddRange(directHits);
        }
        
        return searchHits;
    }

    /// <summary>
    /// Search strategy when user provides genres/moods/people but no specific titles
    /// </summary>
    private async Task<List<SearchHit>> ExecuteGenreMoodSearchAsync(LlmIntent intent, CancellationToken cancellationToken)
    {
        var searchHits = new List<SearchHit>();
        
        _logger.LogInformation("Searching by genres/moods/people without specific titles");
        
        // Search by people first (highest relevance)
        if (intent.People.Any())
        {
            foreach (var person in intent.People)
            {
                try
                {
                    var result = await _tmdb.SearchMultiAsync(person, cancellationToken);
                    var filteredHits = FilterHitsByMediaType(ConvertMultiResultToHits(result, "direct_person"), intent.MediaTypes);
                    searchHits.AddRange(filteredHits);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error searching for person: {Person}", person);
                }
            }
        }
        
        // Search by genres and moods
        if (intent.Genres.Any() || intent.Moods.Any())
        {
            var discoveryHits = await ExecuteDiscoverySearchAsync(intent, cancellationToken);
            searchHits.AddRange(discoveryHits);
        }
        
        return searchHits;
    }

    /// <summary>
    /// Extract genres from exact TMDb matches to find similar content
    /// </summary>
    private Task<List<string>> ExtractGenresFromMatches(IEnumerable<TmdbTitleResult> exactMatches, CancellationToken cancellationToken)
    {
        var extractedGenres = new HashSet<string>();
        
        foreach (var match in exactMatches)
        {
            try
            {
                // Get detailed information for this title to extract genres
                // For now, we'll use a simple mapping based on common patterns
                // In a real implementation, you'd call TMDb's details endpoint
                var genresFromTitle = ExtractGenresFromTitleAndOverview(match.Name, match.Overview);
                foreach (var genre in genresFromTitle)
                {
                    extractedGenres.Add(genre);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting genres from match: {Title}", match.Name);
            }
        }
        
        _logger.LogInformation("Extracted genres from exact matches: {Genres}", string.Join(", ", extractedGenres));
        return Task.FromResult(extractedGenres.ToList());
    }

    /// <summary>
    /// Search for content with similar genres/moods
    /// </summary>
    private async Task<List<SearchHit>> SearchBySimilarGenres(List<string> genres, List<string> moods, IReadOnlyList<string> mediaTypes, CancellationToken cancellationToken)
    {
        var hits = new List<SearchHit>();
        
        var targetMediaTypes = mediaTypes.Any() ? mediaTypes : new[] { "movie", "tv" };
        
        foreach (var mediaType in targetMediaTypes)
        {
            try
            {
                var genreIds = await MapGenresAndMoodsToIds(genres, moods, mediaType, cancellationToken);
                
                if (genreIds.Any())
                {
                    var discoverQuery = new DiscoverQuery(
                        mediaType,
                        genreIds,
                        null, // No year constraints for similar content
                        null,
                        null, // No runtime constraints for similar content
                        "vote_average.desc" // Sort by rating for quality similar content
                    );

                    var result = await _tmdb.DiscoverAsync(discoverQuery, cancellationToken);
                    var similarHits = ConvertDiscoverResultToHits(result, mediaType, "similar_genres");
                    hits.AddRange(similarHits);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error searching similar genres for media type: {MediaType}", mediaType);
            }
        }
        
        return hits;
    }

    /// <summary>
    /// Simple genre extraction from title and overview text
    /// </summary>
    private static List<string> ExtractGenresFromTitleAndOverview(string title, string? overview)
    {
        var genres = new List<string>();
        var text = $"{title} {overview}".ToLowerInvariant();
        
        var genreKeywords = new Dictionary<string, string[]>
        {
            ["action"] = new[] { "action", "fight", "battle", "war", "combat", "spy", "agent" },
            ["comedy"] = new[] { "comedy", "funny", "humor", "laugh", "comic", "hilarious" },
            ["drama"] = new[] { "drama", "emotional", "life", "family", "relationship" },
            ["thriller"] = new[] { "thriller", "suspense", "mystery", "investigation", "crime" },
            ["horror"] = new[] { "horror", "scary", "fear", "terror", "haunted", "zombie" },
            ["romance"] = new[] { "romance", "love", "romantic", "relationship", "wedding" },
            ["science fiction"] = new[] { "sci-fi", "space", "future", "alien", "robot", "technology" },
            ["fantasy"] = new[] { "fantasy", "magic", "wizard", "dragon", "mythical", "supernatural" },
            ["adventure"] = new[] { "adventure", "journey", "quest", "treasure", "expedition" },
            ["animation"] = new[] { "animated", "cartoon", "animation" }
        };
        
        foreach (var (genre, keywords) in genreKeywords)
        {
            if (keywords.Any(keyword => text.Contains(keyword)))
            {
                genres.Add(genre);
            }
        }
        
        return genres;
    }

    /// <summary>
    /// Filter search hits by requested media types
    /// </summary>
    private static IEnumerable<SearchHit> FilterHitsByMediaType(IEnumerable<SearchHit> hits, IReadOnlyList<string> requestedMediaTypes)
    {
        if (!requestedMediaTypes.Any())
            return hits; // No specific media type requested, return all

        return hits.Where(hit => requestedMediaTypes.Contains(hit.MediaType, StringComparer.OrdinalIgnoreCase));
    }
}
