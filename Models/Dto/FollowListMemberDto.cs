namespace API.Models.Dto;

public record FollowListMemberDto
{
    public required UserDto Member { get; init; }
    public DateTime FollowedAtUtc { get; init; }
}
