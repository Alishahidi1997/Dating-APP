using System.Security.Claims;
using API.Models.Dto;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService, ISubscriptionService subscriptionService, IWebHostEnvironment env) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<UserDto>>> GetFeedDefault(CancellationToken ct) =>
        Ok(await userService.GetFeedAsync(UserId, new UserParams(), ct));

    [HttpGet("feed")]
    public async Task<ActionResult<PagedResultDto<UserDto>>> GetFeed([FromQuery] UserParams userParams, CancellationToken ct) =>
        Ok(await userService.GetFeedAsync(UserId, userParams, ct));

    [HttpGet("discovery")]
    public async Task<ActionResult<PagedResultDto<UserDto>>> GetDiscoveryAlias([FromQuery] UserParams userParams, CancellationToken ct) =>
        Ok(await userService.GetFeedAsync(UserId, userParams, ct));

    [HttpGet("hobbies")]
    public async Task<ActionResult<IReadOnlyList<HobbyDto>>> GetHobbies(CancellationToken ct) =>
        Ok(await userService.GetHobbyOptionsAsync(ct));

    [HttpGet("interests")]
    public async Task<ActionResult<IReadOnlyList<HobbyDto>>> GetInterestsAlias(CancellationToken ct) =>
        Ok(await userService.GetHobbyOptionsAsync(ct));

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers(CancellationToken ct)
    {
        if (!env.IsDevelopment() && !User.IsInRole("Admin"))
            return Forbid();

        return Ok(await userService.GetAllUsersAsync(ct));
    }

    [HttpGet("connections")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetConnections(CancellationToken ct) =>
        Ok(await userService.GetConnectionsAsync(UserId, ct));

    [HttpGet("matches")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetMatchesAlias(CancellationToken ct) =>
        Ok(await userService.GetConnectionsAsync(UserId, ct));

    [HttpGet("following")]
    public async Task<ActionResult<IEnumerable<FollowListMemberDto>>> GetFollowingList([FromQuery] string list, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(list))
            return BadRequest("Query parameter 'list' is required (following, followers, or legacy liked / likedby).");

        var normalized = list.ToLowerInvariant();
        if (normalized is not ("following" or "followers" or "liked" or "likedby"))
            return BadRequest("list must be 'following', 'followers', 'liked', or 'likedby'.");

        if (normalized is "followers" or "likedby")
        {
            var summary = await subscriptionService.GetMySummaryAsync(UserId, ct);
            if (summary is null)
                return NotFound();
            if (!summary.SeeFollowersList)
                return StatusCode(StatusCodes.Status403Forbidden,
                    "Plus or Premium required to see your followers list.");
        }

        return Ok(await userService.GetFollowListAsync(UserId, list, ct));
    }

    [HttpGet("likes")]
    public async Task<ActionResult<IEnumerable<FollowListMemberDto>>> GetLikesLegacy([FromQuery] string predicate, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(predicate) || (predicate != "liked" && predicate != "likedby"))
            return BadRequest("Predicate must be 'liked' or 'likedby' (maps to following / followers).");

        if (string.Equals(predicate, "likedby", StringComparison.OrdinalIgnoreCase))
        {
            var summary = await subscriptionService.GetMySummaryAsync(UserId, ct);
            if (summary is null)
                return NotFound();
            if (!summary.SeeFollowersList)
                return StatusCode(StatusCodes.Status403Forbidden,
                    "Plus or Premium required to see your followers list.");
        }

        return Ok(await userService.GetFollowListAsync(UserId, predicate, ct));
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<UserDto>> GetUser(string username, CancellationToken ct)
    {
        var user = await userService.GetUserAsync(username, ct);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPut]
    public async Task<ActionResult> UpdateMember([FromBody] MemberUpdateDto dto, CancellationToken ct)
    {
        if (!await userService.UpdateMemberAsync(UserId, dto, ct))
            return BadRequest("Failed to update profile");

        return NoContent();
    }
}
