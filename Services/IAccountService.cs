using API.Models.Dto;

namespace API.Services;

public interface IAccountService
{
    Task<(UserDto? User, string? Token)?> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<(UserDto? User, string? Token)?> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<bool> DeleteAccountAsync(int userId, CancellationToken ct = default);
}
