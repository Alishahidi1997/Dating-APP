using System.ComponentModel.DataAnnotations;

namespace API.Models.Dto;

public record LoginDto
{
    [Required]
    public required string UserName { get; init; }

    [Required]
    public required string Password { get; init; }
}
