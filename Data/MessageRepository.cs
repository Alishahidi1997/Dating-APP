using API.Entities;
using API.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class MessageRepository(AppDbContext context) : IMessageRepository
{
    public void AddMessage(Message message) => context.Messages.Add(message);

    public void DeleteMessage(Message message) => context.Messages.Remove(message);

    public async Task<Message?> GetMessageAsync(int id, CancellationToken ct = default) =>
        await context.Messages
            .Include(m => m.Sender).ThenInclude(s => s!.Photos)
            .Include(m => m.Recipient).ThenInclude(r => r!.Photos)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<PagedResultDto<Message>> GetMessagesForUserAsync(MessageParams messageParams, CancellationToken ct = default)
    {
        var filtered = messageParams.Container.ToLowerInvariant() switch
        {
            "inbox" => context.Messages.FromSqlInterpolated($@"
                SELECT * FROM Messages
                WHERE RecipientId = {messageParams.UserId}
                  AND RecipientDeleted = 0"),
            "outbox" => context.Messages.FromSqlInterpolated($@"
                SELECT * FROM Messages
                WHERE SenderId = {messageParams.UserId}
                  AND SenderDeleted = 0"),
            _ => context.Messages.FromSqlInterpolated($@"
                SELECT * FROM Messages
                WHERE RecipientId = {messageParams.UserId}
                  AND RecipientDeleted = 0
                  AND DateRead IS NULL")
        };

        var query = filtered
            .Include(m => m.Sender).ThenInclude(s => s!.Photos)
            .Include(m => m.Recipient).ThenInclude(r => r!.Photos);

        query = query.OrderByDescending(m => m.MessageSent);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((messageParams.PageNumber - 1) * messageParams.PageSize)
            .Take(messageParams.PageSize)
            .ToListAsync(ct);

        return new PagedResultDto<Message>(items, totalCount, messageParams.PageNumber, messageParams.PageSize);
    }

    public async Task<IEnumerable<Message>> GetMessageThreadAsync(int userId, int recipientId, CancellationToken ct = default) =>
        await context.Messages
            .Include(m => m.Sender).ThenInclude(s => s!.Photos)
            .Include(m => m.Recipient).ThenInclude(r => r!.Photos)
            .Where(m =>
                (m.SenderId == userId && m.RecipientId == recipientId && !m.SenderDeleted) ||
                (m.SenderId == recipientId && m.RecipientId == userId && !m.RecipientDeleted))
            .OrderBy(m => m.MessageSent)
            .ToListAsync(ct);
}
