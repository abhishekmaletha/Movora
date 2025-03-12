using System.Collections.Generic;
using System.Threading.Tasks;
using Movora.Application.Dtos;

namespace Movora.Application.MovieService.Interfaces
{
    public interface IMovieService
    {
        Task<List<MovieDto>> GetMovieDetailsAsync(List<string> names);
    }
}