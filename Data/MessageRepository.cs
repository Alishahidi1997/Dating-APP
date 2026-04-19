using API.Entities;
using API.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class MessageRepository(AppDbContext context) : IMessageRepository
{
    private const string InboxContainer = "inbox";
    private const string OutboxContainer = "outbox";

    public void AddMessage(Message message) => context.Messages.Add(message);

    public void DeleteMessage(Message message) => context.Messages.Remove(message);

    public async Task<Message?> GetMessageAsync(int id, CancellationToken ct = default) =>
        await context.Messages
            .Include(m => m.Sender).ThenInclude(s => s!.Photos)
            .Include(m => m.Recipient).ThenInclude(r => r!.Photos)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<PagedResultDto<Message>> GetMessagesForUserAsync(MessageParams messageParams, CancellationToken ct = default)
    {
        var query = BuildContainerQuery(messageParams)
            .Include(m => m.Sender).ThenInclude(s => s!.Photos)
            .Include(m => m.Recipient).ThenInclude(r => r!.Photos)
            .OrderByDescending(m => m.MessageSent);

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
            .Where(m => !context.UserBlocks.Any(b =>
                (b.BlockerId == userId && b.BlockedId == recipientId) ||
                (b.BlockerId == recipientId && b.BlockedId == userId)))
            .OrderBy(m => m.MessageSent)
            .ToListAsync(ct);

    private IQueryable<Message> BuildContainerQuery(MessageParams messageParams)
    {
        var container = messageParams.Container.ToLowerInvariant();
        var userId = messageParams.UserId;

        return container switch
        {
            InboxContainer => context.Messages
                .Where(m => m.RecipientId == userId && !m.RecipientDeleted)
                .Where(m => !context.UserBlocks.Any(b =>
                    (b.BlockerId == userId && b.BlockedId == m.SenderId) ||
                    (b.BlockerId == m.SenderId && b.BlockedId == userId))),

            OutboxContainer => context.Messages
                .Where(m => m.SenderId == userId && !m.SenderDeleted)
                .Where(m => !context.UserBlocks.Any(b =>
                    (b.BlockerId == userId && b.BlockedId == m.RecipientId) ||
                    (b.BlockerId == m.RecipientId && b.BlockedId == userId))),

            _ => context.Messages
                .Where(m => m.RecipientId == userId && !m.RecipientDeleted && m.DateRead == null)
                .Where(m => !context.UserBlocks.Any(b =>
                    (b.BlockerId == userId && b.BlockedId == m.SenderId) ||
                    (b.BlockerId == m.SenderId && b.BlockedId == userId)))
        };
    }
}
