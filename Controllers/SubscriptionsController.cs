using System.Security.Claims;
using API.Models.Dto;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanDto>>> GetPlans(CancellationToken ct) =>
        Ok(await subscriptionService.GetPlansAsync(ct));

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<SubscriptionSummaryDto>> GetMine(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var summary = await subscriptionService.GetMySummaryAsync(userId, ct);
        return summary == null ? NotFound() : Ok(summary);
    }

    [HttpPost("subscribe")]
    [Authorize]
    public async Task<ActionResult> Subscribe([FromBody] SubscribeDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!await subscriptionService.SubscribeAsync(userId, dto, ct))
            return BadRequest("Invalid plan or could not update subscription");

        return NoContent();
    }
}
