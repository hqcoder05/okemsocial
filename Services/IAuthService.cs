using okem_social.Models;

namespace okem_social.Services;

public interface IAuthService
{
    Task<User?> ValidateUserAsync(string email, string password);
    Task<User> GetOrCreateExternalUserAsync(string email, string fullName, string? avatarUrl);
}