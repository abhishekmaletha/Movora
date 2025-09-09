using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Movora.Domain.FlexiSearch;

namespace Movora.Infrastructure.FlexiSearch;

/// <summary>
/// Selects and delegates to the appropriate LLM provider based on configuration
/// </summary>
public sealed class LlmSearchSelector : ILlmSearch
{
    private readonly ILlmSearch _implementation;
    private readonly ILogger<LlmSearchSelector> _logger;

    public LlmSearchSelector(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<LlmSearchSelector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var selectedProvider = configuration["Llm:SelectedProvider"]?.ToLowerInvariant();
        
        _implementation = selectedProvider switch
        {
            "openai" => serviceProvider.GetRequiredService<OpenAiLlmSearch>(),
            "groq" => serviceProvider.GetRequiredService<GroqLlmSearch>(),
            _ => throw new InvalidOperationException($"Unknown LLM provider: {selectedProvider}. Supported providers: OpenAI, Groq")
        };

        _logger.LogInformation("LLM provider selected: {Provider}", selectedProvider);
    }

    public async Task<LlmIntent> ExtractIntentAsync(string query, CancellationToken ct = default)
    {
        return await _implementation.ExtractIntentAsync(query, ct);
    }
}
