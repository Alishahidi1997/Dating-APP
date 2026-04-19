using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users { get; set; } = null!;
    public DbSet<Photo> Photos { get; set; } = null!;
    public DbSet<UserFollow> UserFollows { get; set; } = null!;
    public DbSet<UserBookmark> UserBookmarks { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<Hobby> Hobbies { get; set; } = null!;
    public DbSet<UserHobby> UserHobbies { get; set; } = null!;
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
    public DbSet<UserBlock> UserBlocks { get; set; } = null!;
    public DbSet<UserMute> UserMutes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureSubscriptions(builder);
        ConfigureUserFollows(builder);
        ConfigureUserBookmarks(builder);
        ConfigureMessages(builder);
        ConfigureUserHobbies(builder);
        ConfigureUserBlocks(builder);
        ConfigureUserMutes(builder);
        ConfigureUserIndexes(builder);

        builder.Entity<Hobby>().HasData(
            new Hobby { Id = 1, Name = "Travel" },
            new Hobby { Id = 2, Name = "Cooking" },
            new Hobby { Id = 3, Name = "Reading" },
            new Hobby { Id = 4, Name = "Gaming" },
            new Hobby { Id = 5, Name = "Music" },
            new Hobby { Id = 6, Name = "Sports" },
            new Hobby { Id = 7, Name = "Fitness" },
            new Hobby { Id = 8, Name = "Photography" },
            new Hobby { Id = 9, Name = "Movies" },
            new Hobby { Id = 10, Name = "Hiking" }
        );
    }

    private static void ConfigureSubscriptions(ModelBuilder builder)
    {
        builder.Entity<AppUser>()
            .HasOne(u => u.SubscriptionPlan)
            .WithMany(p => p.Subscribers)
            .HasForeignKey(u => u.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SubscriptionPlan>().HasData(
            new SubscriptionPlan
            {
                Id = 1,
                Name = "Free",
                Description = "Limited follows per day; follower list locked.",
                MonthlyPriceUsd = 0,
                UnlimitedFollows = false,
                SeeFollowersList = false,
                PriorityInFeed = false
            },
            new SubscriptionPlan
            {
                Id = 2,
                Name = "Plus",
                Description = "Unlimited follows and see your followers.",
                MonthlyPriceUsd = 9.99m,
                UnlimitedFollows = true,
                SeeFollowersList = true,
                PriorityInFeed = false
            },
            new SubscriptionPlan
            {
                Id = 3,
                Name = "Premium",
                Description = "Same as Plus with stronger feed placement.",
                MonthlyPriceUsd = 19.99m,
                UnlimitedFollows = true,
                SeeFollowersList = true,
                PriorityInFeed = true
            });
    }

    private static void ConfigureUserFollows(ModelBuilder builder)
    {
        builder.Entity<UserFollow>()
            .HasKey(f => new { f.FollowerId, f.FollowingId });

        builder.Entity<UserFollow>()
            .HasOne(f => f.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserFollow>()
            .HasOne(f => f.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUserBookmarks(ModelBuilder builder)
    {
        builder.Entity<UserBookmark>()
            .HasKey(b => new { b.UserId, b.BookmarkedUserId });

        builder.Entity<UserBookmark>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bookmarks)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserBookmark>()
            .HasOne(b => b.BookmarkedUser)
            .WithMany()
            .HasForeignKey(b => b.BookmarkedUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureMessages(ModelBuilder builder)
    {
        builder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.MessagesSent)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Message>()
            .HasOne(m => m.Recipient)
            .WithMany(u => u.MessagesReceived)
            .HasForeignKey(m => m.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUserBlocks(ModelBuilder builder)
    {
        builder.Entity<UserBlock>()
            .HasKey(b => new { b.BlockerId, b.BlockedId });

        builder.Entity<UserBlock>()
            .HasOne(b => b.Blocker)
            .WithMany()
            .HasForeignKey(b => b.BlockerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserBlock>()
            .HasOne(b => b.Blocked)
            .WithMany()
            .HasForeignKey(b => b.BlockedId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUserMutes(ModelBuilder builder)
    {
        builder.Entity<UserMute>()
            .HasKey(m => new { m.MuterId, m.MutedId });

        builder.Entity<UserMute>()
            .HasOne(m => m.Muter)
            .WithMany()
            .HasForeignKey(m => m.MuterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserMute>()
            .HasOne(m => m.Muted)
            .WithMany()
            .HasForeignKey(m => m.MutedId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUserHobbies(ModelBuilder builder)
    {
        builder.Entity<UserHobby>()
            .HasKey(uh => new { uh.AppUserId, uh.HobbyId });

        builder.Entity<UserHobby>()
            .HasOne(uh => uh.AppUser)
            .WithMany(u => u.UserHobbies)
            .HasForeignKey(uh => uh.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserHobby>()
            .HasOne(uh => uh.Hobby)
            .WithMany(h => h.UserHobbies)
            .HasForeignKey(uh => uh.HobbyId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUserIndexes(ModelBuilder builder)
    {
        builder.Entity<AppUser>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        builder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}
