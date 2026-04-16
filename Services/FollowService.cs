using API.Data;
using API.Entities;

namespace API.Services;

public class FollowService(
    IFollowRepository followRepo,
    IUserRepository userRepo,
    IUserModerationRepository moderationRepo,
    ISubscriptionService subscriptionService) : IFollowService
{
    private const int MaxFollowsPerUtcDay = 20;

    public async Task<FollowAddResult> FollowAsync(int followerId, int followingId, CancellationToken ct = default)
    {
        if (followerId == followingId)
            return FollowAddResult.InvalidTarget;

        await subscriptionService.ReconcileUserAsync(followerId, ct);

        var follower = await userRepo.GetUserByIdAsync(followerId, ct);
        if (follower == null)
            return FollowAddResult.InvalidTarget;

        var following = await userRepo.GetUserByIdAsync(followingId, ct);
        if (following == null)
            return FollowAddResult.InvalidTarget;

        if (await moderationRepo.IsBlockedEitherWayAsync(followerId, followingId, ct))
            return FollowAddResult.BlockedUser;

        if (await followRepo.GetFollowAsync(followerId, followingId, ct) != null)
            return FollowAddResult.AlreadyFollowing;

        if (!SubscriptionEntitlements.HasUnlimitedFollows(follower))
        {
            var utcDayStart = DateTime.UtcNow.Date;
            var startedToday = await followRepo.CountFollowsStartedOnUtcDayAsync(followerId, utcDayStart, ct);
            if (startedToday >= MaxFollowsPerUtcDay)
                return FollowAddResult.DailyLimitReached;
        }

        followRepo.AddFollow(new UserFollow
        {
            FollowerId = followerId,
            FollowingId = followingId,
            FollowedAtUtc = DateTime.UtcNow
        });

        return await userRepo.SaveAllAsync(ct) ? FollowAddResult.Success : FollowAddResult.Failed;
    }

    public async Task<bool> UnfollowAsync(int followerId, int followingId, CancellationToken ct = default)
    {
        if (followerId == followingId)
            return false;

        var row = await followRepo.GetFollowAsync(followerId, followingId, ct);
        if (row == null)
            return false;

        followRepo.RemoveFollow(row);
        return await userRepo.SaveAllAsync(ct);
    }
}
