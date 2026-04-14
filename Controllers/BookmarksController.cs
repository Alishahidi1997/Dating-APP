using System.Security.Claims;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookmarksController(IBookmarkService bookmarkService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{userId:int}")]
    public async Task<ActionResult> Save(int userId, CancellationToken ct)
    {
        if (!await bookmarkService.SaveAsync(UserId, userId, ct))
            return BadRequest("Cannot bookmark yourself or user not found");
        return Ok();
    }

    [HttpDelete("{userId:int}")]
    public async Task<ActionResult> Remove(int userId, CancellationToken ct)
    {
        if (!await bookmarkService.RemoveAsync(UserId, userId, ct))
            return BadRequest("Bookmark not found");
        return NoContent();
    }
}
