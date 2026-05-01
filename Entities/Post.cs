using System.ComponentModel.DataAnnotations;

namespace API.Entities;

public enum PostVisibility
{
    Public = 0,
    Followers = 1
}

public class Post
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public AppUser Author { get; set; } = null!;

    [MaxLength(2000)]
    public required string Body { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public DateTime? DeletedUtc { get; set; }
    public PostVisibility Visibility { get; set; } = PostVisibility.Public;
    public ICollection<PostPhoto> PostPhotos { get; set; } = [];
    public ICollection<PostReaction> Reactions { get; set; } = [];
}
