using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movora.Infrastructure.SqlModels;

[Table("series")]
public class Series
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [Column("tmdb_id")]
    public int TmdbId { get; set; }
    
    [Required]
    [Column("title")]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Column("description", TypeName = "text")]
    public string? Description { get; set; }
    
    [Column("genres", TypeName = "text[]")]
    public string[]? Genres { get; set; }
    
    [Column("first_air_date")]
    public DateOnly? FirstAirDate { get; set; }
    
    [Column("poster_url")]
    [MaxLength(1000)]
    public string? PosterUrl { get; set; }
    
    [Column("embedding", TypeName = "vector(1536)")] // OpenAI embedding dimension
    public float[]? Embedding { get; set; }
    
    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [Column("updated_date")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Episode> Episodes { get; set; } = new List<Episode>();
    public virtual ICollection<UserWatched> WatchedByUsers { get; set; } = new List<UserWatched>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
}
