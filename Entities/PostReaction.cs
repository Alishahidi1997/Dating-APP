namespace API.Entities;

public enum PostReactionKind
{
    Like = 0,
    Love = 1,
    Laugh = 2,
    Fire = 3
}

public class PostReaction
{
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public PostReactionKind Kind { get; set; } = PostReactionKind.Like;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
