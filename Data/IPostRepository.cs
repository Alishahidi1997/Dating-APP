using API.Entities;
using API.Models.Dto;

namespace API.Data;

public interface IPostRepository
{
    void Add(Post post);
    void Remove(Post post);
    Task<Post?> GetByIdAsync(int postId, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetOwnedPhotoIdsAsync(int userId, IReadOnlyCollection<int> photoIds, CancellationToken ct = default);
    Task<PagedResultDto<Post>> GetHomeTimelineAsync(int viewerUserId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResultDto<Post>> GetUserTimelineAsync(int viewerUserId, string username, int page, int pageSize, CancellationToken ct = default);
}
