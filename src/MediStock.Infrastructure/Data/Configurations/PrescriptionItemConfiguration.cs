using MediStock.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediStock.Infrastructure.Data.Configurations;

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Dosage).IsRequired().HasMaxLength(250);

        b.HasOne(x => x.Prescription)
            .WithMany(p => p.Items)
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Drug)
            .WithMany()
            .HasForeignKey(x => x.DrugId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
