using MediStock.Domain.Entities;
using MediStock.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await SeedSettingsAsync(db, ct);
        await SeedCatalogAsync(db, ct);
    }

    private static async Task SeedSettingsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Settings.AnyAsync(ct)) return;

        db.Settings.AddRange(
            new Setting { Key = "Pharmacy.Name", Value = "MediStock Demo Pharmacy" },
            new Setting { Key = "Pharmacy.Address", Value = "Plot 12, Kampala Road, Kampala, Uganda" },
            new Setting { Key = "Pharmacy.Phone", Value = "+256 700 000 000" },
            new Setting { Key = "Pharmacy.Currency", Value = "UGX" },
            new Setting { Key = "Tax.VatRate", Value = "0.18" });

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCatalogAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Categories.AnyAsync(ct)) return;

        var categories = new[]
        {
            new Category { Name = "Analgesics", Description = "Pain relief medication" },
            new Category { Name = "Antibiotics", Description = "Bacterial infection treatment" },
            new Category { Name = "Antimalarials", Description = "Malaria treatment and prophylaxis" },
            new Category { Name = "Vitamins & Supplements", Description = "Nutritional supplements" },
            new Category { Name = "Antihypertensives", Description = "Blood pressure medication" }
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync(ct);

        var suppliers = new[]
        {
            new Supplier { Name = "Kampala Pharma Distributors", ContactPerson = "Joseph Mugisha", Phone = "+256 772 111 222", Email = "orders@kpd.co.ug", Address = "Industrial Area, Kampala", Tin = "1000123456" },
            new Supplier { Name = "East African Medical Supplies", ContactPerson = "Sarah Namatovu", Phone = "+256 752 333 444", Email = "sales@eams.co.ug", Address = "Mbale Road, Jinja", Tin = "1000234567" },
            new Supplier { Name = "Vital Health Imports", ContactPerson = "David Okello", Phone = "+256 701 555 666", Email = "info@vitalhealth.ug", Address = "Plot 9, Ntinda", Tin = "1000345678" },
            new Supplier { Name = "Uganda Drug House", ContactPerson = "Grace Atim", Phone = "+256 703 777 888", Email = "udh@drughouse.ug", Address = "Bwaise, Kampala", Tin = "1000456789" },
            new Supplier { Name = "Mbarara Wholesale Pharma", ContactPerson = "Isaac Tumusiime", Phone = "+256 782 999 000", Email = "wholesale@mbpharma.ug", Address = "High Street, Mbarara", Tin = "1000567890" }
        };
        db.Suppliers.AddRange(suppliers);
        await db.SaveChangesAsync(ct);

        var analgesics = categories[0];
        var antibiotics = categories[1];
        var antimalarials = categories[2];
        var vitamins = categories[3];
        var antihypertensives = categories[4];

        var drugs = new[]
        {
            new Drug { Name = "Panadol", GenericName = "Paracetamol", NdaNumber = "NDA-2018-0010", CategoryId = analgesics.Id, Form = DosageForm.Tablet, Strength = "500mg", Barcode = "6001000000017", ReorderLevel = 50 },
            new Drug { Name = "Brufen", GenericName = "Ibuprofen", NdaNumber = "NDA-2019-0231", CategoryId = analgesics.Id, Form = DosageForm.Tablet, Strength = "400mg", Barcode = "6001000000024", ReorderLevel = 40 },
            new Drug { Name = "Amoxil", GenericName = "Amoxicillin", NdaNumber = "NDA-2017-1148", CategoryId = antibiotics.Id, Form = DosageForm.Capsule, Strength = "500mg", RequiresPrescription = true, Barcode = "6001000000031", ReorderLevel = 30 },
            new Drug { Name = "Flagyl", GenericName = "Metronidazole", NdaNumber = "NDA-2020-2234", CategoryId = antibiotics.Id, Form = DosageForm.Tablet, Strength = "400mg", RequiresPrescription = true, Barcode = "6001000000048", ReorderLevel = 30 },
            new Drug { Name = "Coartem", GenericName = "Artemether/Lumefantrine", NdaNumber = "NDA-2016-0045", CategoryId = antimalarials.Id, Form = DosageForm.Tablet, Strength = "20/120mg", RequiresPrescription = true, Barcode = "6001000000055", ReorderLevel = 25 },
            new Drug { Name = "Lonart", GenericName = "Artemether/Lumefantrine", NdaNumber = "NDA-2016-0046", CategoryId = antimalarials.Id, Form = DosageForm.Suspension, Strength = "180/1080mg", RequiresPrescription = true, Barcode = "6001000000062", ReorderLevel = 15 },
            new Drug { Name = "Vitamin C", GenericName = "Ascorbic Acid", NdaNumber = "NDA-2015-0212", CategoryId = vitamins.Id, Form = DosageForm.Tablet, Strength = "500mg", Barcode = "6001000000079", ReorderLevel = 60 },
            new Drug { Name = "Multivite", GenericName = "Multivitamin", NdaNumber = "NDA-2020-1500", CategoryId = vitamins.Id, Form = DosageForm.Capsule, Strength = "Adult", Barcode = "6001000000086", ReorderLevel = 40 },
            new Drug { Name = "Norvasc", GenericName = "Amlodipine", NdaNumber = "NDA-2014-0303", CategoryId = antihypertensives.Id, Form = DosageForm.Tablet, Strength = "5mg", RequiresPrescription = true, Barcode = "6001000000093", ReorderLevel = 25 },
            new Drug { Name = "Lopressor", GenericName = "Metoprolol", NdaNumber = "NDA-2014-0304", CategoryId = antihypertensives.Id, Form = DosageForm.Tablet, Strength = "50mg", RequiresPrescription = true, Barcode = "6001000000109", ReorderLevel = 25 }
        };
        db.Drugs.AddRange(drugs);
        await db.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        var batches = new[]
        {
            new Batch { DrugId = drugs[0].Id, BatchNumber = "PAN-2026-001", SupplierId = suppliers[0].Id, QuantityIn = 200, QuantityRemaining = 200, CostPrice = 100m, SellPrice = 200m, ExpiryDate = now.AddMonths(18), ReceivedAt = now.AddDays(-30) },
            new Batch { DrugId = drugs[1].Id, BatchNumber = "BRU-2026-001", SupplierId = suppliers[0].Id, QuantityIn = 150, QuantityRemaining = 150, CostPrice = 150m, SellPrice = 350m, ExpiryDate = now.AddMonths(20), ReceivedAt = now.AddDays(-25) },
            new Batch { DrugId = drugs[2].Id, BatchNumber = "AMX-2026-007", SupplierId = suppliers[1].Id, QuantityIn = 100, QuantityRemaining = 100, CostPrice = 800m, SellPrice = 1500m, ExpiryDate = now.AddMonths(14), ReceivedAt = now.AddDays(-20) },
            new Batch { DrugId = drugs[3].Id, BatchNumber = "FLG-2026-002", SupplierId = suppliers[1].Id, QuantityIn = 120, QuantityRemaining = 120, CostPrice = 200m, SellPrice = 400m, ExpiryDate = now.AddMonths(16), ReceivedAt = now.AddDays(-18) },
            new Batch { DrugId = drugs[4].Id, BatchNumber = "COA-2026-009", SupplierId = suppliers[2].Id, QuantityIn = 80, QuantityRemaining = 80, CostPrice = 4500m, SellPrice = 8000m, ExpiryDate = now.AddDays(60), ReceivedAt = now.AddDays(-60) },
            new Batch { DrugId = drugs[5].Id, BatchNumber = "LON-2026-002", SupplierId = suppliers[2].Id, QuantityIn = 50, QuantityRemaining = 50, CostPrice = 9000m, SellPrice = 14000m, ExpiryDate = now.AddMonths(10), ReceivedAt = now.AddDays(-15) },
            new Batch { DrugId = drugs[6].Id, BatchNumber = "VTC-2026-011", SupplierId = suppliers[3].Id, QuantityIn = 250, QuantityRemaining = 250, CostPrice = 50m, SellPrice = 150m, ExpiryDate = now.AddMonths(22), ReceivedAt = now.AddDays(-10) },
            new Batch { DrugId = drugs[7].Id, BatchNumber = "MTV-2026-005", SupplierId = suppliers[3].Id, QuantityIn = 120, QuantityRemaining = 120, CostPrice = 600m, SellPrice = 1200m, ExpiryDate = now.AddMonths(15), ReceivedAt = now.AddDays(-12) },
            new Batch { DrugId = drugs[8].Id, BatchNumber = "NRV-2026-004", SupplierId = suppliers[4].Id, QuantityIn = 90, QuantityRemaining = 90, CostPrice = 700m, SellPrice = 1300m, ExpiryDate = now.AddDays(85), ReceivedAt = now.AddDays(-30) },
            new Batch { DrugId = drugs[9].Id, BatchNumber = "LPS-2026-003", SupplierId = suppliers[4].Id, QuantityIn = 70, QuantityRemaining = 70, CostPrice = 800m, SellPrice = 1500m, ExpiryDate = now.AddMonths(12), ReceivedAt = now.AddDays(-22) }
        };
        db.Batches.AddRange(batches);
        await db.SaveChangesAsync(ct);

        var customers = new[]
        {
            new Customer { FullName = "Mary Nakato", Phone = "+256 772 100 001", NIN = "CM91012340000A", IsRecurring = true },
            new Customer { FullName = "John Ssekandi", Phone = "+256 772 100 002", NIN = "CM85060450000B" },
            new Customer { FullName = "Ruth Akello", Phone = "+256 772 100 003", NIN = "CF92110210000C", IsRecurring = true },
            new Customer { FullName = "Peter Mugume", Phone = "+256 772 100 004", NIN = "CM78030170000D" },
            new Customer { FullName = "Esther Anena", Phone = "+256 772 100 005", NIN = "CF90091230000E" }
        };
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync(ct);
    }
}
