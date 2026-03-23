using System.Security.Claims;
using API.Models.Dto;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController(IMessageService messageService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<MessageDto>> CreateMessage([FromBody] CreateMessageDto dto, CancellationToken ct)
    {
        var message = await messageService.CreateMessageAsync(UserId, dto, ct);
        if (message == null)
            return BadRequest("Could not send message");

        return Ok(message);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<MessageDto>>> GetMessages(
        [FromQuery] string container = "Unread",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var params_ = new MessageParams
        {
            UserId = UserId,
            Container = container,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        return Ok(await messageService.GetMessagesForUserAsync(params_, ct));
    }

    [HttpGet("thread/{recipientId:int}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetThread(int recipientId, CancellationToken ct) =>
        Ok(await messageService.GetMessageThreadAsync(UserId, recipientId, ct));

    [HttpDelete("{messageId:int}")]
    public async Task<ActionResult> DeleteMessage(int messageId, CancellationToken ct)
    {
        if (!await messageService.DeleteMessageAsync(UserId, messageId, ct))
            return BadRequest("Message not found");

        return NoContent();
    }

    [HttpPut("{messageId:int}/read")]
    public async Task<ActionResult> MarkAsRead(int messageId, CancellationToken ct)
    {
        if (!await messageService.MarkAsReadAsync(UserId, messageId, ct))
            return BadRequest("Message not found");

        return NoContent();
    }
}
