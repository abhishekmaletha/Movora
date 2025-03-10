using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Movora.Application.AI.Interfaces;
using Movora.Application.Constants;

namespace Movora.Application.AI
{
    public class Groq : ILLM
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public Groq(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task<List<string>> GetMovieResponseASync(string message, string model)
        {
            var promptFilePath = Path.Combine(Directory.GetCurrentDirectory(), Paths.MovieSearchPromptPath);
            var prompt = await File.ReadAllTextAsync(promptFilePath);

            prompt = prompt.Replace("{user_message}", message);

            var response = await CallGroqApiAsync(model, new List<string> { prompt });

            return ExtractMovieNames(response);
        }

        private async Task<string> CallGroqApiAsync(string model, List<string> messages)
        {
            var apiKey = _configuration["LLMS:groq:API_KEY"];
            var requestUri = "https://api.groq.com/openai/v1/chat/completions";

            var requestBody = new
            {
                messages = messages.Select(m => new { role = "user", content = m }).ToList(),
                model = model,
                temperature = 1,
                max_completion_tokens = 1024,
                top_p = 1,
                stream = true,
                stop = (string?)null
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync(requestUri, content);
            response.EnsureSuccessStatusCode();

            // Read response as a stream
            var responseStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(responseStream);

            var fullResponse = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line) && line.StartsWith("data:"))
                {
                    try
                    {
                        // Extract JSON from "data: {...}"
                        var jsonString = line.Substring(5).Trim();
                        if (jsonString == "[DONE]") break;

                        var jsonDoc = JsonDocument.Parse(jsonString);
                        var choices = jsonDoc.RootElement.GetProperty("choices");

                        foreach (var choice in choices.EnumerateArray())
                        {
                            if (choice.TryGetProperty("delta", out var delta))
                            {
                                if (delta.TryGetProperty("content", out var contentValue))
                                {
                                    fullResponse.Append(contentValue.GetString());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing JSON: {ex.Message}");
                    }
                }
            }

            return fullResponse.ToString().Trim();
        }

        private List<string> ExtractMovieNames(string response)
        {
            if (string.IsNullOrEmpty(response))
                return new List<string>();

            try
            {
                // Parse the JSON response
                var jsonDoc = JsonDocument.Parse(response);
                var data = jsonDoc.RootElement.GetProperty("Data");
                var movies = data.GetProperty("Movies");

                // Extract movie names from the "Movies" array
                return movies.EnumerateArray()
                             .Select(movie => movie.GetString())
                             .Where(movie => !string.IsNullOrEmpty(movie))
                             .Select(movie => movie!)
                             .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting movie names: {ex.Message}");
                return new List<string>();
            }
        }
    }
}