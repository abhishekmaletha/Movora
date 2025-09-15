using MediatR;
using Microsoft.Extensions.Logging;
using Movora.Domain.FlexiSearch;

namespace Movora.Application.UseCases.Write.FlexiSearch;

/// <summary>
/// Minimal FlexiSearch handler with deterministic, predictable behavior
/// Implements 4 simple modes: TITLE, SIMILAR, GENRE, FALLBACK
/// </summary>
public sealed class MinimalFlexiSearchCommandHandler : IRequestHandler<MinimalFlexiSearchCommand, FlexiSearchResponse>
{
    private readonly ITmdbClient _tmdb;
    private readonly ILogger<MinimalFlexiSearchCommandHandler> _logger;
    private const int DefaultLimit = 20;

    public MinimalFlexiSearchCommandHandler(
        ITmdbClient tmdb,
        ILogger<MinimalFlexiSearchCommandHandler> logger)
    {
        _tmdb = tmdb ?? throw new ArgumentNullException(nameof(tmdb));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FlexiSearchResponse> Handle(MinimalFlexiSearchCommand request, CancellationToken cancellationToken)
    {
        if (request?.Request == null)
            throw new ArgumentNullException(nameof(request));

        var query = request.Request.Query?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            _logger.LogWarning("MinimalFlexiSearch called with empty query");
            return new FlexiSearchResponse { Results = Array.Empty<MovieSearchDetails>() };
        }

        _logger.LogInformation("Processing MinimalFlexiSearch query: {Query}", query);

        try
        {
            // Step 1: Light parsing (no heavy LLM)
            var parsedQuery = ParseQuery(query);
            _logger.LogDebug("Parsed query: {@ParsedQuery}", parsedQuery);

            // Step 2: Decide the mode (exactly one)
            var mode = DetermineMode(parsedQuery);
            _logger.LogInformation("Selected mode: {Mode}", mode);

            // Step 3-6: Execute based on mode
            var results = mode switch
            {
                SearchMode.Title => await ExecuteTitleMode(parsedQuery, cancellationToken),
                SearchMode.Similar => await ExecuteSimilarMode(parsedQuery, cancellationToken),
                SearchMode.Genre => await ExecuteGenreMode(parsedQuery, cancellationToken),
                SearchMode.Fallback => await ExecuteFallbackMode(parsedQuery, cancellationToken),
                _ => Array.Empty<MovieSearchDetails>()
            };

            _logger.LogInformation("MinimalFlexiSearch completed with {ResultCount} results in {Mode} mode", 
                results.Count, mode);

            return new FlexiSearchResponse { Results = results };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MinimalFlexiSearch for query: {Query}", query);
            return new FlexiSearchResponse { Results = Array.Empty<MovieSearchDetails>() };
        }
    }

    /// <summary>
    /// Step 1: Very light parsing - extract key components without heavy LLM
    /// </summary>
    private ParsedQuery ParseQuery(string query)
    {
        var lowerQuery = query.ToLowerInvariant();
        
        // Extract seed titles (quoted parts and/or after "like/similar to")
        var seedTitles = ExtractSeedTitles(query);
        
        // Check if user is asking for similar content
        var isSimilar = lowerQuery.Contains("like") || 
                       lowerQuery.Contains("similar to") || 
                       lowerQuery.Contains("recommendations for");

        // Extract media types
        var mediaTypes = ExtractMediaTypes(lowerQuery);
        
        // Extract genres from fixed whitelist
        var genres = ExtractGenres(lowerQuery);
        
        // Extract requested count
        var requestedCount = ExtractRequestedCount(lowerQuery);

        return new ParsedQuery(seedTitles, isSimilar, mediaTypes, genres, requestedCount)
        {
            OriginalQuery = query
        };
    }

    private List<string> ExtractSeedTitles(string query)
    {
        var titles = new List<string>();
        var lowerQuery = query.ToLowerInvariant();

        // Extract quoted titles
        var quotedMatches = System.Text.RegularExpressions.Regex.Matches(query, @"""([^""]+)""");
        foreach (System.Text.RegularExpressions.Match match in quotedMatches)
        {
            titles.Add(match.Groups[1].Value.Trim());
        }

        // Extract titles after "like" or "similar to"
        var likePatterns = new[] { "like ", "similar to ", "recommendations for " };
        foreach (var pattern in likePatterns)
        {
            var index = lowerQuery.IndexOf(pattern);
            if (index >= 0)
            {
                var afterLike = query.Substring(index + pattern.Length).Trim();
                // Take everything until comma, "and", "which", or end
                var endMarkers = new[] { ",", " and ", " which ", " that " };
                foreach (var marker in endMarkers)
                {
                    var endIndex = afterLike.ToLowerInvariant().IndexOf(marker);
                    if (endIndex >= 0)
                    {
                        afterLike = afterLike.Substring(0, endIndex).Trim();
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(afterLike))
                {
                    titles.Add(afterLike);
                }
            }
        }

        // Remove duplicates and empty entries
        return titles.Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
    }

    private List<string> ExtractMediaTypes(string lowerQuery)
    {
        var mediaTypes = new List<string>();
        
        if (lowerQuery.Contains("movie") || lowerQuery.Contains("movies") || 
            lowerQuery.Contains("film") || lowerQuery.Contains("films"))
        {
            mediaTypes.Add("movie");
        }
        
        if (lowerQuery.Contains("tv") || lowerQuery.Contains("series") || 
            lowerQuery.Contains("show") || lowerQuery.Contains("shows") || 
            lowerQuery.Contains("television"))
        {
            mediaTypes.Add("tv");
        }

        // Default to both if none specified
        if (!mediaTypes.Any())
        {
            mediaTypes.AddRange(new[] { "movie", "tv" });
        }

        return mediaTypes;
    }

    private List<string> ExtractGenres(string lowerQuery)
    {
        var genreWhitelist = new[]
        {
            "action", "adventure", "animation", "comedy", "crime", "documentary", 
            "drama", "family", "fantasy", "history", "horror", "music", "mystery", 
            "romance", "sci-fi", "thriller", "war", "western"
        };

        var foundGenres = new List<string>();
        foreach (var genre in genreWhitelist)
        {
            if (lowerQuery.Contains(genre))
            {
                foundGenres.Add(genre);
            }
        }

        // Handle special cases
        if (lowerQuery.Contains("science fiction") || lowerQuery.Contains("scifi"))
        {
            if (!foundGenres.Contains("sci-fi"))
                foundGenres.Add("sci-fi");
        }

        return foundGenres.Distinct().ToList();
    }

    private int? ExtractRequestedCount(string lowerQuery)
    {
        var patterns = new[] { "top ", "best ", "give me " };
        
        foreach (var pattern in patterns)
        {
            var index = lowerQuery.IndexOf(pattern);
            if (index >= 0)
            {
                var afterPattern = lowerQuery.Substring(index + pattern.Length);
                var numberMatch = System.Text.RegularExpressions.Regex.Match(afterPattern, @"(\d+)");
                if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out var count))
                {
                    return count;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Step 2: Decide the mode (exactly one)
    /// </summary>
    private SearchMode DetermineMode(ParsedQuery parsed)
    {
        // 1. TITLE MODE: titles present AND not asking for similar
        if (parsed.SeedTitles.Any() && !parsed.IsSimilar)
            return SearchMode.Title;

        // 2. SIMILAR MODE: titles present AND asking for similar
        if (parsed.SeedTitles.Any() && parsed.IsSimilar)
            return SearchMode.Similar;

        // 3. GENRE MODE: no titles but genres present
        if (!parsed.SeedTitles.Any() && parsed.Genres.Any())
            return SearchMode.Genre;

        // 4. FALLBACK MODE: everything else
        return SearchMode.Fallback;
    }

    /// <summary>
    /// Step 3: TITLE MODE - return titles that match the names provided
    /// </summary>
    private async Task<IReadOnlyList<MovieSearchDetails>> ExecuteTitleMode(ParsedQuery parsed, CancellationToken ct)
    {
        _logger.LogDebug("Executing TITLE mode for titles: {Titles}", string.Join(", ", parsed.SeedTitles));

        var results = new List<MovieSearchDetails>();

        foreach (var title in parsed.SeedTitles)
        {
            try
            {
                // Try exact first
                var exactMatch = await _tmdb.FindExactTitleAsync(title, ct);
                if (exactMatch != null)
                {
                    results.Add(ConvertToMovieSearchDetails(exactMatch, "Exact title match"));
                    continue;
                }

                // Fallback to search
                var searchTasks = new List<Task>();
                var movieResults = new List<MovieSearchDetails>();
                var tvResults = new List<MovieSearchDetails>();

                if (parsed.MediaTypes.Contains("movie"))
                {
                    searchTasks.Add(Task.Run(async () =>
                    {
                        var movieSearch = await _tmdb.SearchMovieAsync(title, ct: ct);
                        var topMovie = movieSearch.Results.OrderByDescending(m => m.VoteAverage ?? 0).FirstOrDefault();
                        if (topMovie != null)
                        {
                            movieResults.Add(new MovieSearchDetails
                            {
                                TmdbId = topMovie.Id,
                                Name = topMovie.Title,
                                MediaType = "movie",
                                Rating = topMovie.VoteAverage,
                                Overview = topMovie.Overview,
                                Year = ExtractYear(topMovie.ReleaseDate),
                                ThumbnailUrl = BuildImageUrl(topMovie.PosterPath),
                                RelevanceScore = 1.0,
                                Reasoning = "Title search match"
                            });
                        }
                    }));
                }

                if (parsed.MediaTypes.Contains("tv"))
                {
                    searchTasks.Add(Task.Run(async () =>
                    {
                        var tvSearch = await _tmdb.SearchTvAsync(title, ct);
                        var topTv = tvSearch.Results.OrderByDescending(t => t.VoteAverage ?? 0).FirstOrDefault();
                        if (topTv != null)
                        {
                            tvResults.Add(new MovieSearchDetails
                            {
                                TmdbId = topTv.Id,
                                Name = topTv.Name,
                                MediaType = "tv",
                                Rating = topTv.VoteAverage,
                                Overview = topTv.Overview,
                                Year = ExtractYear(topTv.FirstAirDate),
                                ThumbnailUrl = BuildImageUrl(topTv.PosterPath),
                                RelevanceScore = 1.0,
                                Reasoning = "Title search match"
                            });
                        }
                    }));
                }

                await Task.WhenAll(searchTasks);
                results.AddRange(movieResults);
                results.AddRange(tvResults);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error searching for title: {Title}", title);
            }
        }

        // Dedupe and limit
        var deduped = DeduplicateResults(results);
        var sorted = SortResults(deduped);
        var limited = LimitResults(sorted, parsed.RequestedCount);

        return limited;
    }

    /// <summary>
    /// Step 4: SIMILAR MODE - return titles similar to the named seed(s)
    /// </summary>
    private async Task<IReadOnlyList<MovieSearchDetails>> ExecuteSimilarMode(ParsedQuery parsed, CancellationToken ct)
    {
        _logger.LogDebug("Executing SIMILAR mode for titles: {Titles}", string.Join(", ", parsed.SeedTitles));

        var results = new List<MovieSearchDetails>();

        foreach (var title in parsed.SeedTitles)
        {
            try
            {
                // Resolve single seed (prefer movie if ambiguous)
                TmdbTitleResult? seed = await _tmdb.FindExactTitleAsync(title, ct);
                
                if (seed == null)
                {
                    // Fallback search - prefer movie
                    var movieSearch = await _tmdb.SearchMovieAsync(title, ct: ct);
                    var topMovie = movieSearch.Results.OrderByDescending(m => m.VoteAverage ?? 0).FirstOrDefault();
                    
                    if (topMovie != null)
                    {
                        seed = new TmdbTitleResult(topMovie.Id, "movie", topMovie.Title, 
                            topMovie.Overview, topMovie.VoteAverage, ExtractYear(topMovie.ReleaseDate), topMovie.PosterPath);
                    }
                    else
                    {
                        // Try TV as fallback
                        var tvSearch = await _tmdb.SearchTvAsync(title, ct);
                        var topTv = tvSearch.Results.OrderByDescending(t => t.VoteAverage ?? 0).FirstOrDefault();
                        if (topTv != null)
                        {
                            seed = new TmdbTitleResult(topTv.Id, "tv", topTv.Name, 
                                topTv.Overview, topTv.VoteAverage, ExtractYear(topTv.FirstAirDate), topTv.PosterPath);
                        }
                    }
                }

                if (seed == null)
                {
                    _logger.LogWarning("Could not resolve seed title: {Title}", title);
                    continue;
                }

                _logger.LogDebug("Resolved seed: {Title} ({MediaType}, ID: {Id})", seed.Name, seed.MediaType, seed.Id);

                // Get similar and recommendations
                var similarTask = _tmdb.GetSimilarAsync(seed.MediaType, seed.Id, ct);
                var recsTask = _tmdb.GetRecommendationsAsync(seed.MediaType, seed.Id, ct);

                await Task.WhenAll(similarTask, recsTask);

                var similar = await similarTask;
                var recommendations = await recsTask;

                // Convert similar results
                foreach (var item in similar.Results)
                {
                    results.Add(new MovieSearchDetails
                    {
                        TmdbId = item.Id,
                        Name = item.Name ?? item.Title ?? "Unknown",
                        MediaType = seed.MediaType,
                        Rating = item.VoteAverage,
                        Overview = item.Overview,
                        Year = ExtractYear(item.ReleaseDate ?? item.FirstAirDate),
                        ThumbnailUrl = BuildImageUrl(item.PosterPath),
                        RelevanceScore = 0.9,
                        Reasoning = $"Similar to {seed.Name}"
                    });
                }

                // Convert recommendation results
                foreach (var item in recommendations.Results)
                {
                    results.Add(new MovieSearchDetails
                    {
                        TmdbId = item.Id,
                        Name = item.Name ?? item.Title ?? "Unknown",
                        MediaType = seed.MediaType,
                        Rating = item.VoteAverage,
                        Overview = item.Overview,
                        Year = ExtractYear(item.ReleaseDate ?? item.FirstAirDate),
                        ThumbnailUrl = BuildImageUrl(item.PosterPath),
                        RelevanceScore = 0.8,
                        Reasoning = $"Recommended based on {seed.Name}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting similar content for title: {Title}", title);
            }
        }

        // Apply media type filtering if user specified (e.g., "movies like...")
        if (parsed.MediaTypes.Count == 1)
        {
            results = results.Where(r => r.MediaType == parsed.MediaTypes[0]).ToList();
        }

        // Quality gate: drop low-rated items
        results = results.Where(r => r.Rating >= 6.0).ToList();

        // Dedupe and limit
        var deduped = DeduplicateResults(results);
        var sorted = SortResults(deduped);
        var limited = LimitResults(sorted, parsed.RequestedCount);

        return limited;
    }

    /// <summary>
    /// Step 5: GENRE MODE - return titles that strongly match genres
    /// </summary>
    private async Task<IReadOnlyList<MovieSearchDetails>> ExecuteGenreMode(ParsedQuery parsed, CancellationToken ct)
    {
        _logger.LogDebug("Executing GENRE mode for genres: {Genres}", string.Join(", ", parsed.Genres));

        var results = new List<MovieSearchDetails>();

        foreach (var mediaType in parsed.MediaTypes)
        {
            try
            {
                // Get genre map
                var genreMap = await _tmdb.GetGenreMapAsync(mediaType, ct);
                var genreIds = new List<int>();

                foreach (var genre in parsed.Genres)
                {
                    if (genreMap.TryGetValue(genre, out var genreId))
                    {
                        genreIds.Add(genreId);
                    }
                }

                if (!genreIds.Any())
                {
                    _logger.LogWarning("No valid genre IDs found for {MediaType}", mediaType);
                    continue;
                }

                // Client-side AND: discover per genre and intersect
                var genreResults = new List<List<int>>();
                
                foreach (var genreId in genreIds)
                {
                    var query = new DiscoverQuery(
                        MediaType: mediaType,
                        GenreIds: new[] { genreId },
                        VoteCountGte: 100, // Quality filter
                        SortBy: "vote_average.desc"
                    );

                    var discoverResult = await _tmdb.DiscoverAsync(query, ct);
                    var ids = discoverResult.Results.Select(r => r.Id).ToList();
                    genreResults.Add(ids);
                }

                // Intersect all genre results for strong matching
                var intersection = genreResults.Aggregate((prev, current) => prev.Intersect(current).ToList());
                
                // If empty intersection, fallback to single discovery with all genres
                if (!intersection.Any())
                {
                    _logger.LogDebug("No intersection found, falling back to combined genre discovery");
                    var fallbackQuery = new DiscoverQuery(
                        MediaType: mediaType,
                        GenreIds: genreIds,
                        VoteCountGte: 100,
                        SortBy: "vote_average.desc"
                    );

                    var fallbackResult = await _tmdb.DiscoverAsync(fallbackQuery, ct);
                    intersection = fallbackResult.Results.Select(r => r.Id).ToList();
                }

                // Get full details for intersected IDs (we'll use the discovery results we have)
                var finalQuery = new DiscoverQuery(
                    MediaType: mediaType,
                    GenreIds: genreIds,
                    VoteCountGte: 100,
                    SortBy: "vote_average.desc"
                );

                var finalResult = await _tmdb.DiscoverAsync(finalQuery, ct);
                var filteredResults = finalResult.Results.Where(r => intersection.Contains(r.Id));

                foreach (var item in filteredResults)
                {
                    results.Add(new MovieSearchDetails
                    {
                        TmdbId = item.Id,
                        Name = item.Name ?? item.Title ?? "Unknown",
                        MediaType = mediaType,
                        Rating = item.VoteAverage,
                        Overview = item.Overview,
                        Year = ExtractYear(item.ReleaseDate ?? item.FirstAirDate),
                        ThumbnailUrl = BuildImageUrl(item.PosterPath),
                        RelevanceScore = 1.0,
                        Reasoning = $"Strong match for {string.Join(", ", parsed.Genres)} genres"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in genre discovery for {MediaType}", mediaType);
            }
        }

        // Dedupe and limit
        var deduped = DeduplicateResults(results);
        var sorted = SortResults(deduped);
        var limited = LimitResults(sorted, parsed.RequestedCount);

        return limited;
    }

    /// <summary>
    /// Step 6: FALLBACK MODE - plain text search
    /// </summary>
    private async Task<IReadOnlyList<MovieSearchDetails>> ExecuteFallbackMode(ParsedQuery parsed, CancellationToken ct)
    {
        _logger.LogDebug("Executing FALLBACK mode");

        try
        {
            var multiResult = await _tmdb.SearchMultiAsync(parsed.OriginalQuery, ct);
            var results = new List<MovieSearchDetails>();

            foreach (var item in multiResult.Results.Where(r => r.MediaType == "movie" || r.MediaType == "tv"))
            {
                // Apply media type filter if specified
                if (parsed.MediaTypes.Any() && !parsed.MediaTypes.Contains(item.MediaType))
                    continue;

                results.Add(new MovieSearchDetails
                {
                    TmdbId = item.Id,
                    Name = item.Name ?? item.Title ?? "Unknown",
                    MediaType = item.MediaType,
                    Rating = item.VoteAverage,
                    Overview = item.Overview,
                    Year = ExtractYear(item.ReleaseDate ?? item.FirstAirDate),
                    ThumbnailUrl = BuildImageUrl(item.PosterPath),
                    RelevanceScore = 0.7,
                    Reasoning = "Text search match"
                });
            }

            // Dedupe and limit
            var deduped = DeduplicateResults(results);
            var sorted = SortResults(deduped);
            var limited = LimitResults(sorted, parsed.RequestedCount);

            return limited;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback search");
            return Array.Empty<MovieSearchDetails>();
        }
    }

    /// <summary>
    /// Step 7: Ranking - keep it dumb & predictable
    /// Primary: vote_average desc, Secondary: popularity desc, Tertiary: year desc
    /// </summary>
    private List<MovieSearchDetails> SortResults(List<MovieSearchDetails> results)
    {
        return results
            .OrderByDescending(r => r.Rating ?? 0) // Primary: vote_average desc
            .ThenByDescending(r => r.RelevanceScore) // Secondary: relevance/popularity
            .ThenByDescending(r => r.Year ?? 0) // Tertiary: year desc (newer first)
            .ToList();
    }

    /// <summary>
    /// Step 8: Hygiene - dedupe by TMDB ID
    /// </summary>
    private List<MovieSearchDetails> DeduplicateResults(List<MovieSearchDetails> results)
    {
        return results
            .GroupBy(r => new { r.TmdbId, r.MediaType })
            .Select(g => g.OrderByDescending(r => r.Rating ?? 0).First())
            .ToList();
    }

    private IReadOnlyList<MovieSearchDetails> LimitResults(List<MovieSearchDetails> results, int? requestedCount)
    {
        var limit = requestedCount ?? DefaultLimit;
        return results.Take(limit).ToList();
    }

    // Helper methods
    private MovieSearchDetails ConvertToMovieSearchDetails(TmdbTitleResult title, string reasoning)
    {
        return new MovieSearchDetails
        {
            TmdbId = title.Id,
            Name = title.Name,
            MediaType = title.MediaType,
            Rating = title.VoteAverage,
            Overview = title.Overview,
            Year = title.Year,
            ThumbnailUrl = title.PosterPath,
            RelevanceScore = 1.0,
            Reasoning = reasoning
        };
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

    // Data structures
    private enum SearchMode
    {
        Title,    // Exact/partial title search
        Similar,  // Seed → similar/recs
        Genre,    // Strong genre match
        Fallback  // Plain text search
    }

    private sealed record ParsedQuery(
        List<string> SeedTitles,
        bool IsSimilar,
        List<string> MediaTypes,
        List<string> Genres,
        int? RequestedCount)
    {
        public string OriginalQuery { get; init; } = "";
    }
}
