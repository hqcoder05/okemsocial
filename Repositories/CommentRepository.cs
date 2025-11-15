using Microsoft.EntityFrameworkCore;
using okem_social.Data;
using okem_social.Models;

namespace okem_social.Repositories;

public class CommentRepository(ApplicationDbContext db) : ICommentRepository
{
    public async Task<List<Comment>> GetByPostIdAsync(int postId)
    {
        return await db.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Comment?> GetByIdAsync(int id)
    {
        return await db.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Comment> CreateAsync(Comment comment)
    {
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        
        // Reload with user
        await db.Entry(comment).Reference(c => c.User).LoadAsync();
        return comment;
    }

    public async Task DeleteAsync(int id)
    {
        var comment = await db.Comments.FindAsync(id);
        if (comment != null)
        {
            db.Comments.Remove(comment);
            await db.SaveChangesAsync();
        }
    }

    public Task<int> GetLikesCountAsync(int commentId) =>
        db.Likes.CountAsync(l => l.CommentId == commentId);

    public Task<bool> IsLikedByUserAsync(int commentId, int userId) =>
        db.Likes.AnyAsync(l => l.CommentId == commentId && l.UserId == userId);
}
