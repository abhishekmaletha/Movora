using MediatR;
using Microsoft.AspNetCore.Mvc;
using Movora.Application.UseCases.Write.FlexiSearch;
using System.ComponentModel.DataAnnotations;

namespace Movora.WebApi.Controllers;

/// <summary>
/// Controller for flexible search functionality using LLM-enhanced TMDb queries
/// </summary>
[ApiController]
[Route("api/search")]
[Produces("application/json")]
public sealed class FlexiSearchController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FlexiSearchController> _logger;

    public FlexiSearchController(IMediator mediator, ILogger<FlexiSearchController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Performs flexible search using natural language query
    /// </summary>
    /// <param name="request">Search request containing natural language query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results with relevance scoring and reasoning</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="400">Invalid request or empty query</response>
    /// <response code="500">Internal server error during search</response>
    [HttpPost("flexi")]
    [ProducesResponseType<FlexiSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FlexiSearchResponse>> SearchAsync(
        [FromBody] FlexiSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            _logger.LogWarning("FlexiSearch called with null request");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Request body cannot be null",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        // Validate request manually since model validation might not catch all cases
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            _logger.LogWarning("FlexiSearch called with empty query");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Query",
                Detail = "Query cannot be empty or whitespace",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        if (request.Query.Length > 500)
        {
            _logger.LogWarning("FlexiSearch called with query too long: {QueryLength} characters", request.Query.Length);
            return BadRequest(new ProblemDetails
            {
                Title = "Query Too Long",
                Detail = "Query cannot exceed 500 characters",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            _logger.LogInformation("Processing FlexiSearch request for query: {Query}", request.Query);

            var command = new FlexiSearchCommand(request);
            var response = await _mediator.Send(command, cancellationToken);

            // Add trace ID for request tracking
            response = response with { TraceId = HttpContext.TraceIdentifier };

            _logger.LogInformation("FlexiSearch completed successfully with {ResultCount} results for trace: {TraceId}",
                response.Results.Count, response.TraceId);

            return Ok(response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in FlexiSearch for query: {Query}", request.Query);
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argument error in FlexiSearch for query: {Query}", request.Query);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Argument",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in FlexiSearch for query: {Query}", request.Query);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while processing the search request",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Enhanced flexible search using the new FlexiSearch brain with sophisticated ranking
    /// </summary>
    /// <param name="request">Search request containing natural language query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results with enhanced hybrid ranking and detailed reasoning</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="400">Invalid request or empty query</response>
    /// <response code="500">Internal server error during search</response>
    [HttpPost("enhanced")]
    [ProducesResponseType<FlexiSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FlexiSearchResponse>> EnhancedSearchAsync(
        [FromBody] FlexiSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            _logger.LogWarning("Enhanced FlexiSearch called with null request");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Request body cannot be null",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            _logger.LogWarning("Enhanced FlexiSearch called with empty query");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Query",
                Detail = "Query cannot be empty or whitespace",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            _logger.LogInformation("Processing Enhanced FlexiSearch request for query: {Query}", request.Query);

            // Use the enhanced handler directly
            //var enhancedHandler = HttpContext.RequestServices.GetRequiredService<EnhancedFlexiSearchCommandHandler>();
            var command = new EnhancedFlexiSearchCommand(request);
            var response = await _mediator.Send(command);

            // Add trace ID for request tracking
            response = response with { TraceId = HttpContext.TraceIdentifier };

            _logger.LogInformation("Enhanced FlexiSearch completed successfully with {ResultCount} results for trace: {TraceId}",
                response.Results.Count, response.TraceId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Enhanced FlexiSearch for query: {Query}", request.Query);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while processing the enhanced search request",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Minimal flexible search with deterministic, predictable behavior
    /// Uses 4 simple modes: TITLE, SIMILAR, GENRE, FALLBACK
    /// </summary>
    /// <param name="request">Search request containing natural language query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results with simple, predictable ranking</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="400">Invalid request or empty query</response>
    /// <response code="500">Internal server error during search</response>
    [HttpPost("minimal")]
    [ProducesResponseType<FlexiSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FlexiSearchResponse>> MinimalSearchAsync(
        [FromBody] FlexiSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            _logger.LogWarning("Minimal FlexiSearch called with null request");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Request body cannot be null",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            _logger.LogWarning("Minimal FlexiSearch called with empty query");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Query",
                Detail = "Query cannot be empty or whitespace",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            _logger.LogInformation("Processing Minimal FlexiSearch request for query: {Query}", request.Query);

            // Use the minimal handler directly
            //var minimalHandler = HttpContext.RequestServices.GetRequiredService<MinimalFlexiSearchCommandHandler>();
            var command = new MinimalFlexiSearchCommand(request);
            var response = await this._mediator.Send(command);

            // Add trace ID for request tracking
            response = response with { TraceId = HttpContext.TraceIdentifier };

            _logger.LogInformation("Minimal FlexiSearch completed successfully with {ResultCount} results for trace: {TraceId}",
                response.Results.Count, response.TraceId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Minimal FlexiSearch for query: {Query}", request.Query);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while processing the minimal search request",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Health check endpoint for FlexiSearch functionality
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTimeOffset.UtcNow,
            version = "3.0.0",
            features = new[] { "basic_search", "enhanced_search", "minimal_search", "deterministic_ranking" }
        });
    }
}
