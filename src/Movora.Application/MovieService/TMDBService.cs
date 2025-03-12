// implementation of IMovieService named as TMDBService.cs
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Movora.Application.Dtos;
using Movora.Application.MovieService.Interfaces;

namespace Movora.Application.MovieService
{
    public class TMDBService : IMovieService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly IConfiguration _configuration;

        public TMDBService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<List<MovieDto>> GetMovieDetailsAsync(List<string> names)
        {
            return await this.GetMoviesDetailsAsync(names);
        }

        private async Task<List<MovieDto>> GetMoviesDetailsAsync(List<string> movieNames)
        {
            var movieIdsTasks = movieNames.Select(GetMovieId).ToList();
            var movieIds = await Task.WhenAll(movieIdsTasks);

            var movieDetailsTasks = movieIds
                .Where(id => id.HasValue)
                .Select(id => GetMovieDetails(id.Value))
                .ToList();

            var moviesDetails = await Task.WhenAll(movieDetailsTasks);

            return moviesDetails.Where(details => details != null).ToList();
        }

        private async Task<int?> GetMovieId(string movieName)
        {
            var searchUrl = $"{_configuration["TMDB:SEARCH_URL"]}?api_key={_configuration["TMDB:api_key"]}&query={Uri.EscapeDataString(movieName)}";

            var response = await httpClient.GetAsync(searchUrl);
            if (!response.IsSuccessStatusCode) return null;

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            var results = doc.RootElement.GetProperty("results");

            if (results.GetArrayLength() == 0) return null;

            return results[0].GetProperty("id").GetInt32();
        }

        private async Task<MovieDto> GetMovieDetails(int movieId)
        {
            var detailsUrl = $"{_configuration["TMDB:MOVIE_DETAILS_URL"]}{movieId}?api_key={_configuration["TMDB:api_key"]}&append_to_response=credits";

            var response = await httpClient.GetAsync(detailsUrl);
            if (!response.IsSuccessStatusCode) return null;

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            var title = doc.RootElement.GetProperty("title").GetString();
            var description = doc.RootElement.GetProperty("overview").GetString();
            var thumbnailPath = doc.RootElement.GetProperty("poster_path").GetString();
            var rating = doc.RootElement.GetProperty("vote_average").GetDecimal();
            var cast = doc.RootElement.GetProperty("credits").GetProperty("cast")
                            .EnumerateArray()
                            .Take(5) // Limit to top 5 cast members
                            .Select(c => c.GetProperty("name").GetString())
                            .Where(name => name != null)
                            .Cast<string>()
                            .ToList();

            return new MovieDto
            {
                Title = title ?? string.Empty,
                Description = description ?? string.Empty,
                ThumbnailUrl = $"{_configuration["TMDB:IMAGE_BASE_URL"]}{thumbnailPath}",
                Cast = cast,
                Rating = rating
            };
        }
    }
}