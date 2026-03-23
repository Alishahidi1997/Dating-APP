using System.ComponentModel.DataAnnotations;

namespace API.Models.Dto;

public record CreateMessageDto
{
    [Required, MinLength(1), MaxLength(2000)]
    public required string Content { get; init; }

    [Required]
    public int RecipientId { get; init; }
}
