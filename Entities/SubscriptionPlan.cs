namespace API.Entities;

public class SubscriptionPlan
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal MonthlyPriceUsd { get; set; }

    public bool UnlimitedLikes { get; set; }
    public bool SeeWhoLikedYou { get; set; }
    public bool PriorityInDiscovery { get; set; }

    public ICollection<AppUser> Subscribers { get; set; } = [];
}
