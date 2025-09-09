using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movora.Infrastructure.SqlModels;

[Table("search_history")]
public class SearchHistory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }
    
    [Required]
    [Column("query")]
    [MaxLength(1000)]
    public string Query { get; set; } = string.Empty;
    
    [Column("results", TypeName = "jsonb")]
    public string? Results { get; set; } // Store search results as JSON
    
    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [Column("search_type")]
    [MaxLength(50)]
    public string? SearchType { get; set; } // e.g., "text", "semantic", "advanced"
    
    [Column("result_count")]
    public int ResultCount { get; set; } = 0;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
