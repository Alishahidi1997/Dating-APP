using API.Entities;
using API.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public void Add(AppUser user) => context.Users.Add(user);
    public void Delete(AppUser user) => context.Users.Remove(user);
    public void Update(AppUser user) => context.Entry(user).State = EntityState.Modified;

    public async Task<bool> SaveAllAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct) > 0;

    public async Task<IEnumerable<AppUser>> GetUsersAsync(CancellationToken ct = default) =>
        await context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Photos)
            .Include(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .ToListAsync(ct);

    public async Task<AppUser?> GetUserByIdAsync(int id, CancellationToken ct = default) =>
        await context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Photos)
            .Include(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<AppUser?> GetUserByUsernameAsync(string username, CancellationToken ct = default) =>
        await context.Users
            .FirstOrDefaultAsync(u => u.UserName == username, ct);

    public async Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<AppUser?> GetUserByUsernameWithPhotosAsync(string username, CancellationToken ct = default) =>
        await context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Photos)
            .Include(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .FirstOrDefaultAsync(u => u.UserName == username, ct);

    public async Task<PagedResultDto<AppUser>> GetUsersForFeedAsync(int userId, UserParams userParams, CancellationToken ct = default)
    {
        var query = context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Photos)
            .Include(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .Where(u => u.Id != userId);

        var hobbyIds = ParseHobbyIds(userParams.HobbyIds);
        if (hobbyIds.Count > 0)
            query = query.Where(u => u.UserHobbies.Any(uh => hobbyIds.Contains(uh.HobbyId)));

        var followingIds = await context.UserFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync(ct);
        query = query.Where(u => !followingIds.Contains(u.Id));

        query = query.Where(u =>
            !context.UserBlocks.Any(b =>
                (b.BlockerId == userId && b.BlockedId == u.Id) ||
                (b.BlockerId == u.Id && b.BlockedId == userId)) &&
            !context.UserMutes.Any(m => m.MuterId == userId && m.MutedId == u.Id));

        query = userParams.OrderBy.ToLowerInvariant() switch
        {
            "created" => query.OrderByDescending(u => u.FeedBoostCached).ThenByDescending(u => u.Created),
            _ => query.OrderByDescending(u => u.FeedBoostCached).ThenByDescending(u => u.LastActive)
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((userParams.PageNumber - 1) * userParams.PageSize)
            .Take(userParams.PageSize)
            .ToListAsync(ct);

        return new PagedResultDto<AppUser>(items, totalCount, userParams.PageNumber, userParams.PageSize);
    }

    public async Task<PagedResultDto<AppUser>> SearchUsersAsync(int userId, string? q, UserParams userParams, CancellationToken ct = default)
    {
        var query = context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Photos)
            .Include(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .Where(u => u.Id != userId);

        var term = (q ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var lower = term.ToLowerInvariant();
            query = query.Where(u =>
                u.UserName.ToLower().Contains(lower) ||
                (u.KnownAs != null && u.KnownAs.ToLower().Contains(lower)) ||
                (u.Headline != null && u.Headline.ToLower().Contains(lower)));
        }

        var hobbyIds = ParseHobbyIds(userParams.HobbyIds);
        if (hobbyIds.Count > 0)
            query = query.Where(u => u.UserHobbies.Any(uh => hobbyIds.Contains(uh.HobbyId)));

        // Blocked users are hidden in both directions from search results.
        query = query.Where(u => !context.UserBlocks.Any(b =>
            (b.BlockerId == userId && b.BlockedId == u.Id) ||
            (b.BlockerId == u.Id && b.BlockedId == userId)));

        query = query.OrderByDescending(u => u.LastActive).ThenBy(u => u.UserName);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((userParams.PageNumber - 1) * userParams.PageSize)
            .Take(userParams.PageSize)
            .ToListAsync(ct);

        return new PagedResultDto<AppUser>(items, totalCount, userParams.PageNumber, userParams.PageSize);
    }

    public async Task<PagedResultDto<AppUser>> GetSuggestionsAsync(int userId, int page, int pageSize, CancellationToken ct = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Min(50, Math.Max(1, pageSize));

        var viewer = await context.Users
            .Include(u => u.UserHobbies)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (viewer == null)
            return new PagedResultDto<AppUser>([], 0, normalizedPage, normalizedPageSize);

        var followingIds = await context.UserFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync(ct);
        var followingSet = followingIds.ToHashSet();
        var viewerHobbyIds = viewer.UserHobbies.Select(h => h.HobbyId).ToHashSet();

        var candidates = await context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Photos)
            .Include(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .Where(u => u.Id != userId && !followingSet.Contains(u.Id))
            .Where(u => !context.UserBlocks.Any(b =>
                (b.BlockerId == userId && b.BlockedId == u.Id) ||
                (b.BlockerId == u.Id && b.BlockedId == userId)))
            .Where(u => !context.UserMutes.Any(m => m.MuterId == userId && m.MutedId == u.Id))
            .ToListAsync(ct);

        var candidateIds = candidates.Select(c => c.Id).ToList();
        var candidateFollowingRows = await context.UserFollows
            .Where(f => candidateIds.Contains(f.FollowerId))
            .Select(f => new { f.FollowerId, f.FollowingId })
            .ToListAsync(ct);
        var candidateFollowingMap = candidateFollowingRows
            .GroupBy(x => x.FollowerId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.FollowingId).ToHashSet());

        var scored = candidates
            .Select(candidate =>
            {
                var sharedHobbies = candidate.UserHobbies.Count(uh => viewerHobbyIds.Contains(uh.HobbyId));
                candidateFollowingMap.TryGetValue(candidate.Id, out var candidateFollowingSet);
                var mutualConnections = candidateFollowingSet == null
                    ? 0
                    : candidateFollowingSet.Count(id => followingSet.Contains(id));
                var sameCity = !string.IsNullOrWhiteSpace(viewer.City)
                    && !string.IsNullOrWhiteSpace(candidate.City)
                    && string.Equals(viewer.City, candidate.City, StringComparison.OrdinalIgnoreCase);

                var score = sharedHobbies * 3 + mutualConnections * 2 + (sameCity ? 1 : 0);
                return new { Candidate = candidate, Score = score };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Candidate.LastActive)
            .ThenBy(x => x.Candidate.UserName)
            .ToList();

        var totalCount = scored.Count;
        var items = scored
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => x.Candidate)
            .ToList();

        return new PagedResultDto<AppUser>(items, totalCount, normalizedPage, normalizedPageSize);
    }

    private static List<int> ParseHobbyIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var list = new List<int>();
        foreach (var p in parts)
        {
            if (int.TryParse(p, out var id))
                list.Add(id);
        }
        return list.Distinct().ToList();
    }

    public async Task<IReadOnlyList<FollowRelationResult>> GetFollowRelationsAsync(int userId, string list, CancellationToken ct = default)
    {
        return list.ToLowerInvariant() switch
        {
            "following" => await LoadFollowingAsync(userId, ct),
            "followers" => await LoadFollowersAsync(userId, ct),
            _ => []
        };
    }

    private async Task<IReadOnlyList<FollowRelationResult>> LoadFollowingAsync(int userId, CancellationToken ct)
    {
        var rows = await context.UserFollows
            .Where(f => f.FollowerId == userId)
            .Where(f => !context.UserBlocks.Any(b =>
                (b.BlockerId == userId && b.BlockedId == f.FollowingId) ||
                (b.BlockerId == f.FollowingId && b.BlockedId == userId)))
            .Include(f => f.Following!).ThenInclude(u => u.SubscriptionPlan)
            .Include(f => f.Following!).ThenInclude(u => u.Photos)
            .Include(f => f.Following!).ThenInclude(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .ToListAsync(ct);

        return rows
            .Select(f => new FollowRelationResult(f.Following, f.FollowedAtUtc))
            .ToList();
    }

    private async Task<IReadOnlyList<FollowRelationResult>> LoadFollowersAsync(int userId, CancellationToken ct)
    {
        var rows = await context.UserFollows
            .Where(f => f.FollowingId == userId)
            .Where(f => !context.UserBlocks.Any(b =>
                (b.BlockerId == userId && b.BlockedId == f.FollowerId) ||
                (b.BlockerId == f.FollowerId && b.BlockedId == userId)))
            .Include(f => f.Follower!).ThenInclude(u => u.SubscriptionPlan)
            .Include(f => f.Follower!).ThenInclude(u => u.Photos)
            .Include(f => f.Follower!).ThenInclude(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .ToListAsync(ct);

        return rows
            .Select(f => new FollowRelationResult(f.Follower, f.FollowedAtUtc))
            .ToList();
    }

    public async Task<IEnumerable<AppUser>> GetConnectionsAsync(int userId, CancellationToken ct = default)
    {
        var followingIds = await context.UserFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync(ct);

        return await context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Photos)
            .Include(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .Where(u => followingIds.Contains(u.Id) &&
                        context.UserFollows.Any(f => f.FollowerId == u.Id && f.FollowingId == userId) &&
                        !context.UserBlocks.Any(b =>
                            (b.BlockerId == userId && b.BlockedId == u.Id) ||
                            (b.BlockerId == u.Id && b.BlockedId == userId)))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Hobby>> GetAllHobbiesAsync(CancellationToken ct = default) =>
        await context.Hobbies.OrderBy(h => h.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Hobby>> GetHobbiesByIdsAsync(IEnumerable<int> hobbyIds, CancellationToken ct = default)
    {
        var ids = hobbyIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        return await context.Hobbies.Where(h => ids.Contains(h.Id)).ToListAsync(ct);
    }
}
