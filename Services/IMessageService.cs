using API.Models.Dto;

namespace API.Services;

public interface IMessageService
{
    Task<MessageDto?> CreateMessageAsync(int senderId, CreateMessageDto dto, CancellationToken ct = default);
    Task<PagedResultDto<MessageDto>> GetMessagesForUserAsync(MessageParams messageParams, CancellationToken ct = default);
    Task<IEnumerable<MessageDto>> GetMessageThreadAsync(int userId, int recipientId, CancellationToken ct = default);
    Task<bool> DeleteMessageAsync(int userId, int messageId, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(int userId, int messageId, CancellationToken ct = default);
}
