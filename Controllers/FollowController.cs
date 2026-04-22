using System.Security.Claims;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FollowController(IFollowService followService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{userId:int}")]
    [EnableRateLimiting("FollowEndpoint")]
    public async Task<ActionResult> Follow(int userId, CancellationToken ct) =>
        await followService.FollowAsync(UserId, userId, ct) switch
        {
            FollowAddResult.Success or FollowAddResult.AlreadyFollowing => Ok(),
            FollowAddResult.InvalidTarget => BadRequest("Cannot follow yourself or user not found"),
            FollowAddResult.BlockedUser => BadRequest("Cannot follow a user you have blocked or who has blocked you."),
            FollowAddResult.DailyLimitReached => StatusCode(StatusCodes.Status429TooManyRequests,
                "You can start at most 20 follows per day (UTC) on the free plan."),
            FollowAddResult.Failed => BadRequest("Could not save follow"),
            _ => BadRequest()
        };

    [HttpDelete("{userId:int}")]
    public async Task<ActionResult> Unfollow(int userId, CancellationToken ct)
    {
        if (!await followService.UnfollowAsync(UserId, userId, ct))
            return BadRequest("Not following this user or invalid request");
        return NoContent();
    }
}
