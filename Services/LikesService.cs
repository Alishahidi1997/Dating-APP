using API.Data;
using API.Entities;

namespace API.Services;

public class LikesService(ILikesRepository likesRepo, IUserRepository userRepo) : ILikesService
{
    private const int MaxLikesPerUtcDay = 20;

    public async Task<LikeAddResult> AddLikeAsync(int sourceUserId, int targetUserId, int? targetPhotoId, CancellationToken ct = default)
    {
        if (sourceUserId == targetUserId)
            return LikeAddResult.InvalidTarget;

        var sourceUser = await userRepo.GetUserByIdAsync(sourceUserId, ct);
        if (sourceUser == null)
            return LikeAddResult.InvalidTarget;

        var targetUser = await userRepo.GetUserByIdAsync(targetUserId, ct);
        if (targetUser == null)
            return LikeAddResult.InvalidTarget;

        if (await likesRepo.GetUserLikeAsync(sourceUserId, targetUserId, ct) != null)
            return LikeAddResult.AlreadyLiked;

        int? storedPhotoId;
        if (targetPhotoId.HasValue)
        {
            var photo = targetUser.Photos?.FirstOrDefault(p => p.Id == targetPhotoId.Value);
            if (photo == null)
                return LikeAddResult.InvalidPhoto;
            storedPhotoId = photo.Id;
        }
        else
        {
            var main = targetUser.Photos?.FirstOrDefault(p => p.IsMain) ?? targetUser.Photos?.FirstOrDefault();
            storedPhotoId = main?.Id;
        }

        if (!SubscriptionEntitlements.HasUnlimitedLikes(sourceUser))
        {
            var utcDayStart = DateTime.UtcNow.Date;
            var sentToday = await likesRepo.CountLikesSentOnUtcDayAsync(sourceUserId, utcDayStart, ct);
            if (sentToday >= MaxLikesPerUtcDay)
                return LikeAddResult.DailyLimitReached;
        }

        var like = new UserLike
        {
            SourceUserId = sourceUserId,
            TargetUserId = targetUserId,
            TargetPhotoId = storedPhotoId,
            LikedAt = DateTime.UtcNow
        };
        likesRepo.AddLike(like);

        return await userRepo.SaveAllAsync(ct) ? LikeAddResult.Success : LikeAddResult.Failed;
    }
}
