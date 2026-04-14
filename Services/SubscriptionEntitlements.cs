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
        if (user.SubscriptionEndsUtc == null) return true;
        return user.SubscriptionEndsUtc > DateTime.UtcNow;
    }

    public static bool HasUnlimitedFollows(AppUser user)
    {
        if (user.SubscriptionPlan == null) return false;
        if (!PaidSubscriptionIsActive(user)) return false;
        return user.SubscriptionPlan.UnlimitedFollows;
    }

    public static bool CanSeeFollowersList(AppUser user)
    {
        if (user.SubscriptionPlan == null) return false;
        if (!PaidSubscriptionIsActive(user)) return false;
        return user.SubscriptionPlan.SeeFollowersList;
    }

    public static int FeedBoostFor(AppUser user)
    {
        if (user.SubscriptionPlan == null) return 0;
        if (!PaidSubscriptionIsActive(user)) return 0;
        return user.SubscriptionPlan.PriorityInFeed ? 1 : 0;
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
                UnlimitedFollows = false,
                SeeFollowersList = false,
                PriorityInFeed = false,
                SubscriptionExpiresUtc = null,
                IsPaidPlanActive = false,
                AutoRenew = false,
                RenewalDays = 0
            };
        }

        if (user.SubscriptionPlanId == FreePlanId)
        {
            return new SubscriptionSummaryDto
            {
                PlanId = plan.Id,
                PlanName = plan.Name,
                UnlimitedFollows = plan.UnlimitedFollows,
                SeeFollowersList = plan.SeeFollowersList,
                PriorityInFeed = plan.PriorityInFeed,
                SubscriptionExpiresUtc = null,
                IsPaidPlanActive = false,
                AutoRenew = false,
                RenewalDays = 0
            };
        }

        var active = PaidSubscriptionIsActive(user);
        if (!active)
        {
            return new SubscriptionSummaryDto
            {
                PlanId = FreePlanId,
                PlanName = "Free",
                UnlimitedFollows = false,
                SeeFollowersList = false,
                PriorityInFeed = false,
                SubscriptionExpiresUtc = null,
                IsPaidPlanActive = false,
                AutoRenew = false,
                RenewalDays = 0
            };
        }

        return new SubscriptionSummaryDto
        {
            PlanId = plan.Id,
            PlanName = plan.Name,
            UnlimitedFollows = plan.UnlimitedFollows,
            SeeFollowersList = plan.SeeFollowersList,
            PriorityInFeed = plan.PriorityInFeed,
            SubscriptionExpiresUtc = user.SubscriptionEndsUtc,
            IsPaidPlanActive = true,
            AutoRenew = user.SubscriptionAutoRenew,
            RenewalDays = user.SubscriptionRenewalDays
        };
    }
}
