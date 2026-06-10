using CosengPhotography.Data; // Ensure your AppDbContext namespace is imported
using CosengPhotography.Interfaces;
using CosengPhotography.Models;
using CosengPhotography.Shared.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CosengPhotography.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly AppDbContext _context; // Injecting context to manage transactions

        public AuthService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IJwtTokenService jwtTokenService,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenService = jwtTokenService;
            _context = context;
        }

        public async Task<IdentityResult> RegisterUserAsync(RegisterDto model)
        {
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                return IdentityResult.Failed(new IdentityError { Description = $"Role '{model.Role}' does not exist." });
            }

            // 1. Get the configured execution strategy (NpgsqlRetryingExecutionStrategy)
            var strategy = _context.Database.CreateExecutionStrategy();

            // 2. Execute the entire transaction block inside the strategy
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = new AppUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        EmailConfirmed = true
                    };

                    var createResult = await _userManager.CreateAsync(user, model.Password);
                    if (!createResult.Succeeded)
                    {
                        return createResult;
                    }

                    var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                    if (!roleResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return roleResult;
                    }

                    await transaction.CommitAsync();
                    return IdentityResult.Success;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw; // Retrying strategy catches this and knows whether to try again
                }
            });
        }

        public async Task<string?> LoginAsync(LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return null; // Invalid credentials
            }

            return await _jwtTokenService.GenerateTokenAsync(user);
        }
    }
}