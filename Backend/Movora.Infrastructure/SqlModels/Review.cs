using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movora.Infrastructure.SqlModels;

[Table("reviews")]
public class Review
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
    [Column("rating")]
    [Range(1, 10)]
    public int Rating { get; set; }
    
    [Column("review_text", TypeName = "text")]
    public string? ReviewText { get; set; }
    
    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [Column("updated_date")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    
    [Column("is_spoiler")]
    public bool IsSpoiler { get; set; } = false;
    
    [Column("helpfulness_score")]
    public int HelpfulnessScore { get; set; } = 0;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey("ContentId")]
    public virtual Movie? Movie { get; set; }
    
    [ForeignKey("ContentId")]
    public virtual Series? Series { get; set; }
    
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
