using API.Data;
using API.Entities;
using API.Models.Dto;

namespace API.Services;

public class AccountService(IUserRepository userRepo, ITokenService tokenService) : IAccountService
{
    public async Task<(UserDto? User, string? Token)?> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (await userRepo.GetUserByUsernameAsync(dto.UserName.ToLowerInvariant(), ct) != null)
            return null;

        if (await userRepo.GetUserByEmailAsync(dto.Email.ToLowerInvariant(), ct) != null)
            return null;

        var age = DateTime.Today.Year - dto.DateOfBirth.Year;
        if (dto.DateOfBirth > DateOnly.FromDateTime(DateTime.Today.AddYears(-age))) age--;
        if (age < 18) return null;

        var user = new AppUser
        {
            UserName = dto.UserName.ToLowerInvariant(),
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Gender = dto.Gender,
            LookingFor = dto.LookingFor,
            Bio = dto.Bio,
            KnownAs = dto.KnownAs ?? dto.UserName,
            DateOfBirth = dto.DateOfBirth,
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
        var userDto = userWithPhotos == null ? MapToUserDto(user) : MapToUserDto(userWithPhotos);
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

        return (MapToUserDto(user), tokenService.CreateToken(user));
    }

    public async Task<bool> DeleteAccountAsync(int userId, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(userId, ct);
        if (user is null) return false;

        userRepo.Delete(user);
        return await userRepo.SaveAllAsync(ct);
    }

    private static UserDto MapToUserDto(AppUser user)
    {
        var photos = user.Photos ?? [];
        return new UserDto
    {
        //var hobbies = user.UserHobbies ?? [];
        Id = user.Id,
        UserName = user.UserName,
        KnownAs = user.KnownAs ?? user.UserName,
        Age = GetAge(user.DateOfBirth),
        Bio = user.Bio,
        Gender = user.Gender,
        LookingFor = user.LookingFor,
        City = user.City,
        Country = user.Country,
        JobTitle = user.JobTitle,
        PhotoUrl = photos.FirstOrDefault(p => p.IsMain)?.Url,
        LastActive = user.LastActive,
        Created = user.Created,
        Photos = photos.Select(p => new PhotoDto { Id = p.Id, Url = p.Url, IsMain = p.IsMain }).ToList(),
        Hobbies = (user.UserHobbies ?? [])
            .Where(uh => uh.Hobby != null)
            .Select(uh => new HobbyDto { Id = uh.HobbyId, Name = uh.Hobby.Name })
            .OrderBy(h => h.Name)
            .ToList()
        };
    }

    private static int GetAge(DateOnly dob)
    {
        var age = DateTime.Today.Year - dob.Year;
        if (dob > DateOnly.FromDateTime(DateTime.Today.AddYears(-age))) age--;
        return age;
    }
}
