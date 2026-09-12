using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6, ErrorMessage = "A senha precisa ter pelo menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string DisplayName { get; set; } = string.Empty;
    }

    public class LoginRequestDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
    }
}