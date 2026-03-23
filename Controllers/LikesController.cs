using System.Security.Claims;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LikesController(ILikesService likesService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{targetUserId:int}")]
    public async Task<ActionResult> LikeUser(int targetUserId, CancellationToken ct)
    {
        if (!await likesService.AddLikeAsync(UserId, targetUserId, ct))
            return BadRequest("Cannot like yourself or user not found");

        return Ok();
    }
}
