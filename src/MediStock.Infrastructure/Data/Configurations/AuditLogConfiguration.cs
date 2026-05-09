using MediStock.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediStock.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityName).IsRequired().HasMaxLength(80);
        b.Property(x => x.EntityId).IsRequired().HasMaxLength(80);
        b.Property(x => x.Action).IsRequired().HasMaxLength(40);
        b.Property(x => x.IpAddress).HasMaxLength(60);

        b.HasIndex(x => x.OccurredAt);
        b.HasIndex(x => new { x.EntityName, x.EntityId });

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
