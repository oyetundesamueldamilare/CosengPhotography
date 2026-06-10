using CosengPhotography.Data;
using CosengPhotography.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CosengPhotography.Helpers
{
    public static class SeededRoleHelper
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public static async Task SeedRolesAndUsersAsync(IServiceProvider serviceProvider)
        {
            await _semaphore.WaitAsync();

            try
            {
                using var scope = serviceProvider.CreateScope();

                // 1. Grab your EF Database Context directly to bypass RoleManager tracking quirks
                // CHANGE THIS: Replace 'ApplicationDbContext' with the actual name of your DbContext class if different
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var configuration = scope.ServiceProvider.GetService<IConfiguration>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                string[] roleNames = { "Admin", "Photographer", "Customer" };

                foreach (var roleName in roleNames)
                {
                    // Direct database query: completely accurate and independent of Identity managers
                    bool roleExists = await context.Roles.AnyAsync(r => r.Name == roleName);

                    if (!roleExists)
                    {
                        try
                        {
                            await roleManager.CreateAsync(new IdentityRole(roleName));
                        }
                        catch (DbUpdateException)
                        {
                            // Defensive guard: If PostgreSQL flags a duplicate anyway, catch it safely and keep moving
                            Console.WriteLine($"Role '{roleName}' already recognized by database constraint. Skipping.");
                        }
                    }
                }

                // --- Admin User Seeding Section ---
                var adminEmail = configuration?["SeedAdmin:Email"];
                var adminPassword = configuration?["SeedAdmin:Password"];

                if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                {
                    return;
                }

                var admin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = configuration?["SeedAdmin:FullName"] ?? "Admin",
                    EmailConfirmed = true,
                };

                AppUser? user = await userManager.FindByEmailAsync(admin.Email);

                if (user == null)
                {
                    var result = await userManager.CreateAsync(admin, adminPassword);
                    if (result.Succeeded)
                    {
                        user = await userManager.FindByEmailAsync(admin.Email);
                    }
                }

                if (user != null)
                {
                    var rolesForUser = await userManager.GetRolesAsync(user);

                    if (rolesForUser == null || !rolesForUser.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                    {
                        await userManager.AddToRoleAsync(user, "Admin");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Seeding error gracefully caught: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}