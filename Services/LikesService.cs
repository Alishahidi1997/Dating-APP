using API.Data;
using API.Entities;

namespace API.Services;

public class LikesService(ILikesRepository likesRepo, IUserRepository userRepo) : ILikesService
{
    public async Task<bool> AddLikeAsync(int sourceUserId, int targetUserId, CancellationToken ct = default)
    {
        if (sourceUserId == targetUserId) return false;

        var targetUser = await userRepo.GetUserByIdAsync(targetUserId, ct);
        if (targetUser == null) return false;

        if (await likesRepo.GetUserLikeAsync(sourceUserId, targetUserId, ct) != null)
            return true; // Already liked

        var like = new UserLike { SourceUserId = sourceUserId, TargetUserId = targetUserId };
        likesRepo.AddLike(like);

        return await userRepo.SaveAllAsync(ct);
    }
}
