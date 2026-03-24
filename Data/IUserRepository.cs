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
    Task<PagedResultDto<AppUser>> GetUsersForDiscoveryAsync(int userId, UserParams userParams, CancellationToken ct = default);
    Task<IEnumerable<AppUser>> GetLikedUsersAsync(int userId, string predicate, CancellationToken ct = default);
    Task<IEnumerable<AppUser>> GetMatchesAsync(int userId, CancellationToken ct = default);
}
