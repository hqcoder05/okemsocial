using okem_social.Repositories;
using okem_social.Models;
using okem_social.Data;

namespace okem_social.Services;

public class AuthService(IUserRepository repo, ApplicationDbContext db) : IAuthService
{
    public async Task<User?> ValidateUserAsync(string email, string password)
    {
        var user = await repo.GetByEmailAsync(email);
        if (user is null) return null;

        // So khớp mật khẩu (hash bởi BCrypt)
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public async Task<User> GetOrCreateExternalUserAsync(string email, string fullName, string? avatarUrl)
    {
        var user = await repo.GetByEmailAsync(email);
        if (user != null)
        {
            // Update avatar if provided and not already set
            if (!string.IsNullOrEmpty(avatarUrl) && string.IsNullOrEmpty(user.AvatarUrl))
            {
                user.AvatarUrl = avatarUrl;
                await repo.UpdateAsync(user);
            }
            return user;
        }

        // Tạo tài khoản mới tự động
        user = new User
        {
            Email = email,
            FullName = fullName,
            AvatarUrl = avatarUrl,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
            Role = Role.User,
            CreatedAt = DateTime.UtcNow,
            EmailNotificationsEnabled = true,
            PushNotificationsEnabled = true,
            ContentVisibleToPublic = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}