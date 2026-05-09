using MediStock.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediStock.Infrastructure.Data.Configurations;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.DoctorName).IsRequired().HasMaxLength(150);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        b.HasOne(x => x.Customer)
            .WithMany(c => c.Prescriptions)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
