using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class FollowRepository(AppDbContext context) : IFollowRepository
{
    public async Task<UserFollow?> GetFollowAsync(int followerId, int followingId, CancellationToken ct = default) =>
        await context.UserFollows.FindAsync([followerId, followingId], ct);

    public async Task<int> CountFollowsStartedOnUtcDayAsync(int followerId, DateTime utcDayStart, CancellationToken ct = default)
    {
        var next = utcDayStart.AddDays(1);
        return await context.UserFollows
            .Where(f => f.FollowerId == followerId && f.FollowedAtUtc >= utcDayStart && f.FollowedAtUtc < next)
            .CountAsync(ct);
    }

    public void AddFollow(UserFollow follow) => context.UserFollows.Add(follow);

    public void RemoveFollow(UserFollow follow) => context.UserFollows.Remove(follow);
}
