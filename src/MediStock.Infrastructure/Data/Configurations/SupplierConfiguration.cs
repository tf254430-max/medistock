using MediStock.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediStock.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.Property(x => x.ContactPerson).HasMaxLength(120);
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.Email).HasMaxLength(120);
        b.Property(x => x.Address).HasMaxLength(250);
        b.Property(x => x.Tin).HasMaxLength(40);
        b.HasIndex(x => x.Name);
    }
}
