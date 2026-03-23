using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class LikesRepository(AppDbContext context) : ILikesRepository
{
    public async Task<UserLike?> GetUserLikeAsync(int sourceUserId, int targetUserId, CancellationToken ct = default) =>
        await context.UserLikes.FindAsync([sourceUserId, targetUserId], ct);

    public async Task<AppUser?> GetUserWithLikesAsync(int userId, CancellationToken ct = default) =>
        await context.Users
            .Include(u => u.LikedUsers)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

    public void AddLike(UserLike userLike) => context.UserLikes.Add(userLike);
}
