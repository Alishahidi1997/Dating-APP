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
        if (dto.Gender != null) user.Gender = dto.Gender;
        if (dto.LookingFor != null) user.LookingFor = dto.LookingFor;
        if (dto.DateOfBirth.HasValue) user.DateOfBirth = dto.DateOfBirth.Value;
        if (dto.City != null) user.City = dto.City;
        if (dto.Country != null) user.Country = dto.Country;
        user.LastActive = DateTime.UtcNow;

        userRepo.Update(user);
        return await userRepo.SaveAllAsync(ct);
    }

    public async Task<PagedResultDto<UserDto>> GetUsersForDiscoveryAsync(int userId, UserParams userParams, CancellationToken ct = default)
    {
        var result = await userRepo.GetUsersForDiscoveryAsync(userId, userParams, ct);
        var dtos = result.Items.Select(MapToUserDto).ToList();
        return new PagedResultDto<UserDto>(dtos, result.TotalCount, result.PageNumber, result.PageSize);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await userRepo.GetUsersAsync(ct);
        return users.Select(MapToUserDto).ToList();
    }

    public async Task<IEnumerable<UserDto>> GetLikedUsersAsync(int userId, string predicate, CancellationToken ct = default)
    {
        var users = await userRepo.GetLikedUsersAsync(userId, predicate, ct);
        return users.Select(MapToUserDto);
    }

    public async Task<IEnumerable<UserDto>> GetMatchesAsync(int userId, CancellationToken ct = default)
    {
        var users = await userRepo.GetMatchesAsync(userId, ct);
        return users.Select(MapToUserDto);
    }

    internal static UserDto MapToUserDto(AppUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        KnownAs = user.KnownAs ?? user.UserName,
        Age = GetAge(user.DateOfBirth),
        Bio = user.Bio,
        Gender = user.Gender,
        LookingFor = user.LookingFor,
        City = user.City,
        Country = user.Country,
        PhotoUrl = user.Photos?.FirstOrDefault(p => p.IsMain)?.Url,
        LastActive = user.LastActive,
        Created = user.Created,
        Photos = (user.Photos ?? []).Select(p => new PhotoDto { Id = p.Id, Url = p.Url, IsMain = p.IsMain }).ToList()
    };

    private static int GetAge(DateOnly dob)
    {
        var age = DateTime.Today.Year - dob.Year;
        if (dob > DateOnly.FromDateTime(DateTime.Today.AddYears(-age))) age--;
        return age;
    }
}
