using API.Entities;
using API.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class PostRepository(AppDbContext context) : IPostRepository
{
    public void Add(Post post) => context.Posts.Add(post);

    public void Remove(Post post) => context.Posts.Remove(post);

    public async Task<Post?> GetByIdAsync(int postId, CancellationToken ct = default) =>
        await context.Posts
            .Include(p => p.Author).ThenInclude(a => a.Photos)
            .Include(p => p.PostPhotos).ThenInclude(pp => pp.Photo)
            .FirstOrDefaultAsync(p => p.Id == postId && p.DeletedUtc == null, ct);

    public async Task<IReadOnlyList<int>> GetOwnedPhotoIdsAsync(int userId, IReadOnlyCollection<int> photoIds, CancellationToken ct = default)
    {
        if (photoIds.Count == 0)
            return [];

        return await context.Photos
            .Where(p => p.AppUserId == userId && photoIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);
    }

    public async Task<PagedResultDto<Post>> GetHomeTimelineAsync(int viewerUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Min(50, Math.Max(1, pageSize));

        var followingIds = context.UserFollows
            .Where(f => f.FollowerId == viewerUserId)
            .Select(f => f.FollowingId);

        var query = context.Posts
            .Include(p => p.Author).ThenInclude(a => a.Photos)
            .Include(p => p.PostPhotos).ThenInclude(pp => pp.Photo)
            .Where(p => p.DeletedUtc == null)
            .Where(p => followingIds.Contains(p.AuthorId))
            .Where(p => !context.UserBlocks.Any(b =>
                (b.BlockerId == viewerUserId && b.BlockedId == p.AuthorId) ||
                (b.BlockerId == p.AuthorId && b.BlockedId == viewerUserId)))
            .Where(p => p.Visibility == PostVisibility.Public || context.UserFollows.Any(f =>
                f.FollowerId == viewerUserId && f.FollowingId == p.AuthorId))
            .OrderByDescending(p => p.CreatedUtc);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(ct);

        return new PagedResultDto<Post>(items, totalCount, normalizedPage, normalizedPageSize);
    }

    public async Task<PagedResultDto<Post>> GetUserTimelineAsync(int viewerUserId, string username, int page, int pageSize, CancellationToken ct = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Min(50, Math.Max(1, pageSize));
        var normalizedUsername = username.Trim().ToLowerInvariant();

        var author = await context.Users.FirstOrDefaultAsync(u => u.UserName == normalizedUsername, ct);
        if (author == null)
            return new PagedResultDto<Post>([], 0, normalizedPage, normalizedPageSize);

        var blockedEitherWay = await context.UserBlocks.AnyAsync(b =>
            (b.BlockerId == viewerUserId && b.BlockedId == author.Id) ||
            (b.BlockerId == author.Id && b.BlockedId == viewerUserId), ct);
        if (blockedEitherWay)
            return new PagedResultDto<Post>([], 0, normalizedPage, normalizedPageSize);

        var viewerFollowsAuthor = await context.UserFollows.AnyAsync(f =>
            f.FollowerId == viewerUserId && f.FollowingId == author.Id, ct);

        var query = context.Posts
            .Include(p => p.Author).ThenInclude(a => a.Photos)
            .Include(p => p.PostPhotos).ThenInclude(pp => pp.Photo)
            .Where(p => p.DeletedUtc == null && p.AuthorId == author.Id)
            .Where(p => p.Visibility == PostVisibility.Public || p.AuthorId == viewerUserId || viewerFollowsAuthor)
            .OrderByDescending(p => p.CreatedUtc);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(ct);

        return new PagedResultDto<Post>(items, totalCount, normalizedPage, normalizedPageSize);
    }
}
