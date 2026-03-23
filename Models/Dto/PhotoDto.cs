namespace API.Models.Dto;

public record PhotoDto
{
    public int Id { get; init; }
    public required string Url { get; init; }
    public bool IsMain { get; init; }
}
