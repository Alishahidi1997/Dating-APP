using System.Security.Claims;
using API.Models.Dto;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(IAccountService accountService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("AuthEndpoints")]
    public async Task<ActionResult<object>> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await accountService.RegisterAsync(dto, ct);
        if (result == null)
            return BadRequest("Username or email already exists");

        return Ok(new { result.Value.User, Token = result.Value.Token });
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthEndpoints")]
    public async Task<ActionResult<object>> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await accountService.LoginAsync(dto, ct);
        if (result == null)
            return Unauthorized("Invalid username or password");

        return Ok(new { result.Value.User, Token = result.Value.Token });
    }

    [AllowAnonymous]
    [HttpPost("confirm-email")]
    public async Task<ActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto, CancellationToken ct)
    {
        return await accountService.ConfirmEmailAsync(dto.Token, ct) switch
        {
            ConfirmEmailResult.Success => NoContent(),
            ConfirmEmailResult.AlreadyConfirmed => NoContent(),
            ConfirmEmailResult.InvalidOrExpiredToken => BadRequest("Invalid or expired confirmation token."),
            ConfirmEmailResult.EmailMismatch => BadRequest("Invalid or expired confirmation token."),
            _ => BadRequest()
        };
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        await accountService.ForgotPasswordAsync(dto.Email, ct);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        return await accountService.ResetPasswordAsync(dto.Token, dto.NewPassword, ct) switch
        {
            ResetPasswordResult.Success => NoContent(),
            ResetPasswordResult.InvalidOrExpiredToken => BadRequest("Invalid or expired reset token."),
            ResetPasswordResult.EmailMismatch => BadRequest("Invalid or expired reset token."),
            _ => BadRequest()
        };
    }

    [Authorize]
    [HttpPost("resend-confirmation")]
    public async Task<ActionResult> ResendConfirmation(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        if (!await accountService.ResendConfirmationEmailAsync(userId, ct))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Could not send email. Try again later.");

        return NoContent();
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
