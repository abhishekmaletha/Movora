# FlexiSearch Query Fix Summary

## Problem Analysis

The user's query:
```json
{
  "query": "movies like medium, incantation, wailing, blair withc project, bring her back, which is dark, mysterious, sad, horrigying, scarry, chilling, cold, suspense"
}
```

**Issues Identified:**

1. **Wrong Intent Detection**: The system was treating this as an exact title lookup instead of a similarity discovery request
2. **Ignoring Genre/Mood Context**: The system found exact matches for "Medium", "Incantation", etc. but ignored the genre/mood requirements (dark, mysterious, scary, horror, etc.)
3. **Poor Ranking**: Exact title matches were getting highest scores even when they didn't match the requested horror/thriller genres

## Root Cause

The `ShouldReturnExactMatch()` logic was too simplistic and would return exact matches whenever titles were found, regardless of:
- Whether the user wanted similarity discovery ("movies like...")
- Whether additional genre/mood criteria were specified
- Whether the exact matches actually fit the requested genres/moods

## Fixes Implemented

### 1. Enhanced Intent Detection Logic

**File**: `EnhancedFlexiSearchCommandHandler.cs`

```csharp
private bool ShouldReturnExactMatch(LlmIntent intent, List<ExactMatch> exactMatches)
{
    // If user is requesting suggestions/similar content, never return exact match only
    if (intent.IsRequestingSuggestions) return false;

    // If user specified multiple titles, they likely want discovery, not exact matches
    if (intent.Titles.Count > 1) return false;

    // If user specified moods/genres along with titles, they want discovery
    if (intent.Genres.Any() || intent.Moods.Any()) return false;

    // If user specified people along with titles, they want discovery
    if (intent.People.Any()) return false;

    // Only return exact match for single, clear title requests without additional criteria
    return intent.Titles.Count == 1 && 
           !intent.Genres.Any() && 
           !intent.Moods.Any() && 
           !intent.People.Any();
}
```

**Key Changes:**
- ✅ Never return exact match if user is requesting suggestions
- ✅ Never return exact match for multiple titles (discovery intent)
- ✅ Never return exact match when genres/moods are specified
- ✅ Only return exact match for pure single title queries

### 2. Genre/Mood Compatibility Filtering

**New Method**: `FilterExactMatchesByGenreMood()`

```csharp
private List<ExactMatch> FilterExactMatchesByGenreMood(List<ExactMatch> exactMatches, LlmIntent intent)
{
    // Filter exact matches to only include those compatible with requested genres/moods
    foreach (var match in exactMatches)
    {
        var isCompatible = IsGenreMoodCompatible(match, intent);
        if (!isCompatible)
        {
            // Filter out incompatible exact matches
        }
    }
}
```

**Key Changes:**
- ✅ Filters out exact title matches that don't fit the requested genres/moods
- ✅ Only includes "Medium" or "Incantation" results if they actually match horror/thriller themes
- ✅ Prevents irrelevant exact matches from dominating results

### 3. Enhanced Genre/Mood Keyword Mapping

**New Methods**: `GetGenreKeywords()` and `GetMoodKeywords()`

```csharp
"horror" => new[] { "horror", "scary", "frightening", "terror", "haunted", "supernatural", "evil", "demon", "ghost", "zombie", "killer", "murder", "blood" }
"dark" => new[] { "dark", "gritty", "noir", "bleak", "sinister", "disturbing", "twisted", "evil", "shadow", "nightmare" }
"mysterious" => new[] { "mysterious", "mystery", "enigma", "secret", "hidden", "unknown", "puzzle", "cryptic", "strange", "unexplained" }
"scary" => new[] { "scary", "frightening", "terrifying", "horror", "terror", "spine-chilling", "bone-chilling", "creepy", "eerie" }
```

**Key Changes:**
- ✅ Comprehensive keyword mapping for horror, thriller, mystery genres
- ✅ Extensive mood keyword coverage for dark, scary, chilling, suspenseful themes
- ✅ Better text-based genre/mood detection in movie overviews

### 4. Improved Discovery Prioritization

**File**: `EnhancedFlexiSearchCommandHandler.cs`

```csharp
// For discovery queries, prioritize genre/mood-based results over exact title matches
// Only include exact matches if they fit the requested genres/moods
if (exactMatches.Any())
{
    var filteredExactMatches = FilterExactMatchesByGenreMood(exactMatches, intent);
    if (filteredExactMatches.Any())
    {
        searchHits.AddRange(ConvertToSearchHits(filteredExactMatches, "filtered_exact_match", isExact: false));
    }
}

// Execute discovery queries for similarity - this is the main source for discovery
var discoveryHits = await ExecuteDiscoverySearchAsync(intent, exactMatches, cancellationToken);
searchHits.AddRange(discoveryHits);
```

**Key Changes:**
- ✅ Prioritizes genre-based discovery over exact title matches
- ✅ Uses filtered exact matches as lower-priority candidates
- ✅ Emphasizes discovery results for similarity queries

### 5. Enhanced Ranking for Discovery Queries

**File**: `EnhancedRanker.cs`

```csharp
var baseScore = source switch
{
    "exact_match" => 3.0, // Pure exact matches (single title queries)
    "filtered_exact_match" => 1.6, // Exact matches that passed genre/mood filtering
    "tmdb_similar" => 1.9, // Higher priority for similarity
    "tmdb_recommendations" => 1.8, // Higher priority for recommendations
    "discovery" => 1.5, // Higher priority for genre-based discovery
    "quality_discovery" => 1.7, // Higher priority for high-quality discovery
    "multi_search" => 1.2, // Lower priority for discovery queries
};
```

**Key Changes:**
- ✅ Lower base scores for exact matches in discovery context
- ✅ Higher scores for genre-based discovery results
- ✅ Prioritizes TMDB similar/recommendations over exact title matches
- ✅ New "filtered_exact_match" category for genre-compatible exact matches

## Expected Behavior After Fix

### For the User's Query:
```json
{
  "query": "movies like medium, incantation, wailing, blair withc project, bring her back, which is dark, mysterious, sad, horrigying, scarry, chilling, cold, suspense"
}
```

**New Expected Results:**
1. **Horror/Thriller Movies** similar to the mentioned titles
2. **Genre-Based Discovery** prioritizing dark, mysterious, scary themes
3. **Filtered Exact Matches** only if they actually match horror/thriller genres
4. **TMDB Similar Results** from horror movies like Incantation, The Wailing
5. **Quality Rankings** with detailed "Because..." explanations

**Sample Expected Response:**
```json
{
  "results": [
    {
      "name": "The Conjuring",
      "mediaType": "movie", 
      "rating": 7.5,
      "reasoning": "Because it matches horror genre, features dark and scary themes, and has high rating (7.5/10)"
    },
    {
      "name": "Sinister",
      "mediaType": "movie",
      "rating": 6.8, 
      "reasoning": "Because it matches mysterious and horrifying mood, features supernatural themes, and similar to your search"
    }
  ]
}
```

## Key Improvements

### ✅ Intent Detection
- Properly detects similarity discovery requests
- Handles multiple titles with genre preferences
- Respects "movies like" patterns

### ✅ Genre/Mood Filtering  
- Filters exact matches by genre compatibility
- Comprehensive keyword mapping for horror/thriller themes
- Better text-based genre detection

### ✅ Discovery Prioritization
- Prioritizes genre-based discovery over exact title matches
- Uses exact matches as seeds for similarity, not primary results
- Emphasizes TMDB similar/recommendations

### ✅ Enhanced Ranking
- Lower scores for incompatible exact matches
- Higher scores for genre-matched discovery results
- Better reasoning with genre/mood explanations

The system will now properly understand that the user wants **horror/thriller movies similar to the mentioned titles**, not the exact titles themselves, and will return a curated list of dark, mysterious, scary movies that match the requested mood and themes.
