using CosengPhotography.Shared.Dtos;
using Microsoft.AspNetCore.Identity;

namespace CosengPhotography.Interfaces
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterUserAsync(RegisterDto model);
        Task<string?> LoginAsync(LoginDto model);
    }
}
