namespace API.Models.Dto;

public class MessageParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(MaxPageSize, value);
    }
    public int UserId { get; set; }
    public string Container { get; set; } = "Unread"; // Unread, Inbox, Outbox
}
