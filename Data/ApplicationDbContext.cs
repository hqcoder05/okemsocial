using Microsoft.EntityFrameworkCore;
using okem_social.Models;

namespace okem_social.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Follow> Follows => Set<Follow>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        b.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<int>();

        // Follow: khóa tổng hợp + tự tham chiếu
        b.Entity<Follow>()
            .HasKey(f => new { f.FollowerId, f.FolloweeId });

        b.Entity<Follow>()
            .HasOne(f => f.Follower)
            .WithMany()
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<Follow>()
            .HasOne(f => f.Followee)
            .WithMany()
            .HasForeignKey(f => f.FolloweeId)
            .OnDelete(DeleteBehavior.NoAction);

        // Không tự theo dõi chính mình
        b.Entity<Follow>()
            .HasCheckConstraint("CK_Follow_NoSelf", "FollowerId <> FolloweeId");
    }
}