using System.ComponentModel.DataAnnotations;

namespace API.Models.Dto;

public record RegisterDto
{
    [Required, MinLength(3), MaxLength(50)]
    public required string UserName { get; init; }

    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required, MinLength(6), MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain uppercase, lowercase, and number")]
    public required string Password { get; init; }

    [Required]
    public required string Gender { get; init; }

    [Required]
    public required string LookingFor { get; init; }

    [MaxLength(500)]
    public string? Bio { get; init; }

    [MaxLength(100)]
    public string? KnownAs { get; init; }

    [Required]
    public DateOnly DateOfBirth { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }
}
