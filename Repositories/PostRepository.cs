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
        var raw = await _db.Posts
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(p => new
            {
                Post = p,
                User = p.User,
                LikesCount = p.Likes.Count(),
                CommentsCount = p.Comments.Count(),
                IsLiked = p.Likes.Any(l => l.UserId == userId)
            })
            .ToListAsync();

        return raw.Select(r => {
            r.Post.User = r.User;
            r.Post.LikesCount = r.LikesCount;
            r.Post.CommentsCount = r.CommentsCount;
            r.Post.IsLikedByCurrentUser = r.IsLiked;
            r.Post.Comments = null!;
            r.Post.Likes = null!;
            return r.Post;
        }).ToList();
    }

    public async Task<List<Post>> GetUserPostsAsync(int viewerId, int skip = 0, int take = 20)
    {
        var raw = await _db.Posts
            .Where(p => p.UserId == viewerId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(p => new
            {
                Post = p,
                User = p.User,
                LikesCount = p.Likes.Count(),
                CommentsCount = p.Comments.Count(),
                IsLiked = p.Likes.Any(l => l.UserId == viewerId)
            })
            .ToListAsync();

        return raw.Select(r => {
            r.Post.User = r.User;
            r.Post.LikesCount = r.LikesCount;
            r.Post.CommentsCount = r.CommentsCount;
            r.Post.IsLikedByCurrentUser = r.IsLiked;
            r.Post.Comments = null!;
            r.Post.Likes = null!;
            return r.Post;
        }).ToList();
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

    public async Task<List<TrendingTopic>> GetTrendingTopicsAsync(int take = 5)
    {
        var recentPosts = await _db.Posts
            .Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-30) && p.Caption != null)
            .Select(p => p.Caption)
            .Take(1000)
            .ToListAsync();

        var hashtags = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var regex = new System.Text.RegularExpressions.Regex(@"#\w+");

        foreach (var caption in recentPosts)
        {
            if (string.IsNullOrWhiteSpace(caption)) continue;
            var matches = regex.Matches(caption);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var tag = match.Value;
                if (hashtags.ContainsKey(tag))
                    hashtags[tag]++;
                else
                    hashtags[tag] = 1;
            }
        }

        return hashtags
            .Select(kv => new TrendingTopic { Tag = kv.Key, Count = kv.Value })
            .OrderByDescending(t => t.Count)
            .Take(take)
            .ToList();
    }
}
