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
            UnlimitedFollows = p.UnlimitedFollows,
            SeeFollowersList = p.SeeFollowersList,
            PriorityInFeed = p.PriorityInFeed
        }).ToList();
    }

    public async Task<SubscriptionSummaryDto?> GetMySummaryAsync(int userId, CancellationToken ct = default)
    {
        await ReconcileUserAsync(userId, ct);
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
        user.SubscriptionAutoRenew = dto.AutoRenew;
        user.SubscriptionRenewalDays = dto.RenewalDays;

        userRepo.Update(user);
        if (!await userRepo.SaveAllAsync(ct))
            return false;

        await ReconcileUserAsync(userId, ct);
        return true;
    }

    public async Task<bool> CancelAsync(int userId, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(userId, ct);
        if (user == null) return false;
        if (user.SubscriptionPlanId == SubscriptionEntitlements.FreePlanId) return true;

        user.SubscriptionAutoRenew = false;
        userRepo.Update(user);
        return await userRepo.SaveAllAsync(ct);
    }

    public async Task<bool> SetAutoRenewAsync(int userId, bool enabled, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(userId, ct);
        if (user == null) return false;
        if (user.SubscriptionPlanId == SubscriptionEntitlements.FreePlanId) return false;

        user.SubscriptionAutoRenew = enabled;
        userRepo.Update(user);
        return await userRepo.SaveAllAsync(ct);
    }

    public async Task ReconcileUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(userId, ct);
        if (user == null) return;

        if (user.SubscriptionPlanId > SubscriptionEntitlements.FreePlanId &&
            user.SubscriptionEndsUtc.HasValue &&
            user.SubscriptionEndsUtc.Value <= DateTime.UtcNow)
        {
            if (user.SubscriptionAutoRenew)
            {
                var renewal = Math.Max(user.SubscriptionRenewalDays, 1);
                user.SubscriptionEndsUtc = DateTime.UtcNow.AddDays(renewal);
            }
            else
            {
                user.SubscriptionPlanId = SubscriptionEntitlements.FreePlanId;
                user.SubscriptionEndsUtc = null;
                user.SubscriptionRenewalDays = 30;
                user.SubscriptionAutoRenew = true;
            }
        }

        ApplyFeedBoost(user);
        userRepo.Update(user);
        await userRepo.SaveAllAsync(ct);
    }

    private static void ApplyFeedBoost(AppUser user)
    {
        user.FeedBoostCached = SubscriptionEntitlements.FeedBoostFor(user);
    }
}
