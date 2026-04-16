using API.Data;
using API.Entities;
using API.Models.Dto;

namespace API.Services;

public class MessageService(
    IMessageRepository messageRepo,
    IUserRepository userRepo,
    IUserModerationRepository moderationRepo) : IMessageService
{
    public async Task<MessageDto?> CreateMessageAsync(int senderId, CreateMessageDto dto, CancellationToken ct = default)
    {
        if (senderId == dto.RecipientId) return null;

        var recipient = await userRepo.GetUserByIdAsync(dto.RecipientId, ct);
        if (recipient == null) return null;

        var sender = await userRepo.GetUserByIdAsync(senderId, ct);
        if (sender == null) return null;

        if (await moderationRepo.IsBlockedEitherWayAsync(senderId, dto.RecipientId, ct))
            return null;

        var message = new Message
        {
            SenderId = senderId,
            RecipientId = dto.RecipientId,
            Content = dto.Content
        };

        messageRepo.AddMessage(message);

        if (!await userRepo.SaveAllAsync(ct))
            return null;

        var created = await messageRepo.GetMessageAsync(message.Id, ct);
        return created == null ? null : MapToMessageDto(created);
    }

    public async Task<PagedResultDto<MessageDto>> GetMessagesForUserAsync(MessageParams messageParams, CancellationToken ct = default)
    {
        var result = await messageRepo.GetMessagesForUserAsync(messageParams, ct);
        var dtos = result.Items.Select(MapToMessageDto).ToList();
        return new PagedResultDto<MessageDto>(dtos, result.TotalCount, result.PageNumber, result.PageSize);
    }

    public async Task<IEnumerable<MessageDto>> GetMessageThreadAsync(int userId, int recipientId, CancellationToken ct = default)
    {
        if (await moderationRepo.IsBlockedEitherWayAsync(userId, recipientId, ct))
            return [];

        var messages = await messageRepo.GetMessageThreadAsync(userId, recipientId, ct);
        return messages.Select(MapToMessageDto);
    }

    public async Task<bool> DeleteMessageAsync(int userId, int messageId, CancellationToken ct = default)
    {
        var message = await messageRepo.GetMessageAsync(messageId, ct);
        if (message == null) return false;
        if (message.SenderId != userId && message.RecipientId != userId) return false;

        if (message.SenderId == userId) message.SenderDeleted = true;
        if (message.RecipientId == userId) message.RecipientDeleted = true;

        if (message.SenderDeleted && message.RecipientDeleted)
            messageRepo.DeleteMessage(message);

        return await userRepo.SaveAllAsync(ct);
    }

    public async Task<bool> MarkAsReadAsync(int userId, int messageId, CancellationToken ct = default)
    {
        var message = await messageRepo.GetMessageAsync(messageId, ct);
        if (message == null || message.RecipientId != userId) return false;

        message.DateRead = DateTime.UtcNow;
        return await userRepo.SaveAllAsync(ct);
    }

    private static MessageDto MapToMessageDto(Message m) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        SenderUsername = m.Sender.UserName,
        SenderPhotoUrl = m.Sender.Photos?.FirstOrDefault(p => p.IsMain)?.Url,
        RecipientId = m.RecipientId,
        RecipientUsername = m.Recipient.UserName,
        RecipientPhotoUrl = m.Recipient.Photos?.FirstOrDefault(p => p.IsMain)?.Url,
        Content = m.Content,
        MessageSent = m.MessageSent,
        DateRead = m.DateRead
    };
}
