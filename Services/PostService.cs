using API.Data;
using API.Entities;
using API.Models.Dto;

namespace API.Services;

public class PostService(IPostRepository postRepo, IUserRepository userRepo) : IPostService
{
    public async Task<PostDto?> CreateAsync(int authorId, CreatePostDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Body))
            return null;

        var author = await userRepo.GetUserByIdAsync(authorId, ct);
        if (author == null)
            return null;

        var post = new Post
        {
            AuthorId = authorId,
            Body = dto.Body.Trim(),
            Visibility = dto.Visibility
        };

        var requestedPhotoIds = (dto.PhotoIds ?? [])
            .Distinct()
            .ToList();
        if (requestedPhotoIds.Count > 4)
            return null;

        var ownedPhotoIds = await postRepo.GetOwnedPhotoIdsAsync(authorId, requestedPhotoIds, ct);
        if (ownedPhotoIds.Count != requestedPhotoIds.Count)
            return null;

        post.PostPhotos = requestedPhotoIds
            .Select((photoId, index) => new PostPhoto
            {
                PhotoId = photoId,
                SortOrder = index
            })
            .ToList();

        postRepo.Add(post);
        if (!await userRepo.SaveAllAsync(ct))
            return null;

        var created = await postRepo.GetByIdAsync(post.Id, ct);
        return created == null ? null : MapToDto(created);
    }

    public async Task<PostDto?> UpdateAsync(int currentUserId, int postId, UpdatePostDto dto, CancellationToken ct = default)
    {
        var post = await postRepo.GetByIdAsync(postId, ct);
        if (post == null || post.AuthorId != currentUserId || string.IsNullOrWhiteSpace(dto.Body))
            return null;

        post.Body = dto.Body.Trim();
        post.Visibility = dto.Visibility;
        post.UpdatedUtc = DateTime.UtcNow;

        var requestedPhotoIds = (dto.PhotoIds ?? [])
            .Distinct()
            .ToList();
        if (requestedPhotoIds.Count > 4)
            return null;

        var ownedPhotoIds = await postRepo.GetOwnedPhotoIdsAsync(currentUserId, requestedPhotoIds, ct);
        if (ownedPhotoIds.Count != requestedPhotoIds.Count)
            return null;

        post.PostPhotos.Clear();
        post.PostPhotos = requestedPhotoIds
            .Select((photoId, index) => new PostPhoto
            {
                PostId = post.Id,
                PhotoId = photoId,
                SortOrder = index
            })
            .ToList();

        if (!await userRepo.SaveAllAsync(ct))
            return null;

        var updated = await postRepo.GetByIdAsync(post.Id, ct);
        return updated == null ? null : MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int currentUserId, int postId, CancellationToken ct = default)
    {
        var post = await postRepo.GetByIdAsync(postId, ct);
        if (post == null || post.AuthorId != currentUserId)
            return false;

        post.DeletedUtc = DateTime.UtcNow;
        return await userRepo.SaveAllAsync(ct);
    }

    public async Task<PagedResultDto<PostDto>> GetHomeTimelineAsync(int viewerUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await postRepo.GetHomeTimelineAsync(viewerUserId, page, pageSize, ct);
        var items = result.Items.Select(MapToDto).ToList();
        return new PagedResultDto<PostDto>(items, result.TotalCount, result.PageNumber, result.PageSize);
    }

    public async Task<PagedResultDto<PostDto>> GetUserTimelineAsync(int viewerUserId, string username, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await postRepo.GetUserTimelineAsync(viewerUserId, username, page, pageSize, ct);
        var items = result.Items.Select(MapToDto).ToList();
        return new PagedResultDto<PostDto>(items, result.TotalCount, result.PageNumber, result.PageSize);
    }

    private static PostDto MapToDto(Post post) => new()
    {
        Id = post.Id,
        AuthorId = post.AuthorId,
        AuthorUserName = post.Author.UserName,
        AuthorKnownAs = post.Author.KnownAs,
        AuthorPhotoUrl = post.Author.Photos.FirstOrDefault(p => p.IsMain)?.Url,
        Body = post.Body,
        MediaUrls = post.PostPhotos
            .OrderBy(pp => pp.SortOrder)
            .Select(pp => pp.Photo.Url)
            .ToList(),
        CreatedUtc = post.CreatedUtc,
        UpdatedUtc = post.UpdatedUtc,
        Visibility = post.Visibility
    };
}
