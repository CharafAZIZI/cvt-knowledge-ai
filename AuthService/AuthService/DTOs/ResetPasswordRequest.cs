using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class ResetPasswordRequest
{
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}