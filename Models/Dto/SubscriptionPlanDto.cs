namespace API.Models.Dto;

public record SubscriptionPlanDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal MonthlyPriceUsd { get; init; }
    public bool UnlimitedFollows { get; init; }
    public bool SeeFollowersList { get; init; }
    public bool PriorityInFeed { get; init; }
}
