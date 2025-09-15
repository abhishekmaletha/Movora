# Minimal FlexiSearch Implementation

## 🎯 Mission: Dead-Simple, Deterministic Search

Following your specification, I've implemented a **minimal, predictable FlexiSearch** that eliminates overengineering and delivers exactly what users expect.

## 🏗️ Architecture: 4 Simple Modes

### Mode Selection Logic
```
1. TITLE MODE: seedTitles present AND isSimilar == false
   → Return titles that match the names provided

2. SIMILAR MODE: seedTitles present AND isSimilar == true  
   → Return titles similar to the named seed(s)

3. GENRE MODE: no seedTitles, but genres present
   → Return titles that strongly match genres

4. FALLBACK MODE: none of the above
   → Run simple multi-search and return top titles
```

## 📝 Step 1: Very Light Parsing (No Heavy LLM)

### Extracted Components
```csharp
private sealed record ParsedQuery(
    List<string> SeedTitles,      // Quoted titles + after "like/similar to"
    bool IsSimilar,               // Contains "like", "similar to", "recommendations for"
    List<string> MediaTypes,      // ["movie"], ["tv"], or ["movie","tv"]
    List<string> Genres,          // From fixed whitelist only
    int? RequestedCount           // Number after "top", "best", "give me N"
);
```

### Genre Whitelist (Fixed)
```
action, adventure, animation, comedy, crime, documentary, 
drama, family, fantasy, history, horror, music, mystery, 
romance, sci-fi, thriller, war, western
```

### Parsing Examples
```
"movies like la la land" 
→ seedTitles: ["la la land"], isSimilar: true, mediaTypes: ["movie"]

"interstellar" 
→ seedTitles: ["interstellar"], isSimilar: false, mediaTypes: ["movie","tv"]

"gritty crime dramas" 
→ seedTitles: [], genres: ["crime","drama"], mediaTypes: ["movie","tv"]
```

## 🎬 Mode Implementations

### 1. TITLE MODE - Exact/Partial Title Search
```csharp
for each title in seedTitles:
    1. Try FindExactTitleAsync(title)
    2. If null → SearchMovieAsync(title) + SearchTvAsync(title)
    3. Pick top 1 per mediaType by vote_average desc
    4. Filter by mediaTypes if specified
    5. Dedupe by TMDB ID, limit to requestedCount
```

**Returns**: Only matching titles (no recommendations)

### 2. SIMILAR MODE - Seed → Similar/Recs
```csharp
for each title in seedTitles:
    1. Resolve single seed (prefer movie if ambiguous)
    2. GetSimilarAsync(seed.MediaType, seed.Id)
    3. GetRecommendationsAsync(seed.MediaType, seed.Id)
    4. Merge results, dedupe by ID
    5. Quality gate: drop items with vote_average < 6.0
    6. Force mediaTypes if user said "movies like..." → ["movie"]
```

**Returns**: Only similar/recommended titles (no unrelated search results)

### 3. GENRE MODE - Strong Genre Match
```csharp
for each mediaType:
    1. GetGenreMapAsync(mediaType) → map user genres to IDs
    2. Client-side AND: DiscoverAsync once per genre ID
    3. Intersect the ID sets locally for strong matching
    4. If empty intersection → fallback to combined genre discovery
    5. Sort by vote_average desc
```

**Returns**: Only genre-matched titles with strong overlap

### 4. FALLBACK MODE - Plain Text Search
```csharp
1. SearchMultiAsync(query)
2. Keep only movie/tv results
3. Filter by mediaTypes if specified
4. Sort by vote_average desc
```

**Returns**: Only top verbatim matches

## 📊 Ranking: Dumb & Predictable

```csharp
.OrderByDescending(r => r.Rating ?? 0)        // Primary: vote_average desc
.ThenByDescending(r => r.RelevanceScore)      // Secondary: relevance/popularity  
.ThenByDescending(r => r.Year ?? 0)           // Tertiary: year desc (newer first)
```

**No ML, no custom scoring. Deterministic > clever.**

## 🔧 Implementation Details

### File Structure
- **Handler**: `MinimalFlexiSearchCommandHandler.cs`
- **Endpoint**: `POST /api/search/minimal`
- **Registration**: Added to `ServiceCollectionExtensions.cs`

### Key Features
✅ **Light parsing** with regex/keywords (no heavy LLM)  
✅ **Fixed genre whitelist** prevents hallucination  
✅ **Mode-based execution** - never mix modes in one response  
✅ **Client-side AND** for strong genre matching  
✅ **Quality gates** (vote_average ≥ 6.0 for similar mode)  
✅ **Deterministic ranking** by rating → relevance → year  
✅ **Strict media type filtering**  
✅ **Deduplication** by TMDB ID  
✅ **Default limit** of 20 results  

### Hygiene Rules
- ✅ Never mix modes in one response
- ✅ Dedupe by TMDB ID across all sources  
- ✅ Normalize titles when comparing (trim, case-insensitive)
- ✅ Honor mediaTypes strictly
- ✅ Apply requestedCount if present, else default to 20

## 🧪 Concrete Examples

### Example 1: "movies like la la land"
```
Mode: SIMILAR
Parse → seedTitles: ["la la land"], isSimilar: true, mediaTypes: ["movie"]
Flow → resolve seed movie → GetSimilarAsync + GetRecommendationsAsync → dedupe → limit
Expected: Only movies similar to La La Land (no random stuff)
```

### Example 2: "interstellar"  
```
Mode: TITLE
Parse → seedTitles: ["interstellar"], isSimilar: false
Flow → FindExactTitleAsync → return matching title only
Expected: Just the Interstellar movie, no recommendations
```

### Example 3: "gritty crime dramas"
```
Mode: GENRE  
Parse → genres: ["crime","drama"]
Flow → discover per-genre → client-side intersect → sort by rating
Expected: Only movies/shows that are BOTH crime AND drama
```

### Example 4: "top 5 horror movies"
```
Mode: GENRE
Parse → genres: ["horror"], mediaTypes: ["movie"], requestedCount: 5
Flow → discover horror movies → sort by rating → limit to 5
Expected: Top 5 highest-rated horror movies
```

## 🚀 API Usage

### New Minimal Endpoint
```http
POST /api/search/minimal
Content-Type: application/json

{
  "query": "movies like la la land"
}
```

### Response Format (Same as existing)
```json
{
  "results": [
    {
      "tmdbId": 123,
      "name": "Whiplash",
      "mediaType": "movie",
      "rating": 8.5,
      "overview": "A promising young drummer...",
      "year": 2014,
      "relevanceScore": 0.9,
      "reasoning": "Similar to La La Land"
    }
  ],
  "traceId": "abc123"
}
```

## 📈 Benefits Over Previous Implementation

### ✅ Predictable Behavior
- **"movies like X"** → Only similar movies (no random exact matches)
- **"X"** → Only exact/partial matches (no unwanted recommendations)  
- **"genre"** → Only strong genre matches (no weak associations)

### ✅ Performance
- **Lighter parsing** (regex instead of heavy LLM)
- **Targeted API calls** (no shotgun approach)
- **Client-side logic** (deterministic intersections)

### ✅ User Experience  
- **Clear expectations** (mode-based responses)
- **Quality filtering** (vote_average ≥ 6.0 for similar)
- **Proper media type respect** (movies vs TV)

### ✅ Maintainability
- **Simple switch statement** (4 clear modes)
- **Fixed whitelists** (no expanding complexity)
- **Deterministic ranking** (no black box scoring)

## 🔄 Migration Path

### Available Endpoints
1. **`/api/search/flexi`** - Original basic search
2. **`/api/search/enhanced`** - Complex hybrid search  
3. **`/api/search/minimal`** - New simplified search ⭐

### Recommendation
Use **`/api/search/minimal`** for:
- ✅ Predictable, deterministic results
- ✅ Better user experience alignment  
- ✅ Simpler debugging and maintenance
- ✅ Faster response times

The minimal implementation delivers exactly what users expect without the complexity and unpredictability of the previous approaches. It's **dead-simple, text-only flow** that hits all your goals! 🎯
