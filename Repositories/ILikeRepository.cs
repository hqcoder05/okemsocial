using okem_social.Models;

namespace okem_social.Repositories;

public interface ILikeRepository
{
    Task<Like?> GetPostLikeAsync(int postId, int userId);
    Task<Like?> GetCommentLikeAsync(int commentId, int userId);
    Task<List<User>> GetPostLikesAsync(int postId);
    Task AddPostLikeAsync(int postId, int userId);
    Task AddCommentLikeAsync(int commentId, int userId);
    Task RemoveAsync(Like like);
}
