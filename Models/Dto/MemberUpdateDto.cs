using System.ComponentModel.DataAnnotations;

namespace API.Models.Dto;

public record MemberUpdateDto
{
    [MaxLength(100)]
    public string? KnownAs { get; init; }

    [MaxLength(500)]
    public string? Bio { get; init; }

    public string? Gender { get; init; }
    public string? LookingFor { get; init; }
    public DateOnly? DateOfBirth { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }
}
