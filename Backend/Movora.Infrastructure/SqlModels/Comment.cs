using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movora.Infrastructure.SqlModels;

[Table("comments")]
public class Comment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }
    
    [Required]
    [Column("review_id")]
    public Guid ReviewId { get; set; }
    
    [Required]
    [Column("comment_text", TypeName = "text")]
    public string CommentText { get; set; } = string.Empty;
    
    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [Column("updated_date")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey("ReviewId")]
    public virtual Review Review { get; set; } = null!;
}
