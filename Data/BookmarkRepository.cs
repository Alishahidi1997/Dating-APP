using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class BookmarkRepository(AppDbContext context) : IBookmarkRepository
{
    public async Task<UserBookmark?> GetBookmarkAsync(int userId, int bookmarkedUserId, CancellationToken ct = default) =>
        await context.UserBookmarks.FindAsync([userId, bookmarkedUserId], ct);

    public void AddBookmark(UserBookmark bookmark) => context.UserBookmarks.Add(bookmark);

    public void RemoveBookmark(UserBookmark bookmark) => context.UserBookmarks.Remove(bookmark);
}
