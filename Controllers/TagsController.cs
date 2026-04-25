using System.Security.Claims;
using API.Models.Dto;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TagsController(IUserService userService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TagSummaryDto>>> GetTags([FromQuery] int limit = 20, CancellationToken ct = default) =>
        Ok(await userService.GetTagsAsync(limit, ct));

    [HttpGet("{tag}/users")]
    public async Task<ActionResult<PagedResultDto<UserDto>>> GetUsersByTag(
        string tag,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default) =>
        Ok(await userService.GetUsersByTagAsync(UserId, tag, Math.Max(1, page), pageSize, ct));
}
