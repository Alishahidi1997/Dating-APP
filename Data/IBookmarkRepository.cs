using API.Entities;

namespace API.Data;

public interface IBookmarkRepository
{
    Task<UserBookmark?> GetBookmarkAsync(int userId, int bookmarkedUserId, CancellationToken ct = default);
    void AddBookmark(UserBookmark bookmark);
    void RemoveBookmark(UserBookmark bookmark);
}
