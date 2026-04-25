using API.Entities;
using API.Models.Dto;

namespace API.Services;

public interface IUserService
{
    Task<UserDto?> GetUserAsync(string username, int viewerUserId, CancellationToken ct = default);
    Task<UserDto?> GetUserByIdAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateMemberAsync(int userId, MemberUpdateDto dto, CancellationToken ct = default);
    Task<PagedResultDto<UserDto>> GetFeedAsync(int userId, UserParams userParams, CancellationToken ct = default);
    Task<PagedResultDto<UserDto>> SearchUsersAsync(int userId, string? q, UserParams userParams, CancellationToken ct = default);
    Task<PagedResultDto<UserDto>> GetSuggestionsAsync(int userId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<TagSummaryDto>> GetTagsAsync(int limit, CancellationToken ct = default);
    Task<PagedResultDto<UserDto>> GetUsersByTagAsync(int viewerUserId, string tag, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HobbyDto>> GetHobbyOptionsAsync(CancellationToken ct = default);
    Task<IEnumerable<FollowListMemberDto>> GetFollowListAsync(int userId, string list, CancellationToken ct = default);
    Task<IEnumerable<UserDto>> GetConnectionsAsync(int userId, CancellationToken ct = default);
}
