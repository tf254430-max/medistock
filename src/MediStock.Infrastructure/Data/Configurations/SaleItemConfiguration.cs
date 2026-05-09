using MediStock.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediStock.Infrastructure.Data.Configurations;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.UnitPrice).HasConversion<string>();
        b.Property(x => x.LineTotal).HasConversion<string>();

        b.HasOne(x => x.Sale)
            .WithMany(s => s.Items)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Drug)
            .WithMany()
            .HasForeignKey(x => x.DrugId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Batch)
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
