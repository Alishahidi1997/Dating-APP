using API.Entities;
using Microsoft.AspNetCore.Http;

namespace API.Services;

public interface IPhotoService
{
    Task<Photo?> AddPhotoAsync(int userId, IFormFile file, CancellationToken ct = default);
    Task<bool> DeletePhotoAsync(int userId, int photoId, CancellationToken ct = default);
    Task<bool> SetMainPhotoAsync(int userId, int photoId, CancellationToken ct = default);
}
