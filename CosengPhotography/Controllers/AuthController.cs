using CosengPhotography.Shared.Dtos;
using CosengPhotography.Interfaces;
using CosengPhotography.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CosengPhotography.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthController(
            UserManager<AppUser> userManager,
            IJwtTokenService jwtTokenService,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _roleManager = roleManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (model == null) return BadRequest("Invalid user data.");

            if (!await _roleManager.RoleExistsAsync(model.Role))
                return BadRequest($"Role '{model.Role}' does not exist.");

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest($"User registration failed: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return BadRequest($"Failed to assign role: {errors}");
            }

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (model == null) return BadRequest("Invalid login data.");

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized("Invalid email or password.");
            }

            var token = await _jwtTokenService.GenerateTokenAsync(user);

            return Ok(new { Token = token });
        }
    }
}

