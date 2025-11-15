using Microsoft.EntityFrameworkCore;
using okem_social.Data;
using okem_social.Models;

namespace okem_social.Repositories;

public class PostRepository : IPostRepository
{
    private readonly ApplicationDbContext _db;

    public PostRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Post?> GetByIdAsync(int id, bool includeDetails = false)
    {
        IQueryable<Post> query = _db.Posts;

        if (includeDetails)
        {
            query = query
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.Likes);
        }
        else
        {
            query = query
                .Include(p => p.User);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Post>> GetFeedAsync(int userId, int skip = 0, int take = 20)
    {
        // Tạm thời: feed tất cả bài, có thể lọc theo bạn bè sau
        return await _db.Posts
            .Include(p => p.User)
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<Post>> GetUserPostsAsync(int userId, int skip = 0, int take = 20)
    {
        return await _db.Posts
            .Where(p => p.UserId == userId)
            .Include(p => p.User)
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Post> CreateAsync(Post post)
    {
        post.CreatedAt = DateTime.UtcNow;
        _db.Posts.Add(post);
        await _db.SaveChangesAsync();
        return post;
    }

    public async Task UpdateAsync(Post post)
    {
        _db.Posts.Update(post);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post != null)
        {
            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();
        }
    }

    public Task<int> GetLikesCountAsync(int postId) =>
        _db.Likes.CountAsync(l => l.PostId == postId);

    public Task<int> GetCommentsCountAsync(int postId) =>
        _db.Comments.CountAsync(c => c.PostId == postId);

    public Task<bool> IsLikedByUserAsync(int postId, int userId) =>
        _db.Likes.AnyAsync(l => l.PostId == postId && l.UserId == userId);
}
