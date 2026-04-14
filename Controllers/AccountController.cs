using System.Security.Claims;
using API.Models.Dto;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(IAccountService accountService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<object>> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await accountService.RegisterAsync(dto, ct);
        if (result == null)
            return BadRequest("Username or email already exists");

        return Ok(new { result.Value.User, Token = result.Value.Token });
    }

    [HttpPost("login")]
    public async Task<ActionResult<object>> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await accountService.LoginAsync(dto, ct);
        if (result == null)
            return Unauthorized("Invalid username or password");

        return Ok(new { result.Value.User, Token = result.Value.Token });
    }

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult> DeleteAccount(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        if (!await accountService.DeleteAccountAsync(userId, ct))
            return NotFound("User account not found");

        return NoContent();
    }
}
