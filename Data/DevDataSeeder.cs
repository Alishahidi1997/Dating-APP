using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public static class DevDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await EnsureHobbiesAsync(db, ct);
        await EnsureUsersAsync(db, ct);
        await EnsureSubscriptionSamplesAsync(db, ct);
        await EnsurePhotosAsync(db, ct);
        await EnsureUserHobbiesAsync(db, ct);
        await EnsureFollowsAsync(db, ct);
        await EnsureBookmarksAsync(db, ct);
        await EnsureMessagesAsync(db, ct);
    }

    private static async Task EnsureHobbiesAsync(AppDbContext db, CancellationToken ct)
    {
        var count = await db.Hobbies.CountAsync(ct);
        if (count >= 20) return;

        var names = new[]
        {
            "Travel","Cooking","Reading","Gaming","Music","Sports","Fitness","Photography","Movies","Hiking",
            "Dancing","Art","Yoga","Cycling","Running","Swimming","Pets","Board Games","Tech","Coffee"
        };

        var existing = await db.Hobbies.Select(h => h.Name).ToListAsync(ct);
        var toAdd = names
            .Where(n => !existing.Contains(n, StringComparer.OrdinalIgnoreCase))
            .Take(20 - count)
            .Select(n => new Hobby { Name = n })
            .ToList();

        if (toAdd.Count == 0) return;
        db.Hobbies.AddRange(toAdd);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureUsersAsync(AppDbContext db, CancellationToken ct)
    {
        var usersSet = db.Users;
        var count = await usersSet.CountAsync(ct);
        if (count >= 20) return;

        var toCreate = 20 - count;
        for (var i = 1; i <= toCreate; i++)
        {
            var suffix = $"{DateTime.UtcNow:yyyyMMddHHmmss}{i:D2}";
            var username = $"demo{suffix}".ToLowerInvariant();
            usersSet.Add(new AppUser
            {
                UserName = username,
                Email = $"{username}@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123A"),
                KnownAs = $"Demo {i}",
                Bio = "Seeded demo profile",
                JobTitle = i % 3 == 0 ? "Software Engineer" : i % 3 == 1 ? "Teacher" : "Designer",
                City = "Seed City",
                Country = "Seed Country",
                IsAdmin = false
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureSubscriptionSamplesAsync(AppDbContext db, CancellationToken ct)
    {
        if (!await db.SubscriptionPlans.AnyAsync(ct))
            return;

        if (await db.Users.AnyAsync(u => u.SubscriptionPlanId > 1, ct))
            return;

        var list = await db.Users.OrderBy(u => u.Id).Take(15).ToListAsync(ct);
        if (list.Count == 0)
            return;

        for (var i = 0; i < list.Count; i++)
        {
            if (i < 5)
            {
                list[i].SubscriptionPlanId = 3;
                list[i].SubscriptionEndsUtc = DateTime.UtcNow.AddYears(1);
                list[i].FeedBoostCached = 1;
            }
            else if (i < 10)
            {
                list[i].SubscriptionPlanId = 2;
                list[i].SubscriptionEndsUtc = DateTime.UtcNow.AddMonths(6);
                list[i].FeedBoostCached = 0;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsurePhotosAsync(AppDbContext db, CancellationToken ct)
    {
        var count = await db.Photos.CountAsync(ct);
        if (count >= 20) return;

        var users = await db.Users
            .Include(u => u.Photos)
            .OrderBy(u => u.Id)
            .ToListAsync(ct);

        var needed = 20 - count;
        foreach (var user in users)
        {
            if (needed == 0) break;
            var hasMain = user.Photos.Any(p => p.IsMain);
            user.Photos.Add(new Photo
            {
                Url = "/images/placeholder-profile.png",
                PublicId = Guid.NewGuid().ToString("N")[..16],
                IsMain = !hasMain
            });
            needed--;
        }

        if (needed > 0)
        {
            var firstUser = users.FirstOrDefault();
            if (firstUser != null)
            {
                for (var i = 0; i < needed; i++)
                {
                    firstUser.Photos.Add(new Photo
                    {
                        Url = "/images/placeholder-profile.png",
                        PublicId = Guid.NewGuid().ToString("N")[..16],
                        IsMain = false
                    });
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureUserHobbiesAsync(AppDbContext db, CancellationToken ct)
    {
        var count = await db.UserHobbies.CountAsync(ct);
        if (count >= 20) return;

        var users = await db.Users.OrderBy(u => u.Id).ToListAsync(ct);
        var hobbies = await db.Hobbies.OrderBy(h => h.Id).ToListAsync(ct);
        if (users.Count == 0 || hobbies.Count == 0) return;

        var existing = await db.UserHobbies
            .Select(uh => new { uh.AppUserId, uh.HobbyId })
            .ToListAsync(ct);

        var existingSet = existing
            .Select(x => $"{x.AppUserId}:{x.HobbyId}")
            .ToHashSet();

        var needed = 20 - count;
        foreach (var user in users)
        {
            if (needed == 0) break;
            foreach (var hobby in hobbies)
            {
                if (needed == 0) break;
                var key = $"{user.Id}:{hobby.Id}";
                if (existingSet.Contains(key)) continue;
                db.UserHobbies.Add(new UserHobby { AppUserId = user.Id, HobbyId = hobby.Id });
                existingSet.Add(key);
                needed--;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureFollowsAsync(AppDbContext db, CancellationToken ct)
    {
        var count = await db.UserFollows.CountAsync(ct);
        if (count >= 20) return;

        var users = await db.Users.OrderBy(u => u.Id).Select(u => u.Id).ToListAsync(ct);
        if (users.Count < 2) return;

        var existing = await db.UserFollows
            .Select(f => $"{f.FollowerId}:{f.FollowingId}")
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        var needed = 20 - count;
        for (var i = 0; i < users.Count && needed > 0; i++)
        {
            var follower = users[i];
            var following = users[(i + 1) % users.Count];
            var key = $"{follower}:{following}";
            if (follower == following || existingSet.Contains(key)) continue;

            db.UserFollows.Add(new UserFollow
            {
                FollowerId = follower,
                FollowingId = following,
                FollowedAtUtc = DateTime.UtcNow.AddDays(-30)
            });
            existingSet.Add(key);
            needed--;
        }

        for (var i = 0; i < users.Count && needed > 0; i++)
        {
            var follower = users[i];
            var following = users[(i + 2) % users.Count];
            var key = $"{follower}:{following}";
            if (follower == following || existingSet.Contains(key)) continue;

            db.UserFollows.Add(new UserFollow
            {
                FollowerId = follower,
                FollowingId = following,
                FollowedAtUtc = DateTime.UtcNow.AddDays(-30)
            });
            existingSet.Add(key);
            needed--;
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureBookmarksAsync(AppDbContext db, CancellationToken ct)
    {
        var count = await db.UserBookmarks.CountAsync(ct);
        if (count >= 10) return;

        var users = await db.Users.OrderBy(u => u.Id).Select(u => u.Id).ToListAsync(ct);
        if (users.Count < 2) return;

        var existing = await db.UserBookmarks
            .Select(b => $"{b.UserId}:{b.BookmarkedUserId}")
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        var needed = 10 - count;
        for (var i = 0; i < users.Count && needed > 0; i++)
        {
            var userId = users[i];
            var bookmarked = users[(i + 3) % users.Count];
            var key = $"{userId}:{bookmarked}";
            if (userId == bookmarked || existingSet.Contains(key)) continue;

            db.UserBookmarks.Add(new UserBookmark
            {
                UserId = userId,
                BookmarkedUserId = bookmarked,
                SavedAtUtc = DateTime.UtcNow.AddDays(-7)
            });
            existingSet.Add(key);
            needed--;
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureMessagesAsync(AppDbContext db, CancellationToken ct)
    {
        var count = await db.Messages.CountAsync(ct);
        if (count >= 20) return;

        var users = await db.Users.OrderBy(u => u.Id).Select(u => u.Id).ToListAsync(ct);
        if (users.Count < 2) return;

        var needed = 20 - count;
        for (var i = 0; i < needed; i++)
        {
            var sender = users[i % users.Count];
            var recipient = users[(i + 1) % users.Count];
            if (sender == recipient) continue;

            db.Messages.Add(new Message
            {
                SenderId = sender,
                RecipientId = recipient,
                Content = $"Seeded message #{i + 1}",
                MessageSent = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
