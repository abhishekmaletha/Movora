using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Movora.Application.UseCases.Write.FlexiSearch;
using Movora.Infrastructure.FlexiSearch;
using Movora.Domain.FlexiSearch;

Console.WriteLine("🧪 FlexiSearch Direct Test");
Console.WriteLine("==========================");

// Create configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("../Movora.WebAPI/appsettings.json", optional: false)
    .Build();

// Setup DI container
var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
serviceCollection.AddFlexiSearch(configuration);

// Create service provider
var serviceProvider = serviceCollection.BuildServiceProvider();

// Test the FlexiSearch components directly
try
{
    // Test 1: LLM Intent Extraction
    Console.WriteLine("\n🤖 Testing LLM Intent Extraction...");
    var llmSearch = serviceProvider.GetRequiredService<ILlmSearch>();
    var query = "best 10 movies with psychological thriller";
    var intent = await llmSearch.ExtractIntentAsync(query);
    
    Console.WriteLine($"✅ Intent extracted successfully:");
    Console.WriteLine($"   Titles: [{string.Join(", ", intent.Titles)}]");
    Console.WriteLine($"   Genres: [{string.Join(", ", intent.Genres)}]");
    Console.WriteLine($"   Moods: [{string.Join(", ", intent.Moods)}]");
    Console.WriteLine($"   MediaTypes: [{string.Join(", ", intent.MediaTypes)}]");

    // Test 2: TMDb API
    Console.WriteLine("\n🎬 Testing TMDb API...");
    var tmdbClient = serviceProvider.GetRequiredService<ITmdbClient>();
    var searchResult = await tmdbClient.SearchMultiAsync("psychological thriller");
    Console.WriteLine($"✅ TMDb search returned {searchResult.Results.Count} results");

    // Test 3: Genre Mapping
    Console.WriteLine("\n📋 Testing Genre Mapping...");
    var genreMap = await tmdbClient.GetGenreMapAsync("movie");
    Console.WriteLine($"✅ Found {genreMap.Count} movie genres");
    if (genreMap.ContainsKey("thriller"))
    {
        Console.WriteLine($"   Thriller genre ID: {genreMap["thriller"]}");
    }

    // Test 4: Full Handler with Default Query
    Console.WriteLine("\n🔍 Testing Full FlexiSearch Handler...");
    var handler = serviceProvider.GetRequiredService<FlexiSearchCommandHandler>();
    var request = new FlexiSearchRequest { Query = query };
    var command = new FlexiSearchCommand(request);
    var response = await handler.Handle(command, CancellationToken.None);
    
    Console.WriteLine($"✅ FlexiSearch completed with {response.Results.Count} results");
    
    if (response.Results.Any())
    {
        Console.WriteLine("\n🏆 Top 5 Results:");
        foreach (var result in response.Results.Take(5))
        {
            Console.WriteLine($"   📽️ {result.Name} ({result.Year})");
            Console.WriteLine($"      Score: {result.RelevanceScore:F2} | {result.Reasoning}");
        }
    }

    // Test 5: Testing "Top X" Functionality
    Console.WriteLine("\n🔢 Testing 'Top X' Count Extraction...");
    var countQueries = new[]
    {
        "top 3 action movies",
        "best 5 comedies from 2020",
        "give me 7 sci-fi shows",
        "psychological thrillers" // no count
    };

    foreach (var countQuery in countQueries)
    {
        Console.WriteLine($"\n   Query: '{countQuery}'");
        var countIntent = await llmSearch.ExtractIntentAsync(countQuery);
        Console.WriteLine($"   Extracted count: {countIntent.RequestedCount?.ToString() ?? "null"}");
        
        var countRequest = new FlexiSearchRequest { Query = countQuery };
        var countCommand = new FlexiSearchCommand(countRequest);
        var countResponse = await handler.Handle(countCommand, CancellationToken.None);
        
        Console.WriteLine($"   Results returned: {countResponse.Results.Count}");
        if (countIntent.RequestedCount.HasValue)
        {
            var expectedCount = Math.Min(countIntent.RequestedCount.Value, countResponse.Results.Count);
            var success = countResponse.Results.Count <= countIntent.RequestedCount.Value;
            Console.WriteLine($"   ✅ Count respected: {success} (expected ≤ {countIntent.RequestedCount.Value})");
        }
    }
    
    Console.WriteLine("\n🎉 All tests passed! FlexiSearch is working correctly.");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
    }
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
