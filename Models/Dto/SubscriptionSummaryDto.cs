namespace API.Models.Dto;

public record SubscriptionSummaryDto
{
    public int PlanId { get; init; }
    public required string PlanName { get; init; }
    public bool UnlimitedFollows { get; init; }
    public bool SeeFollowersList { get; init; }
    public bool PriorityInFeed { get; init; }
    public DateTime? SubscriptionExpiresUtc { get; init; }
    public bool IsPaidPlanActive { get; init; }
    public bool AutoRenew { get; init; }
    public int RenewalDays { get; init; }
}
