using CirculationService.Models;
using Microsoft.EntityFrameworkCore;

namespace CirculationService.Data;

public class CirculationDbContext : DbContext
{
    public CirculationDbContext(DbContextOptions<CirculationDbContext> options) : base(options)
    {
    }

    public DbSet<BorrowRecord> BorrowRecords => Set<BorrowRecord>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BorrowRecord>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ReaderName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.BookTitle)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.FineAmount)
                .HasColumnType("numeric(18,2)");

            entity.HasIndex(x => x.ReaderId);
            entity.HasIndex(x => x.BookId);
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Amount)
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.Type)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.HasOne(x => x.BorrowRecord)
                .WithMany(b => b.Invoices)
                .HasForeignKey(x => x.BorrowRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.BorrowRecordId);
            entity.HasIndex(x => x.ReaderId);
        });
    }
}