using API.Entities;

namespace API.Data;

public sealed record FollowRelationResult(AppUser Member, DateTime FollowedAtUtc);
