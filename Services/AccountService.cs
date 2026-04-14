using API.Data;
using API.Entities;
using API.Models.Dto;

namespace API.Services;

public class AccountService(IUserRepository userRepo, ITokenService tokenService, ISubscriptionService subscriptionService) : IAccountService
{
    public async Task<(UserDto? User, string? Token)?> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (await userRepo.GetUserByUsernameAsync(dto.UserName.ToLowerInvariant(), ct) != null)
            return null;

        if (await userRepo.GetUserByEmailAsync(dto.Email.ToLowerInvariant(), ct) != null)
            return null;

        var user = new AppUser
        {
            UserName = dto.UserName.ToLowerInvariant(),
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Bio = dto.Bio,
            KnownAs = dto.KnownAs ?? dto.UserName,
            City = dto.City,
            Country = dto.Country,
            JobTitle = dto.JobTitle
        };

        if (dto.HobbyIds.Count > 0)
        {
            var hobbies = await userRepo.GetHobbiesByIdsAsync(dto.HobbyIds, ct);
            user.UserHobbies = hobbies
                .Select(h => new UserHobby { HobbyId = h.Id })
                .ToList();
        }

        userRepo.Add(user);
        if (!await userRepo.SaveAllAsync(ct))
            return null;

        var userWithPhotos = await userRepo.GetUserByUsernameWithPhotosAsync(user.UserName, ct);
        var userDto = userWithPhotos == null ? UserService.MapToUserDto(user) : UserService.MapToUserDto(userWithPhotos);
        return (userDto, tokenService.CreateToken(user));
    }

    public async Task<(UserDto? User, string? Token)?> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByUsernameWithPhotosAsync(dto.UserName.ToLowerInvariant(), ct);
        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;

        user.LastActive = DateTime.UtcNow;
        userRepo.Update(user);
        await userRepo.SaveAllAsync(ct);

        await subscriptionService.ReconcileUserAsync(user.Id, ct);

        return (UserService.MapToUserDto(user), tokenService.CreateToken(user));
    }

    public async Task<bool> DeleteAccountAsync(int userId, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(userId, ct);
        if (user is null) return false;

        userRepo.Delete(user);
        return await userRepo.SaveAllAsync(ct);
    }
}
