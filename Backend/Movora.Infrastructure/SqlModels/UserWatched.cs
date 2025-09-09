using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movora.Infrastructure.SqlModels;

public enum ContentType
{
    Movie = 0,
    Series = 1
}

[Table("user_watched")]
public class UserWatched
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }
    
    [Required]
    [Column("content_id")]
    public Guid ContentId { get; set; }
    
    [Required]
    [Column("content_type")]
    public ContentType ContentType { get; set; }
    
    [Column("watched_date")]
    public DateTime WatchedDate { get; set; } = DateTime.UtcNow;
    
    [Column("episodes_watched")]
    public int? EpisodesWatched { get; set; } // For series tracking
    
    [Column("progress_percentage")]
    public decimal? ProgressPercentage { get; set; } // 0-100 for partial watches
    
    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [Column("updated_date")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey("ContentId")]
    public virtual Movie? Movie { get; set; }
    
    [ForeignKey("ContentId")]
    public virtual Series? Series { get; set; }
}
