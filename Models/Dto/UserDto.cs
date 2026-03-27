namespace API.Models.Dto;

public record UserDto
{
    public int Id { get; init; }
    public required string UserName { get; init; }
    public required string KnownAs { get; init; }
    public int Age { get; init; }
    public string? Bio { get; init; }
    public required string Gender { get; init; }
    public required string LookingFor { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? JobTitle { get; init; }
    public string? PhotoUrl { get; init; }
    public DateTime LastActive { get; init; }
    public DateTime Created { get; init; }
    public IReadOnlyList<PhotoDto> Photos { get; init; } = [];
    public IReadOnlyList<HobbyDto> Hobbies { get; init; } = [];
}
