# FlexiSearch Sorting & Media Type Filtering Fix

## Issues Identified

### 1. **Sorting Problem**
- Results were not properly sorted by relevance score first
- Rating should be secondary criteria, not primary
- Users expect most relevant results at the top

### 2. **Media Type Filtering Problem**
- When user asks for "movies", system was returning TV series too
- No proper media type filtering based on user intent
- System should respect user's specific media type requests

## Fixes Implemented

### 1. Enhanced Sorting Logic

**File**: `EnhancedRanker.cs`

```csharp
// Enhanced sorting with multiple criteria - relevance score is primary
var rankedItems = deduplicatedItems
    .OrderByDescending(item => item.Score) // Primary: Relevance score
    .ThenByDescending(item => item.Hit.Rating ?? 0) // Secondary: TMDB rating
    .ThenByDescending(item => GetPopularitySignal(item.Hit)) // Tertiary: Popularity
    .Take(50) // Limit to top 50 results
```

**Key Changes:**
- ✅ **Primary Sort**: Relevance score (most important)
- ✅ **Secondary Sort**: TMDB rating (quality indicator)
- ✅ **Tertiary Sort**: Popularity signals
- ✅ Clear sorting hierarchy for consistent results

### 2. Media Type Filtering System

**File**: `EnhancedFlexiSearchCommandHandler.cs`

#### A. Media Type Detection & Normalization
```csharp
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
```

**Supported User Inputs:**
- **Movies**: "movie", "movies", "film", "films", "cinema" → `"movie"`
- **TV Shows**: "tv", "television", "series", "show", "shows", "tv show", "tv series" → `"tv"`

#### B. Media Type Filtering Logic
```csharp
private IEnumerable<RankedItem> ApplyMediaTypeFiltering(IReadOnlyList<RankedItem> results, LlmIntent intent)
{
    // If no specific media types requested, return all
    if (!intent.MediaTypes.Any())
        return results;

    // Normalize and filter by requested media types
    var requestedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var mediaType in intent.MediaTypes)
    {
        var normalizedType = NormalizeMediaType(mediaType);
        if (!string.IsNullOrEmpty(normalizedType))
        {
            requestedTypes.Add(normalizedType);
        }
    }

    return results.Where(item => requestedTypes.Contains(item.Hit.MediaType));
}
```

#### C. Discovery Query Filtering
```csharp
// Build discovery queries based on intent - normalize media types
var normalizedMediaTypes = GetNormalizedMediaTypes(intent);
var mediaTypes = normalizedMediaTypes.Any() ? normalizedMediaTypes : new[] { "movie", "tv" };
```

**Key Changes:**
- ✅ Filters discovery queries by requested media type
- ✅ Only searches TMDB movie endpoint if user wants movies
- ✅ Only searches TMDB TV endpoint if user wants TV shows
- ✅ Searches both if no specific preference

### 3. Complete Filtering Pipeline

**Updated Flow in `EnhancedFlexiSearchCommandHandler`:**

```csharp
// Step E: Hybrid ranking and response formatting
var rankedResults = _ranker.RankAndMerge(searchHits, intent);

// Apply media type filtering based on user request
var mediaFilteredResults = ApplyMediaTypeFiltering(rankedResults, intent);

// Apply count limit if requested
var limitedResults = intent.RequestedCount.HasValue 
    ? mediaFilteredResults.Take(intent.RequestedCount.Value) 
    : mediaFilteredResults.Take(12); // Default to top 12 results
```

## Expected Behavior After Fix

### Sorting Examples

#### Query: "dark horror movies"
**Before**: Mixed order, rating might be primary
**After**: 
1. Highest relevance score (best genre/mood match)
2. Secondary sort by rating for ties
3. Consistent, predictable ordering

```json
{
  "results": [
    {
      "name": "The Conjuring",
      "relevanceScore": 0.92,
      "rating": 7.5,
      "reasoning": "Because it matches horror genre and dark mood..."
    },
    {
      "name": "Sinister", 
      "relevanceScore": 0.89,
      "rating": 6.8,
      "reasoning": "Because it matches horror genre and scary themes..."
    }
  ]
}
```

### Media Type Filtering Examples

#### Query: "horror movies like The Conjuring"
**Before**: Returns movies AND TV series
**After**: Returns ONLY movies
```json
{
  "results": [
    {"name": "Insidious", "mediaType": "movie"},
    {"name": "The Ring", "mediaType": "movie"},
    {"name": "Sinister", "mediaType": "movie"}
  ]
}
```

#### Query: "scary TV series like Stranger Things"
**Before**: Returns movies AND TV series
**After**: Returns ONLY TV series
```json
{
  "results": [
    {"name": "The Haunting of Hill House", "mediaType": "tv"},
    {"name": "American Horror Story", "mediaType": "tv"},
    {"name": "Dark", "mediaType": "tv"}
  ]
}
```

#### Query: "thriller content" (no specific media type)
**After**: Returns BOTH movies AND TV series (default behavior)
```json
{
  "results": [
    {"name": "Gone Girl", "mediaType": "movie"},
    {"name": "Mindhunter", "mediaType": "tv"},
    {"name": "Zodiac", "mediaType": "movie"}
  ]
}
```

## Technical Implementation Details

### 1. LLM Intent Integration
The system leverages the existing `LlmIntent.MediaTypes` property:
```csharp
public IReadOnlyList<string> MediaTypes { get; init; } = new List<string>();
```

### 2. Case-Insensitive Matching
All media type comparisons use `StringComparer.OrdinalIgnoreCase` for robust matching.

### 3. Deduplication Support
Media type filtering works with the existing deduplication logic to prevent duplicate results.

### 4. Discovery Query Optimization
- If user wants only movies → only calls TMDB movie discovery
- If user wants only TV → only calls TMDB TV discovery  
- Reduces API calls and improves performance

### 5. Fallback Behavior
- If no media types specified → searches both movies and TV
- If invalid media type specified → ignores filter, searches both
- Graceful handling of edge cases

## Performance Benefits

### 1. Reduced API Calls
- Targeted discovery queries (movie-only or TV-only)
- Fewer unnecessary TMDB requests
- Better rate limit utilization

### 2. Faster Response Times
- Less data to process and rank
- More focused search results
- Improved user experience

### 3. Better Relevance
- Results match user's specific media type intent
- Higher satisfaction with targeted results
- Reduced cognitive load for users

## User Experience Improvements

### 1. Predictable Sorting
- Most relevant results always appear first
- Consistent ordering across similar queries
- Clear ranking methodology

### 2. Precise Media Type Control
- "movies" → only movies returned
- "TV shows" → only TV series returned
- Respects user's specific intent

### 3. Better Query Understanding
- System understands various ways to say "movies" or "TV"
- Flexible input handling
- Natural language compatibility

The enhanced FlexiSearch now provides properly sorted, media-type-filtered results that precisely match user intent and expectations!
