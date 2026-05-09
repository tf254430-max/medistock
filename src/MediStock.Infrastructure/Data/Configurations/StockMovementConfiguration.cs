using MediStock.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediStock.Infrastructure.Data.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Reference).HasMaxLength(80);
        b.Property(x => x.Notes).HasMaxLength(400);
        b.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => x.PerformedAt);

        b.HasOne(x => x.Batch)
            .WithMany(bt => bt.Movements)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.PerformedBy)
            .WithMany()
            .HasForeignKey(x => x.PerformedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
