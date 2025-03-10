using MediatR;
using Movora.Application.Dtos;
namespace Movora.Application.Requests
{
    public class SearchMoviesQuery : IRequest<List<MovieDto>>
    {
        public string Query { get; }

        public SearchMoviesQuery(string query)
        {
            Query = query;
        }
    }
}