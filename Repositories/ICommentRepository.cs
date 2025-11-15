using okem_social.Models;

namespace okem_social.Repositories;

public interface ICommentRepository
{
    Task<List<Comment>> GetByPostIdAsync(int postId);
    Task<Comment?> GetByIdAsync(int id);
    Task<Comment> CreateAsync(Comment comment);
    Task DeleteAsync(int id);
    Task<int> GetLikesCountAsync(int commentId);
    Task<bool> IsLikedByUserAsync(int commentId, int userId);
}
