using API.Data;
using API.Entities;
using API.Models.Dto;

namespace API.Services;

public class SubscriptionService(IUserRepository userRepo, ISubscriptionRepository planRepo) : ISubscriptionService
{
    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetPlansAsync(CancellationToken ct = default)
    {
        var plans = await planRepo.GetAllPlansAsync(ct);
        return plans.Select(p => new SubscriptionPlanDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            MonthlyPriceUsd = p.MonthlyPriceUsd,
            UnlimitedLikes = p.UnlimitedLikes,
            SeeWhoLikedYou = p.SeeWhoLikedYou,
            PriorityInDiscovery = p.PriorityInDiscovery
        }).ToList();
    }

    public async Task<SubscriptionSummaryDto?> GetMySummaryAsync(int userId, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(userId, ct);
        return user == null ? null : SubscriptionEntitlements.ToSummary(user);
    }

    public async Task<bool> SubscribeAsync(int userId, SubscribeDto dto, CancellationToken ct = default)
    {
        if (dto.PlanId == SubscriptionEntitlements.FreePlanId)
            return false;

        var plan = await planRepo.GetPlanByIdAsync(dto.PlanId, ct);
        if (plan == null)
            return false;

        var user = await userRepo.GetUserByIdAsync(userId, ct);
        if (user == null)
            return false;

        var now = DateTime.UtcNow;
        var baseLine = user.SubscriptionEndsUtc.HasValue && user.SubscriptionEndsUtc > now
            ? user.SubscriptionEndsUtc.Value
            : now;

        user.SubscriptionPlanId = plan.Id;
        user.SubscriptionEndsUtc = baseLine.AddDays(dto.DurationDays);

        userRepo.Update(user);
        if (!await userRepo.SaveAllAsync(ct))
            return false;

        await ReconcileUserAsync(userId, ct);
        return true;
    }

    public async Task ReconcileUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(userId, ct);
        if (user == null) return;

        ApplyDiscoveryBoost(user);
        userRepo.Update(user);
        await userRepo.SaveAllAsync(ct);
    }

    private static void ApplyDiscoveryBoost(AppUser user)
    {
        user.DiscoveryBoostCached = SubscriptionEntitlements.DiscoveryBoostFor(user);
    }
}
