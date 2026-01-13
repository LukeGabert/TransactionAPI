using Microsoft.EntityFrameworkCore;
using TransactionAPI.Models;

namespace TransactionAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<RiskReport> RiskReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Transaction entity
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionID);
            entity.Property(e => e.TransactionID).HasMaxLength(50);
            entity.Property(e => e.AccountID).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Merchant).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(200).IsRequired();
            
            // Configure relationship
            entity.HasMany(e => e.RiskReports)
                  .WithOne(e => e.Transaction)
                  .HasForeignKey(e => e.TransactionID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RiskReport entity
        modelBuilder.Entity<RiskReport>(entity =>
        {
            entity.HasKey(e => e.ReportID);
            entity.Property(e => e.ReportID).ValueGeneratedOnAdd();
            entity.Property(e => e.TransactionID).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DetectedAnomaly).HasMaxLength(500);
            entity.Property(e => e.RecommendedMitigation).HasMaxLength(1000);
            entity.Property(e => e.RiskLevel).IsRequired();
        });
    }
}
