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

        var oldestAllowedDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-18));
        query = query.Where(u => u.DateOfBirth <= oldestAllowedDob);

        if (userParams.MinAge.HasValue || userParams.MaxAge.HasValue)
        {
            var minAge = userParams.MinAge ?? 18;
            var maxAge = userParams.MaxAge ?? 120;
            var minDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-maxAge));
            var maxDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-minAge));
            query = query.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
        }

        if (!string.IsNullOrWhiteSpace(userParams.Gender))
            query = query.Where(u => u.Gender == userParams.Gender);

        var hobbyIds = ParseHobbyIds(userParams.HobbyIds);
        if (hobbyIds.Count > 0)
            query = query.Where(u => u.UserHobbies.Any(uh => hobbyIds.Contains(uh.HobbyId)));

        var followingIds = await context.UserFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync(ct);
        query = query.Where(u => !followingIds.Contains(u.Id));

        query = userParams.OrderBy.ToLowerInvariant() switch
        {
            "created" => query.OrderByDescending(u => u.DiscoveryBoostCached).ThenByDescending(u => u.Created),
            _ => query.OrderByDescending(u => u.DiscoveryBoostCached).ThenByDescending(u => u.LastActive)
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((userParams.PageNumber - 1) * userParams.PageSize)
            .Take(userParams.PageSize)
            .ToListAsync(ct);

        return new PagedResultDto<AppUser>(items, totalCount, userParams.PageNumber, userParams.PageSize);
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
            "following" or "liked" => await LoadFollowingAsync(userId, ct),
            "followers" or "likedby" => await LoadFollowersAsync(userId, ct),
            _ => []
        };
    }

    private async Task<IReadOnlyList<FollowRelationResult>> LoadFollowingAsync(int userId, CancellationToken ct)
    {
        var rows = await context.UserFollows
            .Where(f => f.FollowerId == userId)
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
                        context.UserFollows.Any(f => f.FollowerId == u.Id && f.FollowingId == userId))
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
