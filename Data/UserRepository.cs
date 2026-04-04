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

    public async Task<PagedResultDto<AppUser>> GetUsersForDiscoveryAsync(int userId, UserParams userParams, CancellationToken ct = default)
    {
        var minDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-userParams.MaxAge));
        var maxDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-userParams.MinAge));

        var query = context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Photos)
            .Include(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .Where(u => u.Id != userId)
            .Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);

        if (!string.IsNullOrEmpty(userParams.Gender))
            query = query.Where(u => u.Gender == userParams.Gender);

        var likes = await context.UserLikes
            .Where(ul => ul.SourceUserId == userId)
            .Select(ul => ul.TargetUserId)
            .ToListAsync(ct);
        query = query.Where(u => !likes.Contains(u.Id));

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

    public async Task<IReadOnlyList<LikedUserResult>> GetLikedUsersAsync(int userId, string predicate, CancellationToken ct = default)
    {
        return predicate.ToLowerInvariant() switch
        {
            "liked" => await LoadUsersILikedAsync(userId, ct),
            "likedby" => await LoadUsersWhoLikedMeAsync(userId, ct),
            _ => []
        };
    }

    private async Task<IReadOnlyList<LikedUserResult>> LoadUsersILikedAsync(int userId, CancellationToken ct)
    {
        var likes = await context.UserLikes
            .Where(ul => ul.SourceUserId == userId)
            .Include(ul => ul.TargetPhoto)
            .Include(ul => ul.TargetUser!).ThenInclude(u => u.SubscriptionPlan)
            .Include(ul => ul.TargetUser!).ThenInclude(u => u.Photos)
            .Include(ul => ul.TargetUser!).ThenInclude(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .ToListAsync(ct);

        return likes.Select(ul => new LikedUserResult(ul.TargetUser, ul.TargetPhoto)).ToList();
    }

    private async Task<IReadOnlyList<LikedUserResult>> LoadUsersWhoLikedMeAsync(int userId, CancellationToken ct)
    {
        var likes = await context.UserLikes
            .Where(ul => ul.TargetUserId == userId)
            .Include(ul => ul.TargetPhoto)
            .Include(ul => ul.SourceUser!).ThenInclude(u => u.SubscriptionPlan)
            .Include(ul => ul.SourceUser!).ThenInclude(u => u.Photos)
            .Include(ul => ul.SourceUser!).ThenInclude(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .ToListAsync(ct);

        return likes.Select(ul => new LikedUserResult(ul.SourceUser, ul.TargetPhoto)).ToList();
    }

    public async Task<IEnumerable<AppUser>> GetMatchesAsync(int userId, CancellationToken ct = default)
    {
        var liked = await context.UserLikes
            .Where(ul => ul.SourceUserId == userId)
            .Select(ul => ul.TargetUserId)
            .ToListAsync(ct);

        return await context.Users
            .Include(u => u.SubscriptionPlan)
            .Include(u => u.Photos)
            .Include(u => u.UserHobbies).ThenInclude(uh => uh.Hobby)
            .Where(u => liked.Contains(u.Id) && context.UserLikes.Any(ul => ul.SourceUserId == u.Id && ul.TargetUserId == userId))
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
