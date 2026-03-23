using System.Security.Cryptography;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Http;

namespace API.Services;

public class PhotoService(IUserRepository userRepo, IWebHostEnvironment env) : IPhotoService
{
    private const string ImagesFolder = "images";
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public async Task<Photo?> AddPhotoAsync(int userId, IFormFile file, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(userId, ct);
        if (user == null) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return null;

        if (file.Length > MaxFileSizeBytes)
            return null;

        var publicId = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
        var fileName = $"{publicId}{ext}";
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var folderPath = Path.Combine(webRoot, ImagesFolder);
        Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream, ct);

        var url = $"/{ImagesFolder}/{fileName}";
        var isFirstPhoto = user.Photos.Count == 0;

        var photo = new Photo
        {
            Url = url,
            PublicId = publicId,
            IsMain = isFirstPhoto,
            AppUserId = userId
        };

        user.Photos.Add(photo);
        userRepo.Update(user);

        if (!await userRepo.SaveAllAsync(ct))
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            return null;
        }

        return photo;
    }

    public async Task<bool> DeletePhotoAsync(int userId, int photoId, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(userId, ct);
        if (user == null) return false;

        var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);
        if (photo == null) return false;
        if (photo.IsMain) return false;

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var filePath = Path.Combine(webRoot, ImagesFolder, Path.GetFileName(photo.Url));
        user.Photos.Remove(photo);
        userRepo.Update(user);

        if (!await userRepo.SaveAllAsync(ct))
            return false;

        if (File.Exists(filePath)) File.Delete(filePath);
        return true;
    }

    public async Task<bool> SetMainPhotoAsync(int userId, int photoId, CancellationToken ct = default)
    {
        var user = await userRepo.GetUserByIdAsync(userId, ct);
        if (user == null) return false;

        var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);
        if (photo == null) return false;

        foreach (var p in user.Photos)
            p.IsMain = p.Id == photoId;

        userRepo.Update(user);
        return await userRepo.SaveAllAsync(ct);
    }
}
