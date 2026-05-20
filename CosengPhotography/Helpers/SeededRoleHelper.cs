using CosengPhotography.Models;
using Microsoft.AspNetCore.Identity;


namespace CosengPhotography.Helpers
{
    public static class SeededRoleHelper
    {
        public static async Task SeedRolesAndUsersAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var configuration = scope.ServiceProvider.GetService<IConfiguration>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roleNames = { "Admin", "Photographer", "Customer" };

            foreach (var roleName in roleNames)
            {
                // Always normalize to uppercase
                var normalizedName = roleName.ToUpperInvariant();

                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole
                    {
                        Name = roleName,
                        NormalizedName = normalizedName
                    });
                }
            }

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

            try
            {
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

                    // Case-insensitive check
                    if (rolesForUser == null || !rolesForUser.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                    {
                        await userManager.AddToRoleAsync(user, "Admin");
                    }
                }
            }
            catch (Exception)
            {
                // Optional: log error
            }
        }
    }
}
