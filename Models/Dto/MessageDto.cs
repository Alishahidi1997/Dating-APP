namespace API.Models.Dto;

public record MessageDto
{
    public int Id { get; init; }
    public int SenderId { get; init; }
    public required string SenderUsername { get; init; }
    public string? SenderPhotoUrl { get; init; }
    public int RecipientId { get; init; }
    public required string RecipientUsername { get; init; }
    public string? RecipientPhotoUrl { get; init; }
    public required string Content { get; init; }
    public DateTime MessageSent { get; init; }
    public DateTime? DateRead { get; init; }
}
