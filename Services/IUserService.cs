using API.Entities;
using API.Models.Dto;

namespace API.Services;

public interface IUserService
{
    Task<UserDto?> GetUserAsync(string username, CancellationToken ct = default);
    Task<UserDto?> GetUserByIdAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateMemberAsync(int userId, MemberUpdateDto dto, CancellationToken ct = default);
    Task<PagedResultDto<UserDto>> GetUsersForDiscoveryAsync(int userId, UserParams userParams, CancellationToken ct = default);
    Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<IEnumerable<UserDto>> GetLikedUsersAsync(int userId, string predicate, CancellationToken ct = default);
    Task<IEnumerable<UserDto>> GetMatchesAsync(int userId, CancellationToken ct = default);
}
