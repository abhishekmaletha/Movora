using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movora.Infrastructure.SqlModels;

[Table("episodes")]
public class Episode
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [Column("series_id")]
    public Guid SeriesId { get; set; }
    
    [Required]
    [Column("season_number")]
    public int SeasonNumber { get; set; }
    
    [Required]
    [Column("episode_number")]
    public int EpisodeNumber { get; set; }
    
    [Required]
    [Column("title")]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Column("air_date")]
    public DateOnly? AirDate { get; set; }
    
    [Column("description", TypeName = "text")]
    public string? Description { get; set; }
    
    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [Column("updated_date")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("SeriesId")]
    public virtual Series Series { get; set; } = null!;
}
