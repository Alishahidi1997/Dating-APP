namespace API.Services;

public interface IUserModerationService
{
    Task<bool> BlockAsync(int blockerId, int blockedId, CancellationToken ct = default);
    Task<bool> UnblockAsync(int blockerId, int blockedId, CancellationToken ct = default);
    Task<bool> MuteAsync(int muterId, int mutedId, CancellationToken ct = default);
    Task<bool> UnmuteAsync(int muterId, int mutedId, CancellationToken ct = default);
    Task<bool> IsBlockedEitherWayAsync(int userIdA, int userIdB, CancellationToken ct = default);
}
