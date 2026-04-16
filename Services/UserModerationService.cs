using API.Data;
using API.Entities;

namespace API.Services;

public class UserModerationService(
    IUserRepository userRepo,
    IUserModerationRepository moderationRepo) : IUserModerationService
{
    public Task<bool> IsBlockedEitherWayAsync(int userIdA, int userIdB, CancellationToken ct = default) =>
        moderationRepo.IsBlockedEitherWayAsync(userIdA, userIdB, ct);

    public async Task<bool> BlockAsync(int blockerId, int blockedId, CancellationToken ct = default)
    {
        if (blockerId == blockedId) return false;

        var blocked = await userRepo.GetUserByIdAsync(blockedId, ct);
        if (blocked == null) return false;

        if (await moderationRepo.ExistsBlockAsync(blockerId, blockedId, ct))
            return true;

        await moderationRepo.RemoveFollowsBetweenAsync(blockerId, blockedId, ct);
        await moderationRepo.RemoveMutesBetweenAsync(blockerId, blockedId, ct);

        moderationRepo.AddBlock(new UserBlock
        {
            BlockerId = blockerId,
            BlockedId = blockedId,
            CreatedUtc = DateTime.UtcNow
        });

        return await userRepo.SaveAllAsync(ct);
    }

    public async Task<bool> UnblockAsync(int blockerId, int blockedId, CancellationToken ct = default)
    {
        if (blockerId == blockedId) return false;

        var row = await moderationRepo.GetBlockAsync(blockerId, blockedId, ct);
        if (row == null) return false;

        moderationRepo.RemoveBlock(row);
        return await userRepo.SaveAllAsync(ct);
    }

    public async Task<bool> MuteAsync(int muterId, int mutedId, CancellationToken ct = default)
    {
        if (muterId == mutedId) return false;

        var muted = await userRepo.GetUserByIdAsync(mutedId, ct);
        if (muted == null) return false;

        if (await moderationRepo.IsBlockedEitherWayAsync(muterId, mutedId, ct))
            return false;

        if (await moderationRepo.ExistsMuteAsync(muterId, mutedId, ct))
            return true;

        moderationRepo.AddMute(new UserMute
        {
            MuterId = muterId,
            MutedId = mutedId,
            CreatedUtc = DateTime.UtcNow
        });

        return await userRepo.SaveAllAsync(ct);
    }

    public async Task<bool> UnmuteAsync(int muterId, int mutedId, CancellationToken ct = default)
    {
        if (muterId == mutedId) return false;

        var row = await moderationRepo.GetMuteAsync(muterId, mutedId, ct);
        if (row == null) return false;

        moderationRepo.RemoveMute(row);
        return await userRepo.SaveAllAsync(ct);
    }
}
