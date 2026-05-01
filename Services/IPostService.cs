using API.Models.Dto;
using API.Entities;

namespace API.Services;

public interface IPostService
{
    Task<PostDto?> CreateAsync(int authorId, CreatePostDto dto, CancellationToken ct = default);
    Task<PostDto?> UpdateAsync(int currentUserId, int postId, UpdatePostDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(int currentUserId, int postId, CancellationToken ct = default);
    Task<bool> AddOrUpdateReactionAsync(int currentUserId, int postId, PostReactionKind kind, CancellationToken ct = default);
    Task<bool> RemoveReactionAsync(int currentUserId, int postId, CancellationToken ct = default);
    Task<PagedResultDto<PostDto>> GetHomeTimelineAsync(int viewerUserId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResultDto<PostDto>> GetUserTimelineAsync(int viewerUserId, string username, int page, int pageSize, CancellationToken ct = default);
}
