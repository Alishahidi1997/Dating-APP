namespace API.Entities;

public class SubscriptionPlan
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal MonthlyPriceUsd { get; set; }

    public bool UnlimitedFollows { get; set; }
    public bool SeeFollowersList { get; set; }
    public bool PriorityInFeed { get; set; }

    public ICollection<AppUser> Subscribers { get; set; } = [];
}
