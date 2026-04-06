using API.Entities;
using API.Models.Dto;

namespace API.Services;

/// <summary>Resolves what a user is allowed to do from their stored plan + expiry (no payment gateway here).</summary>
public static class SubscriptionEntitlements
{
    public const int FreePlanId = 1;

    public static bool PaidSubscriptionIsActive(AppUser user)
    {
        if (user.SubscriptionPlanId <= FreePlanId) return false;
        return user.SubscriptionEndsUtc == null || user.SubscriptionEndsUtc > DateTime.UtcNow;
    }

    public static bool HasUnlimitedLikes(AppUser user)
    {
        if (user.SubscriptionPlan == null) return false;
        if (!PaidSubscriptionIsActive(user)) return false;
        return user.SubscriptionPlan.UnlimitedLikes;
    }

    public static bool CanSeeWhoLikedYou(AppUser user)
    {
        if (user.SubscriptionPlan == null) return false;
        if (!PaidSubscriptionIsActive(user)) return false;
        return user.SubscriptionPlan.SeeWhoLikedYou;
    }

    public static int DiscoveryBoostFor(AppUser user)
    {
        if (user.SubscriptionPlan == null) return 0;
        if (!PaidSubscriptionIsActive(user)) return 0;
        return user.SubscriptionPlan.PriorityInDiscovery ? 1 : 0;
    }

    public static SubscriptionSummaryDto ToSummary(AppUser user)
    {
        var plan = user.SubscriptionPlan;
        if (plan == null)
        {
            return new SubscriptionSummaryDto
            {
                PlanId = FreePlanId,
                PlanName = "Free",
                UnlimitedLikes = false,
                SeeWhoLikedYou = false,
                PriorityInDiscovery = false,
                SubscriptionExpiresUtc = null,
                IsPaidPlanActive = false
            };
        }

        if (user.SubscriptionPlanId == FreePlanId)
        {
            return new SubscriptionSummaryDto
            {
                PlanId = plan.Id,
                PlanName = plan.Name,
                UnlimitedLikes = plan.UnlimitedLikes,
                SeeWhoLikedYou = plan.SeeWhoLikedYou,
                PriorityInDiscovery = plan.PriorityInDiscovery,
                SubscriptionExpiresUtc = null,
                IsPaidPlanActive = false
            };
        }

        var active = PaidSubscriptionIsActive(user);
        if (!active)
        {
            return new SubscriptionSummaryDto
            {
                PlanId = FreePlanId,
                PlanName = "Free",
                UnlimitedLikes = false,
                SeeWhoLikedYou = false,
                PriorityInDiscovery = false,
                SubscriptionExpiresUtc = null,
                IsPaidPlanActive = false
            };
        }

        return new SubscriptionSummaryDto
        {
            PlanId = plan.Id,
            PlanName = plan.Name,
            UnlimitedLikes = plan.UnlimitedLikes,
            SeeWhoLikedYou = plan.SeeWhoLikedYou,
            PriorityInDiscovery = plan.PriorityInDiscovery,
            SubscriptionExpiresUtc = user.SubscriptionEndsUtc,
            IsPaidPlanActive = true
        };
    }
}
