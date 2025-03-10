using MediatR;
using Movora.Application.AI.Interfaces;
using Movora.Application.Constants;
using Movora.Application.Dtos;
using Movora.Application.Requests;
namespace Movora.Application.Handlers
{
public class SearchMoviesQueryHandler : IRequestHandler<SearchMoviesQuery, List<MovieDto>>
    {
        private readonly ILLM lLM;

        public SearchMoviesQueryHandler(ILLM lLM)
        {
            this.lLM = lLM ?? throw new ArgumentNullException(nameof(lLM));
        }

        public async Task<List<MovieDto>> Handle(SearchMoviesQuery request, CancellationToken cancellationToken)
        {
            var response = await this.lLM.GetMovieResponseASync(request.Query, GroqModels.Llama3_70b8192);

            return new List<MovieDto>(); // Return the search results
        }
    }
}