using API.Entities;

namespace API.Data;

public interface ILikesRepository
{
    Task<UserLike?> GetUserLikeAsync(int sourceUserId, int targetUserId, CancellationToken ct = default);
    Task<AppUser?> GetUserWithLikesAsync(int userId, CancellationToken ct = default);
    void AddLike(UserLike userLike);
}
