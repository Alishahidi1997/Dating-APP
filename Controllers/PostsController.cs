using System.Security.Claims;
using API.Models.Dto;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController(IPostService postService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto dto, CancellationToken ct)
    {
        var created = await postService.CreateAsync(UserId, dto, ct);
        if (created == null)
            return BadRequest("Could not create post.");

        return Ok(created);
    }

    [HttpPut("{postId:int}")]
    public async Task<ActionResult<PostDto>> UpdatePost(int postId, [FromBody] UpdatePostDto dto, CancellationToken ct)
    {
        var updated = await postService.UpdateAsync(UserId, postId, dto, ct);
        if (updated == null)
            return BadRequest("Could not update post.");

        return Ok(updated);
    }

    [HttpDelete("{postId:int}")]
    public async Task<ActionResult> DeletePost(int postId, CancellationToken ct)
    {
        if (!await postService.DeleteAsync(UserId, postId, ct))
            return BadRequest("Could not delete post.");

        return NoContent();
    }

    [HttpPost("{postId:int}/reactions")]
    public async Task<ActionResult> ReactToPost(int postId, [FromBody] ReactToPostDto dto, CancellationToken ct)
    {
        if (!await postService.AddOrUpdateReactionAsync(UserId, postId, dto.Kind, ct))
            return BadRequest("Could not react to post.");

        return NoContent();
    }

    [HttpDelete("{postId:int}/reactions")]
    public async Task<ActionResult> RemoveReaction(int postId, CancellationToken ct)
    {
        if (!await postService.RemoveReactionAsync(UserId, postId, ct))
            return BadRequest("Could not remove reaction.");

        return NoContent();
    }
}
