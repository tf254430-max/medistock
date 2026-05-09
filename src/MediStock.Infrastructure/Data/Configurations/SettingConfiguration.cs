using MediStock.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediStock.Infrastructure.Data.Configurations;

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).IsRequired().HasMaxLength(80);
        b.Property(x => x.Value).HasMaxLength(500);
        b.HasIndex(x => x.Key).IsUnique();
    }
}
