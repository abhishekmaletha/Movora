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

            // Step 2: Execute TMDb searches
            var searchHits = new List<SearchHit>();

            // Multi-search for direct title/people matches
            if (intent.Titles.Any() || intent.People.Any())
            {
                var directHits = await ExecuteDirectSearchAsync(intent, cancellationToken);
                searchHits.AddRange(directHits);
            }

            // Genre/mood-based discovery
            if (intent.Genres.Any() || intent.Moods.Any())
            {
                var discoveryHits = await ExecuteDiscoverySearchAsync(intent, cancellationToken);
                searchHits.AddRange(discoveryHits);
            }

            // Recommendations/similar content for exact matches
            var exactMatches = await FindExactTitleMatchesAsync(intent.Titles, cancellationToken);
            if (exactMatches.Any())
            {
                var relatedHits = await ExecuteRelatedSearchAsync(exactMatches, cancellationToken);
                searchHits.AddRange(relatedHits);
            }

            // Step 3: Rank and merge results
            var rankedItems = _ranker.RankAndMerge(searchHits, intent);

            // Step 4: Apply requested count limit if specified
            var limitedResults = intent.RequestedCount.HasValue 
                ? rankedItems.Take(intent.RequestedCount.Value) 
                : rankedItems;

            // Step 5: Convert to response format
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
}
