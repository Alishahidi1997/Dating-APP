using API.Entities;
using API.Models.Dto;

namespace API.Data;

public interface IUserRepository
{
    void Add(AppUser user);
    void Delete(AppUser user);
    void Update(AppUser user);
    Task<bool> SaveAllAsync(CancellationToken ct = default);
    Task<IEnumerable<AppUser>> GetUsersAsync(CancellationToken ct = default);
    Task<AppUser?> GetUserByIdAsync(int id, CancellationToken ct = default);
    Task<AppUser?> GetUserByUsernameAsync(string username, CancellationToken ct = default);
    Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task<AppUser?> GetUserByUsernameWithPhotosAsync(string username, CancellationToken ct = default);
    Task<PagedResultDto<AppUser>> GetUsersForFeedAsync(int userId, UserParams userParams, CancellationToken ct = default);
    Task<PagedResultDto<AppUser>> SearchUsersAsync(int userId, string? q, UserParams userParams, CancellationToken ct = default);
    Task<PagedResultDto<AppUser>> GetSuggestionsAsync(int userId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<TagSummaryDto>> GetHobbyTagsAsync(int limit, CancellationToken ct = default);
    Task<PagedResultDto<AppUser>> GetUsersByTagAsync(int viewerUserId, string tag, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<FollowRelationResult>> GetFollowRelationsAsync(int userId, string list, CancellationToken ct = default);
    Task<IEnumerable<AppUser>> GetConnectionsAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<Hobby>> GetAllHobbiesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Hobby>> GetHobbiesByIdsAsync(IEnumerable<int> hobbyIds, CancellationToken ct = default);
}
