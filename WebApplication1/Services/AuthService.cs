using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Configurations;
using WebApplication1.DTOs.Auth;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class AuthResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public AuthResponseDto? Response { get; init; }

        public static AuthResult Fail(string error) => new() { Success = false, Error = error };
        public static AuthResult Ok(AuthResponseDto response) => new() { Success = true, Response = response };
    }

    public class ProfileResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public UserProfileDto? Profile { get; init; }

        public static ProfileResult Fail(string error) => new() { Success = false, Error = error };
        public static ProfileResult Ok(UserProfileDto profile) => new() { Success = true, Profile = profile };
    }

    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtOptions _jwtOptions;

        public AuthService(UserManager<ApplicationUser> userManager, IOptions<JwtOptions> jwtOptions)
        {
            _userManager = userManager;
            _jwtOptions = jwtOptions.Value;
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequestDto request)
        {
            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing is not null)
            {
                return AuthResult.Fail("Já existe uma conta com esse e-mail.");
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                DisplayName = request.DisplayName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return AuthResult.Fail(errors);
            }

            return AuthResult.Ok(BuildAuthResponse(user));
        }

        public async Task<AuthResult> LoginAsync(LoginRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return AuthResult.Fail("E-mail ou senha inválidos.");
            }

            var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!validPassword)
            {
                return AuthResult.Fail("E-mail ou senha inválidos.");
            }

            return AuthResult.Ok(BuildAuthResponse(user));
        }

        public async Task<ProfileResult> GetProfileAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return ProfileResult.Fail("Usuário não encontrado.");

            return ProfileResult.Ok(BuildProfileDto(user));
        }

        public async Task<ProfileResult> UpdateProfileAsync(int userId, UpdateProfileRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return ProfileResult.Fail("Usuário não encontrado.");

            user.DisplayName = request.DisplayName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return ProfileResult.Fail(errors);
            }

            return ProfileResult.Ok(BuildProfileDto(user));
        }

        private static UserProfileDto BuildProfileDto(ApplicationUser user) => new()
        {
            Id = user.Id,
            Name = user.DisplayName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber
        };

        private AuthResponseDto BuildAuthResponse(ApplicationUser user)
        {
            return new AuthResponseDto
            {
                Token = GenerateJwtToken(user),
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.DisplayName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber
                }
            };
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            if (string.IsNullOrEmpty(_jwtOptions.Key))
            {
                throw new InvalidOperationException(
                    "Jwt:Key não está configurado. Configure via user-secrets (dev) ou variável de ambiente (produção).");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.DisplayName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}