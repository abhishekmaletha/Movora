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

            // Step 2: Create requested genres list
            var requestedGenres = new List<int>();

            // Step 3: If titles are provided, search them and extract their genres
            if (intent.Titles.Any())
            {
                var genresFromTitles = await ExtractGenresFromTitlesAsync(intent.Titles, intent.MediaTypes, cancellationToken);
                requestedGenres.AddRange(genresFromTitles);
                _logger.LogInformation("Extracted {Count} genres from provided titles", genresFromTitles.Count);
            }

            // Step 4: If requestedGenres is empty, extract from user's genres/moods
            if (!requestedGenres.Any())
            {
                var genresFromIntent = await ExtractGenresFromIntentAsync(intent, cancellationToken);
                requestedGenres.AddRange(genresFromIntent);
                _logger.LogInformation("Extracted {Count} genres from user intent", genresFromIntent.Count);
            }

            // Step 5: If still no genres, return empty results
            if (!requestedGenres.Any())
            {
                _logger.LogWarning("No genres could be determined from query");
                return new FlexiSearchResponse { Results = Array.Empty<MovieSearchDetails>() };
            }

            // Step 6: Find top 100 movies matching requested genres
            var searchHits = await FindTop100MoviesForGenresAsync(requestedGenres, intent.MediaTypes, cancellationToken);
            
            // Step 7: Rank and sort based on description match, rating, and year
            var rankedItems = RankByDescriptionRatingYear(searchHits, intent);

            // Step 8: Apply requested count limit if specified
            var limitedResults = intent.RequestedCount.HasValue 
                ? rankedItems.Take(intent.RequestedCount.Value) 
                : rankedItems;

            // Step 9: Convert to response format
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

            _logger.LogInformation("FlexiSearch completed with {ResultCount} results", results.Count);

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

    /// <summary>
    /// Simple genre extraction from title and overview text
    /// </summary>
    private static List<string> ExtractGenresFromTitleAndOverview(string title, string? overview)
    {
        var genres = new List<string>();
        var text = $"{title} {overview}".ToLowerInvariant();
        
        var genreKeywords = new Dictionary<string, string[]>
        {
            ["action"] = new[] { "action", "fight", "battle", "war", "combat", "spy", "agent", "martial arts", "gun", "explosion", "assassin", "soldier" },
            ["adventure"] = new[] { "adventure", "journey", "quest", "treasure", "expedition", "explore", "survival", "voyage" },
            ["animation"] = new[] { "animated", "cartoon", "animation", "pixar", "disney", "stop-motion", "cgi" },
            ["biography"] = new[] { "biopic", "based on true story", "real life", "inspired by", "historical figure" },
            ["comedy"] = new[] { "comedy", "funny", "humor", "laugh", "comic", "hilarious", "parody", "satire", "rom-com" },
            ["crime"] = new[] { "crime", "gangster", "mafia", "heist", "detective", "mob", "law", "courtroom", "trial" },
            ["documentary"] = new[] { "documentary", "true story", "docuseries", "nonfiction", "based on real events" },
            ["drama"] = new[] { "drama", "emotional", "life", "family", "relationship", "tragedy", "society", "struggle", "character-driven" },
            ["fantasy"] = new[] { "fantasy", "magic", "wizard", "dragon", "mythical", "supernatural", "sorcery", "fairy tale", "elves", "orcs" },
            ["historical"] = new[] { "historical", "period drama", "medieval", "ancient", "renaissance", "victorian", "war history" },
            ["horror"] = new[] { "horror", "scary", "fear", "terror", "haunted", "zombie", "ghost", "vampire", "slasher", "monster", "demon", "possession" },
            ["musical"] = new[] { "musical", "songs", "dance", "singing", "broadway", "opera" },
            ["romance"] = new[] { "romance", "love", "romantic", "relationship", "wedding", "affair", "heartbreak", "valentine", "rom-com" },
            ["science fiction"] = new[] { "sci-fi", "science fiction", "space", "future", "alien", "robot", "technology", "dystopian", "cyberpunk", "time travel" },
            ["sports"] = new[] { "sports", "football", "soccer", "basketball", "cricket", "baseball", "athlete", "team", "tournament" },
            ["thriller"] = new[] { "thriller", "suspense", "mystery", "investigation", "crime", "psychological", "dark", "chase", "conspiracy" },
            ["war"] = new[] { "war", "military", "battlefield", "soldier", "army", "world war", "veteran", "conflict" },
            ["western"] = new[] { "western", "cowboy", "gunslinger", "sheriff", "saloon", "duel", "outlaw", "frontier" }
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

    /// <summary>
    /// Extract genre IDs from provided titles by searching TMDb and getting their detailed information
    /// </summary>
    private async Task<List<int>> ExtractGenresFromTitlesAsync(IReadOnlyList<string> titles, IReadOnlyList<string> mediaTypes, CancellationToken cancellationToken)
    {
        var genreIds = new HashSet<int>();
        
        foreach (var title in titles)
        {
            try
            {
                // Search for the title to get basic info
                var searchResult = await _tmdb.SearchMultiAsync(title, cancellationToken);
                
                // For each result, get detailed information with actual genres from TMDb
                foreach (var result in searchResult.Results.Take(3)) // Limit to top 3 matches per title
                {
                    try
                    {
                        // Filter by requested media types if specified
                        var targetMediaTypes = mediaTypes.Any() ? mediaTypes : new[] { "movie", "tv" };
                        if (!targetMediaTypes.Contains(result.MediaType, StringComparer.OrdinalIgnoreCase))
                            continue;

                        // Get detailed information based on media type
                        if (result.MediaType.Equals("movie", StringComparison.OrdinalIgnoreCase))
                        {
                            var movieDetails = await _tmdb.GetMovieDetailsAsync(result.Id, cancellationToken);
                            if (movieDetails?.Genres != null)
                            {
                                foreach (var genre in movieDetails.Genres)
                                {
                                    genreIds.Add(genre.Id);
                                }
                                _logger.LogDebug("Extracted {Count} genres from movie: {Title}", movieDetails.Genres.Count, movieDetails.Title);
                            }
                        }
                        else if (result.MediaType.Equals("tv", StringComparison.OrdinalIgnoreCase))
                        {
                            var tvDetails = await _tmdb.GetTvDetailsAsync(result.Id, cancellationToken);
                            if (tvDetails?.Genres != null)
                            {
                                foreach (var genre in tvDetails.Genres)
                                {
                                    genreIds.Add(genre.Id);
                                }
                                _logger.LogDebug("Extracted {Count} genres from TV series: {Name}", tvDetails.Genres.Count, tvDetails.Name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error getting details for {MediaType} ID {Id}: {Title}", 
                            result.MediaType, result.Id, result.Name ?? result.Title);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error searching for title: {Title}", title);
            }
        }
        
        _logger.LogInformation("Extracted {Count} unique genre IDs from {TitleCount} titles", genreIds.Count, titles.Count);
        return genreIds.ToList();
    }

    /// <summary>
    /// Extract genre IDs from user's genres and moods in the intent
    /// </summary>
    private async Task<List<int>> ExtractGenresFromIntentAsync(LlmIntent intent, CancellationToken cancellationToken)
    {
        var genreIds = new List<int>();
        var targetMediaTypes = intent.MediaTypes.Any() ? intent.MediaTypes : new[] { "movie", "tv" };
        
        foreach (var mediaType in targetMediaTypes)
        {
            try
            {
                var genreIdsForMediaType = await MapGenresAndMoodsToIds(intent.Genres, intent.Moods, mediaType, cancellationToken);
                genreIds.AddRange(genreIdsForMediaType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting genres from intent for media type: {MediaType}", mediaType);
            }
        }
        
        // Remove duplicates
        return genreIds.Distinct().ToList();
    }

    /// <summary>
    /// Find top 100 movies/TV shows matching the requested genres
    /// </summary>
    private async Task<List<SearchHit>> FindTop100MoviesForGenresAsync(List<int> requestedGenres, IReadOnlyList<string> mediaTypes, CancellationToken cancellationToken)
    {
        var searchHits = new List<SearchHit>();
        var targetMediaTypes = mediaTypes.Any() ? mediaTypes : new[] { "movie", "tv" };
        
        foreach (var mediaType in targetMediaTypes)
        {
            try
            {
                // Create multiple discover queries with different sorting to get variety
                var discoverQueries = new[]
                {
                    new DiscoverQuery(
                        MediaType: mediaType,
                        GenreIds: requestedGenres,
                        SortBy: "popularity.desc",
                        VoteCountGte: 100
                    ),
                    new DiscoverQuery(
                        MediaType: mediaType,
                        GenreIds: requestedGenres,
                        SortBy: "vote_average.desc",
                        VoteCountGte: 500
                    ),
                    new DiscoverQuery(
                        MediaType: mediaType,
                        GenreIds: requestedGenres,
                        SortBy: "release_date.desc",
                        VoteCountGte: 50
                    )
                };

                foreach (var query in discoverQueries)
                {
                    try
                    {
                        var result = await _tmdb.DiscoverAsync(query, cancellationToken);
                        var hits = ConvertDiscoverResultToHits(result, mediaType, $"genre_discovery_{query.SortBy}");
                        searchHits.AddRange(hits);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error in discover query for {MediaType} with sort {SortBy}", mediaType, query.SortBy);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding movies for genres in media type: {MediaType}", mediaType);
            }
        }
        
        // Remove duplicates based on TMDb ID and media type
        var uniqueHits = searchHits
            .GroupBy(hit => new { hit.TmdbId, hit.MediaType })
            .Select(group => group.First())
            .Take(100)
            .ToList();
        
        _logger.LogInformation("Found {Count} unique movies/shows for requested genres", uniqueHits.Count);
        return uniqueHits;
    }

    /// <summary>
    /// Rank results by description match, then rating, then year
    /// </summary>
    private List<RankedItem> RankByDescriptionRatingYear(List<SearchHit> searchHits, LlmIntent intent)
    {
        var queryText = string.Join(" ", intent.Titles.Concat(intent.Genres).Concat(intent.Moods).Concat(intent.People)).ToLowerInvariant();
        
        return searchHits.Select(hit => 
        {
            var score = CalculateDescriptionMatchScore(hit, queryText, intent);
            var reasoning = BuildReasoning(hit, score, intent);
            
            return new RankedItem(hit, score, reasoning);
        })
        .OrderByDescending(item => item.Score) // Description match score (primary)
        .ThenByDescending(item => item.Hit.Rating ?? 0) // Rating (secondary)
        .ThenByDescending(item => item.Hit.Year ?? 0) // Year (tertiary)
        .ToList();
    }

    /// <summary>
    /// Calculate description match score based on query terms found in title/overview
    /// </summary>
    private static double CalculateDescriptionMatchScore(SearchHit hit, string queryText, LlmIntent intent)
    {
        var score = 0.0;
        var searchableText = $"{hit.Name} {hit.Overview}".ToLowerInvariant();
        
        // Base score from rating (normalized to 0-1)
        score += (hit.Rating ?? 0) / 10.0;
        
        // Bonus for year recency (movies from last 20 years get bonus)
        if (hit.Year.HasValue && hit.Year.Value >= DateTime.Now.Year - 20)
        {
            score += 0.2;
        }
        
        // Text matching bonuses
        var queryWords = queryText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matchedWords = queryWords.Count(word => word.Length > 2 && searchableText.Contains(word));
        score += (double)matchedWords / Math.Max(queryWords.Length, 1) * 2.0; // Up to 2 points for text matches
        
        // Genre matching bonus (if we can determine genres from signals)
        if (hit.Signals.TryGetValue("genre_ids", out var genreIdsObj) && genreIdsObj is int[] genreIds)
        {
            // This would require mapping back from genre IDs to names for comparison
            // For now, we'll give a small bonus for having genre metadata
            score += 0.1;
        }
        
        return score;
    }

    /// <summary>
    /// Build reasoning text for why this result was ranked as it was
    /// </summary>
    private static string BuildReasoning(SearchHit hit, double score, LlmIntent intent)
    {
        var reasons = new List<string>();
        
        if (hit.Rating.HasValue && hit.Rating.Value >= 7.0)
        {
            reasons.Add($"High rating ({hit.Rating.Value:F1})");
        }
        
        if (hit.Year.HasValue && hit.Year.Value >= DateTime.Now.Year - 5)
        {
            reasons.Add("Recent release");
        }
        
        var searchableText = $"{hit.Name} {hit.Overview}".ToLowerInvariant();
        var queryTerms = string.Join(" ", intent.Titles.Concat(intent.Genres).Concat(intent.Moods)).ToLowerInvariant();
        var queryWords = queryTerms.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matchedWords = queryWords.Count(word => word.Length > 2 && searchableText.Contains(word));
        
        if (matchedWords > 0)
        {
            reasons.Add($"Matches {matchedWords} query terms");
        }
        
        if (hit.Signals.TryGetValue("source", out var source))
        {
            reasons.Add($"Found via {source}");
        }
        
        return reasons.Any() ? string.Join(", ", reasons) : "General match";
    }
}
