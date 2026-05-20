using CosengPhotography.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CosengPhotography.Interfaces
{
  
    public interface IJwtTokenService
    {
        Task<string> GenerateTokenAsync(AppUser user);
    }

    
}
