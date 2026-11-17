using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;
public class RegisterRequestDto
{   
    [Required]
    public string FullName {get; set;} = string.Empty;

    [Required]
    [EmailAddress]
    [RegularExpression(
    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
    ErrorMessage = "Please enter a valid email address with a domain extension.")]
    public string Email {get; set;} = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password {get; set;} = string.Empty;

    public string Phone {get; set;} = string.Empty;
}