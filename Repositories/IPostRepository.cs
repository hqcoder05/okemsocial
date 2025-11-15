using okem_social.Models;

namespace okem_social.Repositories;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(int id, bool includeDetails = false);
    Task<List<Post>> GetFeedAsync(int userId, int skip = 0, int take = 20);
    Task<List<Post>> GetUserPostsAsync(int userId, int skip = 0, int take = 20);
    Task<Post> CreateAsync(Post post);
    Task UpdateAsync(Post post);
    Task DeleteAsync(int id);
    Task<int> GetLikesCountAsync(int postId);
    Task<int> GetCommentsCountAsync(int postId);
    Task<bool> IsLikedByUserAsync(int postId, int userId);
}
