using Microsoft.EntityFrameworkCore;
using okem_social.Data;
using okem_social.Models;

namespace okem_social.Repositories;

public class LikeRepository(ApplicationDbContext db) : ILikeRepository
{
    public Task<Like?> GetPostLikeAsync(int postId, int userId) =>
        db.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

    public Task<Like?> GetCommentLikeAsync(int commentId, int userId) =>
        db.Likes.FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);

    public async Task<List<User>> GetPostLikesAsync(int postId)
    {
        return await db.Likes
            .Where(l => l.PostId == postId)
            .Include(l => l.User)
            .Select(l => l.User!)
            .ToListAsync();
    }

    public async Task AddPostLikeAsync(int postId, int userId)
    {
        var existing = await GetPostLikeAsync(postId, userId);
        if (existing == null)
        {
            db.Likes.Add(new Like { PostId = postId, UserId = userId });
            await db.SaveChangesAsync();
        }
    }

    public async Task AddCommentLikeAsync(int commentId, int userId)
    {
        var existing = await GetCommentLikeAsync(commentId, userId);
        if (existing == null)
        {
            db.Likes.Add(new Like { CommentId = commentId, UserId = userId });
            await db.SaveChangesAsync();
        }
    }

    public async Task RemoveAsync(Like like)
    {
        db.Likes.Remove(like);
        await db.SaveChangesAsync();
    }
}
