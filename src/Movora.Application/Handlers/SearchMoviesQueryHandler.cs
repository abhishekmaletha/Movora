using System.Text.Json;
using MediatR;
using Movora.Application.AI.Interfaces;
using Movora.Application.Constants;
using Movora.Application.Dtos;
using Movora.Application.Requests;
namespace Movora.Application.Handlers
{
    public class SearchMoviesQueryHandler : IRequestHandler<SearchMoviesQuery, List<MovieDto>>
    {
        private static readonly string apiKey = "fb3d1750402e3acd66b0bb9a2fe3bdd5"; // 🔹 Replace with your TMDB API Key
        private static readonly string baseUrl = "https://api.themoviedb.org/3/";
        private readonly ILLM lLM;

        public SearchMoviesQueryHandler(ILLM lLM)
        {
            this.lLM = lLM ?? throw new ArgumentNullException(nameof(lLM));
        }

        public async Task<List<MovieDto>> Handle(SearchMoviesQuery request, CancellationToken cancellationToken)
        {
            var response = await this.lLM.GetMovieResponseASync(request.Query, GroqModels.Llama3_70b8192);
            var details = await FetchMultipleMovieDetails(response);
            return new List<MovieDto>(); // Return the search results
        }

        /// <summary>
        /// Fetches movie details for a list of movie names from TMDB API concurrently.
        /// </summary>
        /// <param name="movieNames">List of movie names</param>
        /// <returns>List of movie details as strings</returns>
        static async Task<List<string>> FetchMultipleMovieDetails(List<string> movieNames)
        {
            List<Task<string>> tasks = new List<Task<string>>();

            foreach (var movie in movieNames)
            {
                tasks.Add(GetMovieDetails(movie));
            }

            // 🔹 Execute all tasks in parallel and wait for results
            var results = await Task.WhenAll(tasks);

            // 🔹 Convert array to list and return
            return new List<string>(results);
        }

        /// <summary>
        /// Fetches movie details for a single movie from TMDB API.
        /// </summary>
        /// <param name="movieName">The name of the movie</param>
        /// <returns>Formatted movie details as a string</returns>
        static async Task<string> GetMovieDetails(string movieName)
        {
            using (HttpClient client = new HttpClient())
            {
                string searchUrl = $"{baseUrl}search/movie?query={Uri.EscapeDataString(movieName)}&api_key={apiKey}";

                HttpResponseMessage response = await client.GetAsync(searchUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ Failed to fetch data for {movieName}";
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                if (root.GetProperty("results").GetArrayLength() == 0)
                {
                    return $"❌ No results found for {movieName}";
                }

                var firstResult = root.GetProperty("results")[0];

                string title = firstResult.GetProperty("title").GetString();
                string overview = firstResult.GetProperty("overview").GetString();
                string releaseDate = firstResult.TryGetProperty("release_date", out JsonElement dateElem) && dateElem.ValueKind == JsonValueKind.String
                    ? dateElem.GetString()
                    : "Unknown";
                double rating = firstResult.GetProperty("vote_average").GetDouble();

                return $"🎬 {title} ({releaseDate})\n⭐ Rating: {rating}/10\n📖 Overview: {overview}\n";
            }
        }
    }
}