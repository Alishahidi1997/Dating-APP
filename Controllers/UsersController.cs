using System.Security.Claims;
using API.Models.Dto;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(CancellationToken ct) =>
        Ok(await userService.GetUsersForDiscoveryAsync(UserId, new UserParams(), ct));

    [HttpGet("discovery")]
    public async Task<ActionResult<PagedResultDto<UserDto>>> GetDiscovery([FromQuery] UserParams userParams, CancellationToken ct) =>
        Ok(await userService.GetUsersForDiscoveryAsync(UserId, userParams, ct));

    [HttpGet("matches")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetMatches(CancellationToken ct) =>
        Ok(await userService.GetMatchesAsync(UserId, ct));

    [HttpGet("likes")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetLikes([FromQuery] string predicate, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(predicate) || (predicate != "liked" && predicate != "likedby"))
            return BadRequest("Predicate must be 'liked' or 'likedby'");

        return Ok(await userService.GetLikedUsersAsync(UserId, predicate, ct));
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
