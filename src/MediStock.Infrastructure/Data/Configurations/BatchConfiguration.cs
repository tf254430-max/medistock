using MediStock.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediStock.Infrastructure.Data.Configurations;

public class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.BatchNumber).IsRequired().HasMaxLength(60);

        // Decimal stored as TEXT in SQLite preserves precision
        b.Property(x => x.CostPrice).HasConversion<string>();
        b.Property(x => x.SellPrice).HasConversion<string>();

        b.HasIndex(x => new { x.DrugId, x.ExpiryDate });

        b.HasOne(x => x.Drug)
            .WithMany(d => d.Batches)
            .HasForeignKey(x => x.DrugId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Supplier)
            .WithMany(s => s.Batches)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
