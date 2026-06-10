using CosengPhotography.Interfaces;
using CosengPhotography.Services;
using CosengPhotography.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CosengPhotography.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (model == null) return BadRequest("Invalid user data.");

            var result = await _authService.RegisterUserAsync(model);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest($"Registration failed: {errors}");
            }

            return Ok(new { Message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (model == null) return BadRequest("Invalid login data.");

            var token = await _authService.LoginAsync(model);

            if (token == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            return Ok(new { Token = token });
        }
    }
}