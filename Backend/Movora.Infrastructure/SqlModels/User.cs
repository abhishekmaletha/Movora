using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movora.Infrastructure.SqlModels;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [Column("email")]
    [MaxLength(320)] // Standard max email length
    public string Email { get; set; } = string.Empty;
    
    [Column("google_id")]
    [MaxLength(100)]
    public string? GoogleId { get; set; }
    
    [Column("preferences", TypeName = "jsonb")]
    public string? Preferences { get; set; }
    
    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [Column("updated_date")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<UserWatched> WatchedContent { get; set; } = new List<UserWatched>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
    public virtual ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
}
