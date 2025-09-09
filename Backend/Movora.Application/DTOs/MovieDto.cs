namespace Movora.Application.DTOs;

public class MovieDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Genre { get; set; }
    public int? ReleaseYear { get; set; }
    public int? Duration { get; set; }
    public string? Director { get; set; }
    public string? Cast { get; set; }
    public decimal? Rating { get; set; }
    public string? PosterUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
