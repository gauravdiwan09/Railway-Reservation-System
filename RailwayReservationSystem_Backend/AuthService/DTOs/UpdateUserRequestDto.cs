using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class UpdateUserRequestDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
}