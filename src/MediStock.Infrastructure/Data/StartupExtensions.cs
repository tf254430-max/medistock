using MediStock.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediStock.Infrastructure.Data;

public static class StartupExtensions
{
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        Directory.CreateDirectory("data");

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var role in new[] { "Admin", "Pharmacist", "Cashier" })
        {
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));
        }

        await SeedUserAsync(userMgr, "admin@medistock.local", "Admin@123", "Admin", "Admin User");
        await SeedUserAsync(userMgr, "pharmacist@medistock.local", "Pharma@123", "Pharmacist", "Pharmacist User");
        await SeedUserAsync(userMgr, "cashier@medistock.local", "Cash@123", "Cashier", "Cashier User");

        await DataSeeder.SeedAsync(db);
    }

    private static async Task SeedUserAsync(
        UserManager<AppUser> mgr, string email, string password, string role, string fullName)
    {
        if (await mgr.FindByEmailAsync(email) is not null) return;

        var u = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };
        await mgr.CreateAsync(u, password);
        await mgr.AddToRoleAsync(u, role);
    }
}
