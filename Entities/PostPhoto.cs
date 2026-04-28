namespace API.Entities;

public class PostPhoto
{
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int PhotoId { get; set; }
    public Photo Photo { get; set; } = null!;

    public int SortOrder { get; set; }
}
