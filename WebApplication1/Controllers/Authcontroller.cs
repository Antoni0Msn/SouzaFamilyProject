using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTOs.Auth;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.Success) return BadRequest(new { message = result.Error });
            return Ok(result.Response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.Success) return Unauthorized(new { message = result.Error });
            return Ok(result.Response);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var result = await _authService.GetProfileAsync(GetUserId());
            if (!result.Success) return NotFound(new { message = result.Error });
            return Ok(result.Profile);
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe(UpdateProfileRequestDto request)
        {
            var result = await _authService.UpdateProfileAsync(GetUserId(), request);
            if (!result.Success) return BadRequest(new { message = result.Error });
            return Ok(result.Profile);
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}