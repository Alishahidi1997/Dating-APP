using API.Entities;

namespace API.Models.Dto;

public class PostReactionSummaryDto
{
    public PostReactionKind Kind { get; set; }
    public int Count { get; set; }
}
