using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionAPI.Models;

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public class RiskReport
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReportID { get; set; }

    [Required]
    [MaxLength(50)]
    public string TransactionID { get; set; } = string.Empty;

    [Required]
    public RiskLevel RiskLevel { get; set; }

    [MaxLength(500)]
    public string? DetectedAnomaly { get; set; }

    [MaxLength(1000)]
    public string? RecommendedMitigation { get; set; }

    [MaxLength(2000)]
    public string? Reasoning { get; set; }

    [MaxLength(500)]
    public string? TLDR { get; set; }

    // Navigation property
    [ForeignKey("TransactionID")]
    public virtual Transaction? Transaction { get; set; }
}
