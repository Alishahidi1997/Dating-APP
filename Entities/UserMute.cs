namespace API.Entities;

public class UserMute
{
    public int MuterId { get; set; }
    public AppUser Muter { get; set; } = null!;

    public int MutedId { get; set; }
    public AppUser Muted { get; set; } = null!;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
