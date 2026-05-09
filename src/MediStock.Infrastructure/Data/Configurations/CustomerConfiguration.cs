using MediStock.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediStock.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.NIN).HasMaxLength(40);
        b.HasIndex(x => x.Phone);
        b.HasIndex(x => x.FullName);
    }
}
