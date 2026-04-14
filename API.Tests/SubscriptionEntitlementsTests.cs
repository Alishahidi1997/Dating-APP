using API.Entities;
using API.Services;
using Xunit;

namespace API.Tests;

public class SubscriptionEntitlementsTests
{
    private static AppUser UserWithPlan(int planId, SubscriptionPlan plan, DateTime? endsUtc, bool autoRenew = true)
    {
        return new AppUser
        {
            UserName = "t",
            Email = "t@test.com",
            PasswordHash = "x",
            SubscriptionPlanId = planId,
            SubscriptionPlan = plan,
            SubscriptionEndsUtc = endsUtc,
            SubscriptionAutoRenew = autoRenew,
            SubscriptionRenewalDays = 30
        };
    }

    private static SubscriptionPlan Free => new()
    {
        Id = 1,
        Name = "Free",
        Description = "f",
        MonthlyPriceUsd = 0,
        UnlimitedFollows = false,
        SeeFollowersList = false,
        PriorityInFeed = false
    };

    private static SubscriptionPlan Plus => new()
    {
        Id = 2,
        Name = "Plus",
        Description = "p",
        MonthlyPriceUsd = 9.99m,
        UnlimitedFollows = true,
        SeeFollowersList = true,
        PriorityInFeed = false
    };

    [Fact]
    public void Free_user_not_paid_active()
    {
        var u = UserWithPlan(1, Free, null);
        Assert.False(SubscriptionEntitlements.PaidSubscriptionIsActive(u));
        Assert.False(SubscriptionEntitlements.HasUnlimitedFollows(u));
        Assert.False(SubscriptionEntitlements.CanSeeFollowersList(u));
    }

    [Fact]
    public void Plus_future_expiry_is_active()
    {
        var u = UserWithPlan(2, Plus, DateTime.UtcNow.AddDays(7));
        Assert.True(SubscriptionEntitlements.PaidSubscriptionIsActive(u));
        Assert.True(SubscriptionEntitlements.HasUnlimitedFollows(u));
        Assert.True(SubscriptionEntitlements.CanSeeFollowersList(u));
    }

    [Fact]
    public void Plus_past_expiry_not_active_even_if_auto_renew_flag_true()
    {
        var u = UserWithPlan(2, Plus, DateTime.UtcNow.AddDays(-1), autoRenew: true);
        Assert.False(SubscriptionEntitlements.PaidSubscriptionIsActive(u));
        Assert.False(SubscriptionEntitlements.HasUnlimitedFollows(u));
    }

    [Fact]
    public void Plus_null_expiry_treated_as_active()
    {
        var u = UserWithPlan(2, Plus, null);
        Assert.True(SubscriptionEntitlements.PaidSubscriptionIsActive(u));
        Assert.True(SubscriptionEntitlements.HasUnlimitedFollows(u));
    }

    [Fact]
    public void Summary_maps_plan_flags()
    {
        var u = UserWithPlan(2, Plus, DateTime.UtcNow.AddDays(7));
        var s = SubscriptionEntitlements.ToSummary(u);
        Assert.True(s.UnlimitedFollows);
        Assert.True(s.SeeFollowersList);
        Assert.False(s.PriorityInFeed);
    }
}
