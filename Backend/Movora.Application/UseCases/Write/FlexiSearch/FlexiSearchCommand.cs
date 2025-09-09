using MediatR;

namespace Movora.Application.UseCases.Write.FlexiSearch;

/// <summary>
/// Command for executing flexible search operations
/// </summary>
public sealed record FlexiSearchCommand(FlexiSearchRequest Request) : IRequest<FlexiSearchResponse>;
