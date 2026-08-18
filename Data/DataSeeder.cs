using Microsoft.EntityFrameworkCore;
using okem_social.Models;

namespace okem_social.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        try
        {
            logger.LogInformation("Applying migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Checking database seed state...");

            var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123@");

            if (await db.Users.AnyAsync())
            {
                logger.LogInformation("Database already has users. Updating profile pictures, covers, headlines if needed...");
                var usersToUpdate = await db.Users.ToListAsync();
                foreach (var u in usersToUpdate)
                {
                    if (string.IsNullOrEmpty(u.AvatarUrl))
                    {
                        if (u.Email.Contains("quoc")) u.AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=400&q=80";
                        else if (u.Email.Contains("lan")) u.AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=400&q=80";
                        else if (u.Email.Contains("nam")) u.AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&q=80";
                        else u.AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=400&q=80";
                    }
                    if (string.IsNullOrEmpty(u.CoverUrl))
                    {
                        u.CoverUrl = "https://images.unsplash.com/photo-1707343843437-caacff5cfa74?w=1200&q=80";
                    }
                    if (string.IsNullOrEmpty(u.Headline))
                    {
                        if (u.Email.Contains("quoc")) u.Headline = "Fullstack .NET & AI Lead Architect";
                        else if (u.Email.Contains("lan")) u.Headline = "Senior UI/UX Product Designer";
                        else if (u.Email.Contains("nam")) u.Headline = "DevOps & Cloud Specialist";
                        else u.Headline = "Thành viên tích cực tại Okem Social";
                    }
                    if (string.IsNullOrEmpty(u.Location)) u.Location = "Hồ Chí Minh, Việt Nam";
                    if (string.IsNullOrEmpty(u.WebsiteUrl)) u.WebsiteUrl = "https://github.com/hqcoder05";
                }
                await db.SaveChangesAsync();
                return;
            }

            logger.LogInformation("Seeding initial rich demo data for Okem Social...");

            // 1. Create Demo Users with real Avatars, Covers, Headlines, Locations
            var userDemo = new User
            {
                Email = "demo@okemsocial.com",
                FullName = "Hoàng Quốc",
                Nickname = "hoang_quoc",
                Headline = "Lead Fullstack & AI Engineer",
                Bio = "Đam mê công nghệ, xây dựng sản phẩm chất lượng cao với .NET 8, React & AI 🚀",
                Location = "Hồ Chí Minh, Việt Nam",
                WebsiteUrl = "https://github.com/hqcoder05",
                AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=400&q=80",
                CoverUrl = "https://images.unsplash.com/photo-1707343843437-caacff5cfa74?w=1200&q=80",
                PasswordHash = defaultPasswordHash,
                Role = Role.User,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            };

            var userQuoc = new User
            {
                Email = "quoc@okemsocial.com",
                FullName = "Quốc Hoàng",
                Nickname = "quoc_coder",
                Headline = "CTO & Software Architect at TechCorp",
                Bio = "Kiến trúc sư phần mềm | Chuyên gia phân tán hệ thống & Real-time Web Applications.",
                Location = "Đà Nẵng, Việt Nam",
                WebsiteUrl = "https://github.com/hqcoder05/okem-client",
                AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=400&q=80",
                CoverUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=1200&q=80",
                PasswordHash = defaultPasswordHash,
                Role = Role.Admin,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };

            var userLan = new User
            {
                Email = "lan@okemsocial.com",
                FullName = "Mai Lan",
                Nickname = "mai_lan",
                Headline = "Product Designer | Creative Visual Artist",
                Bio = "Yêu nhiếp ảnh, du lịch và chia sẻ những khoảnh khắc đẹp trong cuộc sống 🌿📷",
                Location = "Hà Nội, Việt Nam",
                WebsiteUrl = "https://behance.net",
                AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=400&q=80",
                CoverUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=1200&q=80",
                PasswordHash = defaultPasswordHash,
                Role = Role.User,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            };

            var userNam = new User
            {
                Email = "nam@okemsocial.com",
                FullName = "Nam Trần",
                Nickname = "nam_tech",
                Headline = "DevOps & Cloud Engineer | K8s & Docker Enthusiast",
                Bio = "Tự động hóa CI/CD, tối ưu hóa hạ tầng điện toán đám mây và microservices 💻",
                Location = "Hồ Chí Minh, Việt Nam",
                WebsiteUrl = "https://linkedin.com",
                AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&q=80",
                CoverUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=1200&q=80",
                PasswordHash = defaultPasswordHash,
                Role = Role.User,
                CreatedAt = DateTime.UtcNow.AddDays(-12)
            };

            var userLinh = new User
            {
                Email = "linh@okemsocial.com",
                FullName = "Thùy Linh",
                Nickname = "thuy_linh",
                Headline = "Frontend Engineer | React & Tailwind Enthusiast",
                Bio = "Xây dựng trải nghiệm người dùng hiện đại, thanh lịch và mượt mà ✨",
                Location = "Hà Nội, Việt Nam",
                WebsiteUrl = "https://github.com",
                AvatarUrl = "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=400&q=80",
                CoverUrl = "https://images.unsplash.com/photo-1579546929518-9e396f3cc809?w=1200&q=80",
                PasswordHash = defaultPasswordHash,
                Role = Role.User,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            };

            db.Users.AddRange(userDemo, userQuoc, userLan, userNam, userLinh);
            await db.SaveChangesAsync();

            // 2. Friendships
            db.FriendRequests.AddRange(
                new FriendRequest { FromUserId = userDemo.Id, ToUserId = userQuoc.Id, IsAccepted = true, CreatedAt = DateTime.UtcNow.AddDays(-9) },
                new FriendRequest { FromUserId = userLan.Id, ToUserId = userDemo.Id, IsAccepted = true, CreatedAt = DateTime.UtcNow.AddDays(-8) },
                new FriendRequest { FromUserId = userNam.Id, ToUserId = userDemo.Id, IsAccepted = true, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new FriendRequest { FromUserId = userLinh.Id, ToUserId = userDemo.Id, IsAccepted = true, CreatedAt = DateTime.UtcNow.AddDays(-4) },
                new FriendRequest { FromUserId = userQuoc.Id, ToUserId = userLan.Id, IsAccepted = true, CreatedAt = DateTime.UtcNow.AddDays(-7) }
            );
            await db.SaveChangesAsync();

            // 3. Posts
            var postQuoc = new Post
            {
                UserId = userQuoc.Id,
                Caption = "Chào mừng tất cả các bạn đến với Okem Social! 🎉 Nền tảng kết nối mạng lưới nghề nghiệp hiện đại, chia sẻ hình ảnh và gọi video realtime SignalR chuẩn WebRTC.",
                ImageUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=800&q=80",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            var postLan = new Post
            {
                UserId = userLan.Id,
                Caption = "Một chiều hoàng hôn tuyệt đẹp bên bờ biển 🌅 'Mỗi ngày là một cơ hội mới để bắt đầu lại.'",
                ImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=800&q=80",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };

            var postNam = new Post
            {
                UserId = userNam.Id,
                Caption = "Triển khai hạ tầng Docker và SQL Server container hóa hoàn tất 100%! Tốc độ build và phản hồi cực kỳ ấn tượng ⚡ #DevOps #Docker #DotNet8",
                ImageUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=800&q=80",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var postDemo = new Post
            {
                UserId = userDemo.Id,
                Caption = "Hôm nay là một ngày tuyệt vời để khám phá những điều mới mẻ! 🚀 Chúc mọi người có một tuần làm việc hiệu quả và tràn đầy năng lượng cùng Okem.",
                CreatedAt = DateTime.UtcNow.AddHours(-12)
            };

            db.Posts.AddRange(postQuoc, postLan, postNam, postDemo);
            await db.SaveChangesAsync();

            // 4. Likes & Comments
            db.Likes.AddRange(
                new Like { PostId = postQuoc.Id, UserId = userDemo.Id, CreatedAt = DateTime.UtcNow.AddDays(-4) },
                new Like { PostId = postQuoc.Id, UserId = userLan.Id, CreatedAt = DateTime.UtcNow.AddDays(-4) },
                new Like { PostId = postLan.Id, UserId = userDemo.Id, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new Like { PostId = postNam.Id, UserId = userDemo.Id, CreatedAt = DateTime.UtcNow.AddHours(-18) },
                new Like { PostId = postDemo.Id, UserId = userQuoc.Id, CreatedAt = DateTime.UtcNow.AddHours(-6) },
                new Like { PostId = postDemo.Id, UserId = userLinh.Id, CreatedAt = DateTime.UtcNow.AddHours(-4) }
            );

            db.Comments.AddRange(
                new Comment
                {
                    PostId = postQuoc.Id,
                    UserId = userDemo.Id,
                    Content = "Giao diện Okem tối giản, sang trọng và mượt mà quá! Chúc mừng team 👏",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new Comment
                {
                    PostId = postQuoc.Id,
                    UserId = userLan.Id,
                    Content = "Tính năng gọi video chất lượng cao thật ấn tượng!",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Comment
                {
                    PostId = postLan.Id,
                    UserId = userDemo.Id,
                    Content = "Cảnh hoàng hôn đẹp xuất sắc luôn bạn ơi 😍",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Comment
                {
                    PostId = postNam.Id,
                    UserId = userLinh.Id,
                    Content = "Kiến trúc hệ thống chạy rất nhẹ và ổn định!",
                    CreatedAt = DateTime.UtcNow.AddHours(-10)
                }
            );
            await db.SaveChangesAsync();

            // 5. Conversations & Messages
            var convDemoQuoc = new Conversation
            {
                IsGroup = false,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            var convDemoLan = new Conversation
            {
                IsGroup = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddHours(-2)
            };
            db.Conversations.AddRange(convDemoQuoc, convDemoLan);
            await db.SaveChangesAsync();

            db.ConversationMembers.AddRange(
                new ConversationMember { ConversationId = convDemoQuoc.Id, UserId = userDemo.Id, JoinedAt = DateTime.UtcNow.AddDays(-3), LastReadAt = DateTime.UtcNow },
                new ConversationMember { ConversationId = convDemoQuoc.Id, UserId = userQuoc.Id, JoinedAt = DateTime.UtcNow.AddDays(-3), LastReadAt = DateTime.UtcNow },
                new ConversationMember { ConversationId = convDemoLan.Id, UserId = userDemo.Id, JoinedAt = DateTime.UtcNow.AddDays(-2), LastReadAt = DateTime.UtcNow },
                new ConversationMember { ConversationId = convDemoLan.Id, UserId = userLan.Id, JoinedAt = DateTime.UtcNow.AddDays(-2), LastReadAt = DateTime.UtcNow }
            );

            db.Messages.AddRange(
                new Message
                {
                    ConversationId = convDemoQuoc.Id,
                    SenderId = userQuoc.Id,
                    Content = "Chào Quốc! Cảm ơn bạn đã trải nghiệm Okem Social.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Message
                {
                    ConversationId = convDemoQuoc.Id,
                    SenderId = userDemo.Id,
                    Content = "Chào bạn! Hệ thống chạy rất mượt, có cả gọi thoại và video WebRTC nữa.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Message
                {
                    ConversationId = convDemoQuoc.Id,
                    SenderId = userQuoc.Id,
                    Content = "Đúng rồi bạn, toàn bộ chức năng chat và call đều hỗ trợ realtime SignalR 💬",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                },
                new Message
                {
                    ConversationId = convDemoLan.Id,
                    SenderId = userLan.Id,
                    Content = "Chào Quốc! Bộ thiết kế giao diện Okem mới rất đẹp đó!",
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                }
            );

            // 6. Notifications for Demo User
            db.Notifications.AddRange(
                new Notification
                {
                    UserId = userDemo.Id,
                    Type = "friend_request",
                    Title = "Kết nối thành công",
                    Content = "Thùy Linh đã trở thành bạn bè với bạn.",
                    Url = "/Users/Search",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-3)
                },
                new Notification
                {
                    UserId = userDemo.Id,
                    Type = "post_like",
                    Title = "Có người đã thích bài viết của bạn",
                    Content = "Quốc Hoàng vừa thích bài viết của bạn.",
                    Url = $"/Posts/Feed#post-{postDemo.Id}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-6)
                },
                new Notification
                {
                    UserId = userDemo.Id,
                    Type = "comment",
                    Title = "Bình luận mới",
                    Content = "Thùy Linh đã bình luận về bài viết của Nam Trần.",
                    Url = $"/Posts/Feed#post-{postNam.Id}",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddHours(-10)
                }
            );

            await db.SaveChangesAsync();
            logger.LogInformation("Rich demo data seeded successfully for Okem Social!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while migrating and seeding database: {Message}", ex.Message);
        }
    }
}
