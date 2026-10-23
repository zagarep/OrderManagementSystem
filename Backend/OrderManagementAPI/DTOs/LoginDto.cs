//This DTO receives the user's email and password when they log in.

using System.ComponentModel.DataAnnotations;

namespace OrderManagementAPI.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}

