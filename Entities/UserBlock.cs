namespace API.Entities;

public class UserBlock
{
    public int BlockerId { get; set; }
    public AppUser Blocker { get; set; } = null!;

    public int BlockedId { get; set; }
    public AppUser Blocked { get; set; } = null!;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
