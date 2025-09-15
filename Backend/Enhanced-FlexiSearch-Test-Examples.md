# Enhanced FlexiSearch Test Examples

## Test Scenarios for the Enhanced FlexiSearch Brain

### 1. Exact Title Lookup Tests

#### Test Case 1.1: Simple Movie Title
**Query**: `"Inception"`
**Expected Behavior**: 
- Should return ONLY the exact match for Inception (2010)
- No similar movies should be included
- Reasoning: "Exact title match"

**Expected Response**:
```json
{
  "results": [
    {
      "tmdbId": 27205,
      "name": "Inception",
      "mediaType": "movie",
      "year": 2010,
      "relevanceScore": 1.0,
      "reasoning": "Exact title match"
    }
  ]
}
```

#### Test Case 1.2: TV Series Title
**Query**: `"Stranger Things"`
**Expected Behavior**:
- Should return ONLY the exact match for Stranger Things TV series
- No similar shows should be included
- High confidence exact match detection

#### Test Case 1.3: Title with Year
**Query**: `"The Dark Knight 2008"`
**Expected Behavior**:
- Should match the specific 2008 movie
- Year constraint should be applied during matching
- Should not return other Batman movies

### 2. Similarity Discovery Tests

#### Test Case 2.1: Movies Like Pattern
**Query**: `"movies like Inception"`
**Expected Behavior**:
- Should find Inception as seed movie
- Return similar mind-bending sci-fi movies
- Include movies with similar themes: complex plots, reality-bending, psychological

**Expected Similar Movies**:
- The Matrix
- Shutter Island
- Memento
- Interstellar
- The Prestige

#### Test Case 2.2: TV Series Similarity
**Query**: `"shows similar to Stranger Things"`
**Expected Behavior**:
- Find shows with similar themes: supernatural, 80s nostalgia, kids protagonists
- Include horror/sci-fi elements
- Similar tone and atmosphere

**Expected Similar Shows**:
- Dark
- The X-Files
- Twin Peaks
- Supernatural
- Fringe

### 3. Mood-Based Discovery Tests

#### Test Case 3.1: Feel-Good Movies
**Query**: `"feel-good movies"`
**Expected Behavior**:
- Map "feel-good" mood to appropriate genres (comedy, family, music, romance)
- Return uplifting, positive movies
- High ratings preferred

**Expected Movies**:
- Forrest Gump
- The Pursuit of Happyness
- Good Will Hunting
- The Princess Bride
- Paddington

#### Test Case 3.2: Dark Psychological Thrillers
**Query**: `"dark psychological thriller movies"`
**Expected Behavior**:
- Map "dark" and "psychological" to appropriate genres
- Return intense, mind-bending thrillers
- Focus on psychological elements

**Expected Movies**:
- Black Swan
- Shutter Island
- Zodiac
- Gone Girl
- The Silence of the Lambs

### 4. Actor/Director-Based Tests

#### Test Case 4.1: Actor Movies
**Query**: `"Tom Hanks movies from the 90s"`
**Expected Behavior**:
- Search for Tom Hanks as person
- Filter results to 1990-1999 year range
- Return his notable 90s films

**Expected Movies**:
- Forrest Gump (1994)
- Philadelphia (1993)
- Saving Private Ryan (1998)
- Cast Away (1999)
- You've Got Mail (1998)

#### Test Case 4.2: Director Style
**Query**: `"Christopher Nolan films"`
**Expected Behavior**:
- Identify Christopher Nolan as director
- Return his filmography
- High-quality, mind-bending movies

**Expected Movies**:
- Inception
- The Dark Knight
- Interstellar
- Memento
- Dunkirk

### 5. Complex Multi-Criteria Tests

#### Test Case 5.1: Complex Query
**Query**: `"recent action movies like John Wick but shorter than 2 hours"`
**Expected Behavior**:
- Find John Wick as seed movie
- Filter for recent releases (last 5-10 years)
- Action genre preference
- Runtime constraint (< 120 minutes)
- Similar style: stylized action, assassin themes

**Expected Movies**:
- Nobody (2021)
- Atomic Blonde (2017)
- The Equalizer (2014)
- Taken (2008)
- Man on Fire (2004)

#### Test Case 5.2: Family-Friendly Animated
**Query**: `"feel-good animated movies for kids from the 2010s"`
**Expected Behavior**:
- Animation genre filter
- Family-friendly content
- 2010-2019 year range
- Positive, uplifting themes

**Expected Movies**:
- Coco (2017)
- Moana (2016)
- Zootopia (2016)
- Inside Out (2015)
- Frozen (2013)

### 6. Edge Cases and Error Handling

#### Test Case 6.1: Ambiguous Query
**Query**: `"dark"`
**Expected Behavior**:
- Should interpret as mood preference
- Return various dark-themed movies/shows
- Multiple genres: horror, thriller, crime, drama

#### Test Case 6.2: Non-Existent Title
**Query**: `"The Completely Made Up Movie Title 2023"`
**Expected Behavior**:
- No exact match found
- Should fall back to broader search
- Return empty results or suggest alternatives

#### Test Case 6.3: Very Specific Constraints
**Query**: `"Japanese horror movies from 2019 with high ratings"`
**Expected Behavior**:
- Language filter (Japanese)
- Genre filter (Horror)
- Year constraint (2019)
- Quality filter (high ratings)
- May return limited results due to specificity

### 7. Ranking Quality Tests

#### Test Case 7.1: Ranking Consistency
**Query**: `"sci-fi movies with time travel"`
**Expected Ranking Factors**:
1. **Genre Match**: Strong sci-fi classification
2. **Theme Match**: Time travel keywords in overview
3. **Quality**: High TMDB ratings
4. **Popularity**: Well-known movies ranked higher
5. **Recency**: Slight boost for newer releases

**Expected Top Results Order**:
1. Back to the Future (classic, perfect match)
2. Groundhog Day (perfect theme match)
3. The Terminator (iconic sci-fi)
4. Looper (recent, high quality)
5. Predestination (complex time travel)

#### Test Case 7.2: Reasoning Quality
**Query**: `"movies like The Godfather"`
**Expected Reasoning Examples**:
- "Because it matches crime and drama genres, features family themes, and has excellent rating (9.2/10)"
- "Because it's similar to your search, features organized crime, and is highly rated"
- "Because it matches drama genre and features similar themes of power and family"

### 8. Performance Tests

#### Test Case 8.1: Response Time
**Queries**: Various complexity levels
**Expected Response Times**:
- Simple exact match: < 500ms
- Basic discovery: < 1000ms
- Complex multi-criteria: < 2000ms
- Actor/director search: < 1500ms

#### Test Case 8.2: Rate Limiting
**Scenario**: Multiple rapid requests
**Expected Behavior**:
- Should handle TMDB rate limits gracefully
- Implement retry logic with exponential backoff
- No failures due to rate limiting

### 9. Integration Tests

#### Test Case 9.1: Full Pipeline
**Query**: `"mind-bending movies like Inception with Leonardo DiCaprio"`
**Pipeline Steps**:
1. **Intent Extraction**: Titles=["Inception"], People=["Leonardo DiCaprio"], Moods=["mind-bending"]
2. **ID Resolution**: Find Inception movie, Leonardo DiCaprio person
3. **Discovery**: Genre-based + person-based + similar movies
4. **Ranking**: Hybrid scoring with multiple factors
5. **Response**: Top 12 results with detailed reasoning

#### Test Case 9.2: Error Recovery
**Scenario**: TMDB API temporarily unavailable
**Expected Behavior**:
- Graceful degradation
- Return cached results if available
- Meaningful error messages
- No system crashes

### 10. A/B Testing Scenarios

#### Test Case 10.1: Ranking Algorithm Comparison
**Query**: `"romantic comedies from the 2000s"`
**Compare**:
- Enhanced hybrid ranking vs basic ranking
- Measure relevance and user satisfaction
- Track click-through rates on results

#### Test Case 10.2: Intent Detection Accuracy
**Various Queries**: Mix of exact titles and discovery requests
**Metrics**:
- Exact match detection accuracy
- False positive rate for similarity requests
- User satisfaction with result types

## Test Data Setup

### Mock TMDB Responses
```json
{
  "inception_search": {
    "results": [
      {
        "id": 27205,
        "title": "Inception",
        "release_date": "2010-07-16",
        "vote_average": 8.8,
        "overview": "Dom Cobb is a skilled thief..."
      }
    ]
  }
}
```

### Test Environment Configuration
```json
{
  "TMDb": {
    "ApiKey": "test-api-key",
    "BaseUrl": "https://api.themoviedb.org/3/"
  },
  "TestMode": true,
  "MockResponses": true
}
```

## Success Criteria

### Functional Requirements
- ✅ Exact title detection accuracy > 95%
- ✅ Similarity discovery relevance score > 0.8
- ✅ Response time < 2000ms for complex queries
- ✅ Zero system crashes under normal load
- ✅ Graceful error handling for all edge cases

### Quality Requirements
- ✅ Detailed reasoning for all results
- ✅ Consistent ranking across similar queries
- ✅ Appropriate result count (1 for exact, up to 12 for discovery)
- ✅ High-quality content filtering (vote count thresholds)
- ✅ Relevant genre and mood mapping

### User Experience Requirements
- ✅ Intuitive natural language understanding
- ✅ Meaningful "Because..." explanations
- ✅ Diverse but relevant result sets
- ✅ Clear distinction between exact matches and discoveries
- ✅ Helpful suggestions for ambiguous queries

This comprehensive test suite ensures the Enhanced FlexiSearch system delivers accurate, relevant, and high-quality movie and TV show recommendations across all supported query types and use cases.
