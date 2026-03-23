namespace API.Services;

public interface ILikesService
{
    Task<bool> AddLikeAsync(int sourceUserId, int targetUserId, CancellationToken ct = default);
}
