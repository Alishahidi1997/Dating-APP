using API.Entities;
using API.Models.Dto;

namespace API.Data;

public interface IMessageRepository
{
    void AddMessage(Message message);
    void DeleteMessage(Message message);
    Task<Message?> GetMessageAsync(int id, CancellationToken ct = default);
    Task<PagedResultDto<Message>> GetMessagesForUserAsync(MessageParams messageParams, CancellationToken ct = default);
    Task<IEnumerable<Message>> GetMessageThreadAsync(int userId, int recipientId, CancellationToken ct = default);
}
