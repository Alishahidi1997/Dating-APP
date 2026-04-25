namespace API.Models.Dto;

public record TagSummaryDto
{
    public required string Tag { get; init; }
    public int UserCount { get; init; }
}
