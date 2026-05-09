using MediStock.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediStock.Infrastructure.Data.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(40);
        b.Property(x => x.MobileMoneyRef).HasMaxLength(80);
        b.Property(x => x.VoidReason).HasMaxLength(400);
        b.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);

        b.Property(x => x.SubTotal).HasConversion<string>();
        b.Property(x => x.Discount).HasConversion<string>();
        b.Property(x => x.VatAmount).HasConversion<string>();
        b.Property(x => x.Total).HasConversion<string>();

        b.HasIndex(x => x.ReceiptNumber).IsUnique();
        b.HasIndex(x => x.CompletedAt);

        b.HasOne(x => x.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.Prescription)
            .WithMany()
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.Cashier)
            .WithMany()
            .HasForeignKey(x => x.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.VoidedBy)
            .WithMany()
            .HasForeignKey(x => x.VoidedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
