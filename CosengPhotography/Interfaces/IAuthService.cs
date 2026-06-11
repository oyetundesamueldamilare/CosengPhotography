using CosengPhotography.Shared.Dtos;
using Microsoft.AspNetCore.Identity;

namespace CosengPhotography.Interfaces
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterUserAsync(RegisterDto model);
        Task<string?> LoginAsync(LoginDto model);
        Task<IdentityResult> ChangePasswordAsync(string email, string currentPassword, string newPassword);
        Task<bool> LogoutAsync(string email);



    }
}
