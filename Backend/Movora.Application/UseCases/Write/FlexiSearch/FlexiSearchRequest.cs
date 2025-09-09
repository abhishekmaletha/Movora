using System.ComponentModel.DataAnnotations;

namespace Movora.Application.UseCases.Write.FlexiSearch;

/// <summary>
/// Request DTO for flexible search functionality
/// </summary>
public sealed record FlexiSearchRequest
{
    /// <summary>
    /// Natural language search query from the user
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Query cannot be empty")]
    public required string Query { get; init; }
}
