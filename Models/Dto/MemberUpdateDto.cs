using System.ComponentModel.DataAnnotations;

namespace API.Models.Dto;

public record MemberUpdateDto
{
    [MaxLength(100)]
    public string? KnownAs { get; init; }

    [MaxLength(1000)]
    public string? Bio { get; init; }

    public string? Gender { get; init; }
    public string? LookingFor { get; init; }
    public DateOnly? DateOfBirth { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }

    [MaxLength(100)]
    public string? JobTitle { get; init; }

    public IReadOnlyList<int>? HobbyIds { get; init; }
}
