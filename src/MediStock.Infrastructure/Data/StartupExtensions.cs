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
        var existing = await mgr.FindByEmailAsync(email);
        if (existing is not null)
        {
            // Ensure the seeded password is always usable on a re-run, even if
            // the password policy changed after the user was first created.
            var token = await mgr.GeneratePasswordResetTokenAsync(existing);
            var reset = await mgr.ResetPasswordAsync(existing, token, password);
            if (!reset.Succeeded)
                Console.Error.WriteLine($"[seed] reset failed for {email}: {string.Join(", ", reset.Errors.Select(e => e.Description))}");
            if (!await mgr.IsInRoleAsync(existing, role))
                await mgr.AddToRoleAsync(existing, role);
            return;
        }

        var u = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };
        var created = await mgr.CreateAsync(u, password);
        if (!created.Succeeded)
        {
            Console.Error.WriteLine($"[seed] create failed for {email}: {string.Join(", ", created.Errors.Select(e => e.Description))}");
            return;
        }
        await mgr.AddToRoleAsync(u, role);
    }
}
