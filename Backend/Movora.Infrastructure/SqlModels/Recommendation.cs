using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movora.Infrastructure.SqlModels;

[Table("recommendations")]
public class Recommendation
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
    
    [Required]
    [Column("score")]
    [Range(0.0, 1.0)]
    public decimal Score { get; set; } // Recommendation confidence score 0-1
    
    [Column("reasoning", TypeName = "text")]
    public string? Reasoning { get; set; } // AI-generated explanation
    
    [Column("recommendation_type")]
    [MaxLength(50)]
    public string? RecommendationType { get; set; } // e.g., "collaborative", "content-based", "hybrid"
    
    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [Column("viewed_date")]
    public DateTime? ViewedDate { get; set; }
    
    [Column("is_dismissed")]
    public bool IsDismissed { get; set; } = false;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey("ContentId")]
    public virtual Movie? Movie { get; set; }
    
    [ForeignKey("ContentId")]
    public virtual Series? Series { get; set; }
}
