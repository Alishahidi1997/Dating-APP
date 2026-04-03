using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser>? Users { get; set; }
    public DbSet<Photo>? Photos { get; set; }
    public DbSet<UserLike> UserLikes { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<Hobby> Hobbies { get; set; } = null!;
    public DbSet<UserHobby> UserHobbies { get; set; } = null!;
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureSubscriptions(builder);
        ConfigureUserLikes(builder);
        ConfigureMessages(builder);
        ConfigureUserHobbies(builder);
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
                Description = "Daily like cap; who-liked-you is locked.",
                MonthlyPriceUsd = 0,
                UnlimitedLikes = false,
                SeeWhoLikedYou = false,
                PriorityInDiscovery = false
            },
            new SubscriptionPlan
            {
                Id = 2,
                Name = "Plus",
                Description = "Unlimited likes and see who liked you.",
                MonthlyPriceUsd = 9.99m,
                UnlimitedLikes = true,
                SeeWhoLikedYou = true,
                PriorityInDiscovery = false
            },
            new SubscriptionPlan
            {
                Id = 3,
                Name = "Premium",
                Description = "Same as Plus with higher placement in discovery.",
                MonthlyPriceUsd = 19.99m,
                UnlimitedLikes = true,
                SeeWhoLikedYou = true,
                PriorityInDiscovery = true
            });
    }

    private static void ConfigureUserLikes(ModelBuilder builder)
    {
        builder.Entity<UserLike>()
            .HasKey(ul => new { ul.SourceUserId, ul.TargetUserId });

        builder.Entity<UserLike>()
            .HasOne(ul => ul.SourceUser)
            .WithMany(u => u.LikedUsers)
            .HasForeignKey(ul => ul.SourceUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserLike>()
            .HasOne(ul => ul.TargetUser)
            .WithMany(u => u.LikedByUsers)
            .HasForeignKey(ul => ul.TargetUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserLike>()
            .HasOne(ul => ul.TargetPhoto)
            .WithMany()
            .HasForeignKey(ul => ul.TargetPhotoId)
            .OnDelete(DeleteBehavior.SetNull);
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
