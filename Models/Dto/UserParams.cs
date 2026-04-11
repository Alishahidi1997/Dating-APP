namespace API.Models.Dto;

public class UserParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(MaxPageSize, value);
    }

    /// <summary>Optional filter; when set, only members with this gender value are returned.</summary>
    public string? Gender { get; set; }

    /// <summary>Optional minimum age (inclusive). When null with <see cref="MaxAge"/> null, only 18+ is enforced.</summary>
    public int? MinAge { get; set; }

    /// <summary>Optional maximum age (inclusive).</summary>
    public int? MaxAge { get; set; }

    /// <summary>Comma-separated hobby ids (e.g. "1,3,5") to prefer people sharing those topics.</summary>
    public string? HobbyIds { get; set; }

    public string OrderBy { get; set; } = "lastActive";
}
