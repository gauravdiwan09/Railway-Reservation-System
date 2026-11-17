using System.ComponentModel.DataAnnotations;
namespace AuthService.DTOs;
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress] //something.something.something
        [RegularExpression(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "Please enter a valid email address with a domain extension.")]
        public string Email {get; set;} = string.Empty;

        [Required]
        // [RegularExpression(
        // @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$", 
        // ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
        public string Password {get; set;} = string.Empty;
    }