using API.Entities;
using API.Models.Dto;

namespace API.Data;

public interface IPostRepository
{
    void Add(Post post);
    void Remove(Post post);
    Task<Post?> GetByIdAsync(int postId, CancellationToken ct = default);
    Task<Post?> GetVisiblePostForViewerAsync(int postId, int viewerUserId, CancellationToken ct = default);
    Task<PostReaction?> GetReactionAsync(int postId, int userId, CancellationToken ct = default);
    void AddReaction(PostReaction reaction);
    void RemoveReaction(PostReaction reaction);
    Task<IReadOnlyList<int>> GetOwnedPhotoIdsAsync(int userId, IReadOnlyCollection<int> photoIds, CancellationToken ct = default);
    Task<PagedResultDto<Post>> GetHomeTimelineAsync(int viewerUserId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResultDto<Post>> GetUserTimelineAsync(int viewerUserId, string username, int page, int pageSize, CancellationToken ct = default);
}
