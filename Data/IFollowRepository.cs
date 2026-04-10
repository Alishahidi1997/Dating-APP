using API.Entities;

namespace API.Data;

public interface IFollowRepository
{
    Task<UserFollow?> GetFollowAsync(int followerId, int followingId, CancellationToken ct = default);
    Task<int> CountFollowsStartedOnUtcDayAsync(int followerId, DateTime utcDayStart, CancellationToken ct = default);
    void AddFollow(UserFollow follow);
    void RemoveFollow(UserFollow follow);
}
