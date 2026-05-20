using Microsoft.AspNetCore.Identity;

namespace CosengPhotography.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}
