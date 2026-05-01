using API.Entities;

namespace API.Models.Dto;

public class ReactToPostDto
{
    public PostReactionKind Kind { get; set; } = PostReactionKind.Like;
}
