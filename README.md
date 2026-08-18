# Okem Social

Một nền tảng mạng xã hội hiện đại được xây dựng với ASP.NET Core 8.0, Entity Framework Core và Tailwind CSS. Okem Social hỗ trợ đầy đủ các tính năng cơ bản của một mạng xã hội như: Đăng bài, Thích, Bình luận, Theo dõi/Kết bạn, Nhắn tin theo thời gian thực (Real-time Chat) và Nhận thông báo (Real-time Notifications).

Dự án này cung cấp cả Web UI (Render bằng Razor Views) và RESTful API cho di động hoặc các frontend framework khác (React/Vue/Angular).

## 🚀 Tính năng nổi bật

- **Tài khoản & Hồ sơ**: Đăng ký, Đăng nhập, Quản lý hồ sơ, Cập nhật ảnh đại diện/ảnh bìa (Avatar/Cover).
- **Mạng lưới kết nối**: Gửi lời mời kết bạn, Theo dõi (Follow), Danh sách bạn bè.
- **Bảng tin (Feed)**: Đăng bài viết (hỗ trợ ảnh/video), Thích (Like), Bình luận (Comment), Chia sẻ (Share) bài viết về tường nhà.
- **Nhắn tin (Chat)**: Trò chuyện 1-1, trò chuyện nhóm theo thời gian thực với SignalR.
- **Thông báo (Notifications)**: Thông báo tức thì khi có người thích, bình luận bài viết hoặc gửi lời mời kết bạn.
- **UI/UX hiện đại**: Giao diện được thiết kế lại hoàn toàn với Tailwind CSS (Mobile-first, Responsive, phong cách tối giản).

## 🛠 Tech Stack

- **Backend Framework**: ASP.NET Core 8.0 (MVC + Web API)
- **Database**: Microsoft SQL Server 2022
- **ORM**: Entity Framework Core 8.0
- **Real-time Engine**: ASP.NET Core SignalR
- **Frontend**: Razor Views (`.cshtml`), Tailwind CSS (qua CDN), Vanilla JavaScript, FontAwesome 6
- **Authentication**: JWT (JSON Web Tokens) cho API & Cookie Authentication cho MVC Web
- **Bảo mật**: BCrypt (Mã hóa mật khẩu)

## 📦 Hướng dẫn cài đặt & Chạy cục bộ (Local)

### Yêu cầu hệ thống (Prerequisites)
1. **.NET 8.0 SDK**: Cần cài đặt bản SDK mới nhất của .NET 8.
2. **SQL Server**: Có thể dùng SQL Server LocalDB, SQL Server Developer Edition hoặc SQL Server qua Docker.
3. **IDE**: Visual Studio 2022, Rider hoặc VS Code.

### Cài đặt từng bước

**Bước 1: Clone dự án**
```bash
git clone https://github.com/hqcoder05/socialokem.git
cd socialokem
```

**Bước 2: Cấu hình Chuỗi kết nối (Connection String)**
Mở tệp `appsettings.json` hoặc `appsettings.Development.json` và thay đổi `DefaultConnection` cho phù hợp với SQL Server của bạn. 
*Ví dụ dùng SQL Server LocalDB:*
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OkemSocialDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

**Bước 3: Chạy Migration để tạo Database**
Mở Terminal/Command Prompt tại thư mục dự án và chạy lệnh sau (yêu cầu đã cài đặt `dotnet-ef`):
```bash
dotnet tool install --global dotnet-ef
dotnet ef database update
```
*Lưu ý: Ứng dụng cũng đã được tích hợp tính năng tự động tạo Database (Auto-Migration) và tự động tạo dữ liệu mẫu (Data Seeding) ngay khi chạy lần đầu tiên.*

**Bước 4: Chạy ứng dụng**
```bash
dotnet run
```
Ứng dụng sẽ khởi chạy tại `http://localhost:5000` hoặc `https://localhost:5001`.

## 🧑‍💻 Tài khoản Demo (Dữ liệu mẫu)

Nếu ứng dụng tự động chạy Data Seeder, bạn có thể đăng nhập bằng các tài khoản sau:
- **Email**: `quoc@okem.vn` / `lan@okem.vn` / `nam@okem.vn` / `minh@okem.vn` / `hoa@okem.vn`
- **Mật khẩu chung**: `Password123@`

## 📁 Cấu trúc thư mục dự án

```
okemsocial/
├── Controllers/
│   ├── Api/                 # Chứa các Web API (Trao đổi dữ liệu dạng JSON)
│   └── ...                  # Chứa các MVC Controllers (Trả về Razor Views)
├── Models/                  # Entities cho Database (User, Post, Comment, Like, Message,...)
├── DTOs/                    # Data Transfer Objects
├── Repositories/            # Lớp truy cập dữ liệu (Repository Pattern)
├── Services/                # Lớp nghiệp vụ xử lý Logic (AuthService, UserService,...)
├── Hubs/                    # Các Hub của SignalR (ChatHub, NotificationHub)
├── Data/                    # DbContext & Migrations & DataSeeder
├── Views/                   # Chứa giao diện (.cshtml) được chia theo Controller
└── wwwroot/                 # Chứa CSS, JS và thư mục uploads (ảnh/video tải lên)
```

## 📝 Nhật ký cập nhật (Changelog) gần nhất
- **UI/UX Redesign**: Chuyển đổi toàn bộ từ Bootstrap sang Tailwind CSS để giao diện đẹp và hiện đại hơn.
- **Tính năng Chia sẻ**: Cập nhật khả năng chia sẻ bài viết của người khác lên trang cá nhân của bản thân.
- **UI Bình luận**: Hỗ trợ khả năng tự xóa bình luận của cá nhân.
- **Lỗi Avatar**: Tích hợp cơ chế Fallback Avatar (Dùng avatar chữ cái qua API ui-avatars.com) để khắc phục lỗi mất hình trên các nền tảng deploy dùng bộ nhớ tạm (như Render Free Tier).

## 📄 Bản quyền (License)

MIT License - Copyright (c) 2026 Okem Social

---
**Happy Coding! 🎉**
