# FlexiSearch Troubleshooting Guide

## 🎯 Quick Fix Applied

**Fixed Groq model configuration:**
- Changed from `"openai/gpt-oss-20b"` (invalid) to `"llama-3.1-70b-versatile"` (valid)
- This was likely the main cause of your failure

## 🧪 Testing Methods

### Method 1: Direct Component Test (Recommended)
```bash
cd Backend/FlexiSearchTester
dotnet run
```
This tests FlexiSearch components directly without the web server.

### Method 2: Web API Test
1. Start the server:
   ```bash
   cd Backend/Movora.WebAPI
   dotnet run
   ```

2. Run the test script:
   ```powershell
   cd Backend
   .\test-flexisearch.ps1
   ```

### Method 3: Manual API Test
```bash
curl -X POST "http://localhost:51819/api/search/flexi" \
  -H "Content-Type: application/json" \
  -d '{"query": "best 10 movies with psychological thriller"}'
```

## 🔍 Common Issues & Solutions

### Issue 1: LLM Provider Errors
**Symptoms:** 500 errors, "LLM provider failed"
**Solutions:**
- ✅ **Fixed:** Updated Groq model name
- Switch to OpenAI: Change `"SelectedProvider": "OpenAI"` in appsettings.json
- Check API keys are valid

### Issue 2: TMDb API Errors
**Symptoms:** Empty results, TMDb errors
**Solutions:**
- Verify TMDb API key: `fb3d1750402e3acd66b0bb9a2fe3bdd5`
- Check rate limiting (40 requests per 10 seconds)
- Test direct TMDb API: `https://api.themoviedb.org/3/search/multi?api_key=YOUR_KEY&query=test`

### Issue 3: Database Connection Issues
**Symptoms:** Server won't start
**Solutions:**
- Ensure PostgreSQL is running
- Update connection string in appsettings.json
- Run: `dotnet ef database update` if needed

### Issue 4: MediatR Registration Issues
**Symptoms:** 404 errors, handler not found
**Solutions:**
- Verify MediatR is registered (✅ already configured)
- Check handler namespace: `Movora.Application.UseCases.Write.FlexiSearch`

## 📊 Expected Behavior

### For "best 10 movies with psychological thriller":

**LLM Should Extract:**
```json
{
  "titles": [],
  "people": [],
  "genres": ["thriller"],
  "moods": ["psychological"],
  "mediaTypes": ["movie", "tv"],
  "yearFrom": null,
  "yearTo": null,
  "runtimeMaxMinutes": null,
  "requestedCount": 10
}
```

### For "psychological thriller movies" (no count):

**LLM Should Extract:**
```json
{
  "titles": [],
  "people": [],
  "genres": ["thriller"],
  "moods": ["psychological"],
  "mediaTypes": ["movie", "tv"],
  "yearFrom": null,
  "yearTo": null,
  "runtimeMaxMinutes": null,
  "requestedCount": null
}
```

**TMDb Should Find:**
- Direct search for "psychological thriller"
- Discovery with thriller genre
- Results like "Shutter Island", "Black Swan", "The Sixth Sense"

**Expected Results:**
- When count specified (e.g., "top 10"): Returns exactly that many results or fewer
- When no count specified: Returns all relevant results (up to 50)
- Each with score 0.0-1.0 and reasoning
- Higher scores for exact genre matches

## 🔢 "Top X" Functionality

### Supported Patterns:
- "top 5 movies"
- "best 10 comedies"
- "give me 3 action films"
- "show me 7 thrillers"
- "find 15 romantic movies"
- "5 sci-fi movies"

### Behavior:
- **With count**: Returns ≤ requested number
- **Without count**: Returns all relevant results (max 50)
- **Invalid count**: Treats as no count specified

## 🚨 Debugging Steps

1. **Test each component:**
   ```bash
   cd Backend/FlexiSearchTester
   dotnet run
   ```

2. **Check logs:**
   - Look for LLM intent extraction logs
   - Verify TMDb API calls
   - Check ranking/scoring logs

3. **Verify configuration:**
   - LLM provider selection
   - API keys present
   - Database connection

4. **Test fallbacks:**
   - LLM should fallback on errors
   - Empty results should return gracefully

## 🔧 Configuration Validation

### Required Settings:
```json
{
  "TMDb": {
    "ApiKey": "fb3d1750402e3acd66b0bb9a2fe3bdd5"  // ✅ Present
  },
  "Llm": {
    "SelectedProvider": "Groq"  // ✅ Valid
  },
  "Groq": {
    "ApiKey": "gsk_...",  // ✅ Present
    "Model": "llama-3.1-70b-versatile"  // ✅ Fixed
  }
}
```

## 📞 Next Steps

1. **Run the direct tester first** - this will isolate the issue
2. **Check component by component** - LLM → TMDb → Ranking
3. **Review logs** for specific error messages
4. **Try different queries** to test various code paths

Your FlexiSearch implementation is architecturally sound. The Groq model fix should resolve the primary issue!
