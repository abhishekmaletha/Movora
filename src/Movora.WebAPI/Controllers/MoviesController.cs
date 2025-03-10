using MediatR;
using Microsoft.AspNetCore.Mvc;
using Movora.Application.Requests;
using System.Threading.Tasks;

namespace Movora.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MoviesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var result = await _mediator.Send(new SearchMoviesQuery(query));
            return Ok(result);
        }
    }
}