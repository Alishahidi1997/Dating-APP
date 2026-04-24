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

    [HttpGet("search")]
    public async Task<ActionResult<PagedResultDto<UserDto>>> SearchUsers(
        [FromQuery] string? q,
        [FromQuery] string? hobbyIds,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var userParams = new UserParams
        {
            PageNumber = Math.Max(1, page),
            PageSize = pageSize,
            HobbyIds = hobbyIds
        };

        return Ok(await userService.SearchUsersAsync(UserId, q, userParams, ct));
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<PagedResultDto<UserDto>>> GetSuggestions(
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default) =>
        Ok(await userService.GetSuggestionsAsync(UserId, Math.Max(1, page), pageSize, ct));

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

    [HttpGet("following")]
    public async Task<ActionResult<IEnumerable<FollowListMemberDto>>> GetFollowingList([FromQuery] string list, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(list))
            return BadRequest("Query parameter 'list' is required (following or followers).");

        var normalized = list.ToLowerInvariant();
        if (normalized is not ("following" or "followers"))
            return BadRequest("list must be 'following' or 'followers'.");

        if (normalized is "followers")
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

    [HttpGet("{username}")]
    public async Task<ActionResult<UserDto>> GetUser(string username, CancellationToken ct)
    {
        var user = await userService.GetUserAsync(username, UserId, ct);
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
