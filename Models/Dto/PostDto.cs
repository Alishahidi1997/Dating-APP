using API.Entities;

namespace API.Models.Dto;

public class PostDto
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string AuthorUserName { get; set; } = string.Empty;
    public string? AuthorKnownAs { get; set; }
    public string? AuthorPhotoUrl { get; set; }
    public string Body { get; set; } = string.Empty;
    public List<string> MediaUrls { get; set; } = [];
    public List<PostReactionSummaryDto> Reactions { get; set; } = [];
    public PostReactionKind? MyReaction { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public PostVisibility Visibility { get; set; }
}
