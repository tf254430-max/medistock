using MediStock.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediStock.Infrastructure.Data.Configurations;

public class DrugConfiguration : IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.Property(x => x.GenericName).HasMaxLength(150);
        b.Property(x => x.NdaNumber).HasMaxLength(40);
        b.Property(x => x.Strength).IsRequired().HasMaxLength(60);
        b.Property(x => x.Barcode).HasMaxLength(60);
        b.Property(x => x.Form).HasConversion<string>().HasMaxLength(30);

        b.Ignore(x => x.CurrentStock);

        b.HasIndex(x => x.Name);
        b.HasIndex(x => x.Barcode);

        b.HasOne(x => x.Category)
            .WithMany(c => c.Drugs)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
