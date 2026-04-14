using API.Data;
using API.Entities;
using API.Models.Dto;

namespace API.Services;

public class UserService(IUserRepository userRepo) : IUserService
{
    public async Task<UserDto?> GetUserAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        var user = await userRepo.GetUserByUsernameWithPhotosAsync(username.ToLowerInvariant(), ct);
        return user == null ? null : MapToUserDto(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(id, ct);
        return user == null ? null : MapToUserDto(user);
    }

    public async Task<bool> UpdateMemberAsync(int userId, MemberUpdateDto dto, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(userId, ct);
        if (user == null) return false;

        if (dto.KnownAs != null) user.KnownAs = dto.KnownAs;
        if (dto.Bio != null) user.Bio = dto.Bio;
        if (dto.Headline != null) user.Headline = dto.Headline;
        if (dto.ProfileLinks != null) user.ProfileLinks = dto.ProfileLinks;
        if (dto.City != null) user.City = dto.City;
        if (dto.Country != null) user.Country = dto.Country;
        if (dto.JobTitle != null) user.JobTitle = dto.JobTitle;
        if (dto.HobbyIds != null)
        {
            var hobbies = await userRepo.GetHobbiesByIdsAsync(dto.HobbyIds, ct);
            user.UserHobbies = hobbies
                .Select(h => new UserHobby { AppUserId = user.Id, HobbyId = h.Id })
                .ToList();
        }
        user.LastActive = DateTime.UtcNow;

        userRepo.Update(user);
        return await userRepo.SaveAllAsync(ct);
    }

    public async Task<PagedResultDto<UserDto>> GetFeedAsync(int userId, UserParams userParams, CancellationToken ct = default)
    {
        var result = await userRepo.GetUsersForFeedAsync(userId, userParams, ct);
        var dtos = result.Items.Select(MapToUserDto).ToList();
        return new PagedResultDto<UserDto>(dtos, result.TotalCount, result.PageNumber, result.PageSize);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await userRepo.GetUsersAsync(ct);
        return users.Select(MapToUserDto).ToList();
    }

    public async Task<IReadOnlyList<HobbyDto>> GetHobbyOptionsAsync(CancellationToken ct = default)
    {
        var hobbies = await userRepo.GetAllHobbiesAsync(ct);
        return hobbies.Select(h => new HobbyDto { Id = h.Id, Name = h.Name }).ToList();
    }

    public async Task<IEnumerable<FollowListMemberDto>> GetFollowListAsync(int userId, string list, CancellationToken ct = default)
    {
        var rows = await userRepo.GetFollowRelationsAsync(userId, list, ct);
        return rows.Select(r => new FollowListMemberDto
        {
            Member = MapToUserDto(r.Member),
            FollowedAtUtc = r.FollowedAtUtc
        });
    }

    public async Task<IEnumerable<UserDto>> GetConnectionsAsync(int userId, CancellationToken ct = default)
    {
        var users = await userRepo.GetConnectionsAsync(userId, ct);
        return users.Select(MapToUserDto);
    }

    internal static UserDto MapToUserDto(AppUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        KnownAs = user.KnownAs ?? user.UserName,
        Bio = user.Bio,
        Headline = user.Headline,
        ProfileLinks = user.ProfileLinks,
        IsVerified = user.IsVerified,
        City = user.City,
        Country = user.Country,
        JobTitle = user.JobTitle,
        PhotoUrl = user.Photos?.FirstOrDefault(p => p.IsMain)?.Url,
        LastActive = user.LastActive,
        Created = user.Created,
        Photos = (user.Photos ?? []).Select(p => new PhotoDto { Id = p.Id, Url = p.Url, IsMain = p.IsMain }).ToList(),
        Hobbies = (user.UserHobbies ?? [])
            .Where(uh => uh.Hobby != null)
            .Select(uh => new HobbyDto { Id = uh.HobbyId, Name = uh.Hobby.Name })
            .OrderBy(h => h.Name)
            .ToList(),
        Subscription = SubscriptionEntitlements.ToSummary(user)
    };
}
