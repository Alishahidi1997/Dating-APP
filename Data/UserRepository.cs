using API.Entities;
using API.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public void Add(AppUser user) => context.Users.Add(user);
    public void Update(AppUser user) => context.Entry(user).State = EntityState.Modified;

    public async Task<bool> SaveAllAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct) > 0;

    public async Task<IEnumerable<AppUser>> GetUsersAsync(CancellationToken ct = default) =>
        await context.Users
            .Include(u => u.Photos)
            .ToListAsync(ct);

    public async Task<AppUser?> GetUserByIdAsync(int id, CancellationToken ct = default) =>
        await context.Users
            .Include(u => u.Photos)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<AppUser?> GetUserByUsernameAsync(string username, CancellationToken ct = default) =>
        await context.Users
            .FirstOrDefaultAsync(u => u.UserName == username, ct);

    public async Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<AppUser?> GetUserByUsernameWithPhotosAsync(string username, CancellationToken ct = default) =>
        await context.Users
            .Include(u => u.Photos)
            .FirstOrDefaultAsync(u => u.UserName == username, ct);

    public async Task<PagedResultDto<AppUser>> GetUsersForDiscoveryAsync(int userId, UserParams userParams, CancellationToken ct = default)
    {
        var minDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-userParams.MaxAge));
        var maxDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-userParams.MinAge));

        var query = context.Users
            .Include(u => u.Photos)
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
            "created" => query.OrderByDescending(u => u.Created),
            _ => query.OrderByDescending(u => u.LastActive)
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((userParams.PageNumber - 1) * userParams.PageSize)
            .Take(userParams.PageSize)
            .ToListAsync(ct);

        return new PagedResultDto<AppUser>(items, totalCount, userParams.PageNumber, userParams.PageSize);
    }

    public async Task<IEnumerable<AppUser>> GetLikedUsersAsync(int userId, string predicate, CancellationToken ct = default)
    {
        var users = context.Users
            .Include(u => u.Photos)
            .AsQueryable();

        var userLikes = context.UserLikes.AsQueryable();

        return predicate.ToLowerInvariant() switch
        {
            "liked" => await userLikes
                .Where(ul => ul.SourceUserId == userId)
                .Join(users, ul => ul.TargetUserId, u => u.Id, (_, u) => u)
                .ToListAsync(ct),
            "likedby" => await userLikes
                .Where(ul => ul.TargetUserId == userId)
                .Join(users, ul => ul.SourceUserId, u => u.Id, (_, u) => u)
                .ToListAsync(ct),
            _ => []
        };
    }

    public async Task<IEnumerable<AppUser>> GetMatchesAsync(int userId, CancellationToken ct = default)
    {
        var liked = await context.UserLikes
            .Where(ul => ul.SourceUserId == userId)
            .Select(ul => ul.TargetUserId)
            .ToListAsync(ct);

        return await context.Users
            .Include(u => u.Photos)
            .Where(u => liked.Contains(u.Id) && context.UserLikes.Any(ul => ul.SourceUserId == u.Id && ul.TargetUserId == userId))
            .ToListAsync(ct);
    }
}
