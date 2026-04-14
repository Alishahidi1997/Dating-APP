using System.ComponentModel.DataAnnotations;

namespace API.Entities;

public class AppUser
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? Bio { get; set; }
    public string? KnownAs { get; set; }
    [MaxLength(200)]
    public string? Headline { get; set; }
    public string? ProfileLinks { get; set; }
    public bool IsVerified { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime LastActive { get; set; } = DateTime.UtcNow;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? JobTitle { get; set; }
    public bool IsAdmin { get; set; }

    public int SubscriptionPlanId { get; set; } = 1;
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
    public DateTime? SubscriptionEndsUtc { get; set; }
    public bool SubscriptionAutoRenew { get; set; } = true;
    public int SubscriptionRenewalDays { get; set; } = 30;
    public int FeedBoostCached { get; set; }

    public ICollection<Photo> Photos { get; set; } = [];
    public ICollection<UserFollow> Following { get; set; } = [];
    public ICollection<UserFollow> Followers { get; set; } = [];
    public ICollection<UserBookmark> Bookmarks { get; set; } = [];
    public ICollection<Message> MessagesSent { get; set; } = [];
    public ICollection<Message> MessagesReceived { get; set; } = [];
    public ICollection<UserHobby> UserHobbies { get; set; } = [];
}
