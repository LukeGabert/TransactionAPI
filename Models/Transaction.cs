using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionAPI.Models;

public class Transaction
{
    [Key]
    [MaxLength(50)]
    public string TransactionID { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AccountID { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(200)]
    public string Merchant { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; set; }

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    // Navigation property
    public virtual ICollection<RiskReport> RiskReports { get; set; } = new List<RiskReport>();
}
