# Okem Social

> Nền tảng mạng xã hội xây dựng trên ASP.NET Core 8 (MVC + Web API), cung cấp cả giao diện web (Razor Views) và RESTful API, hỗ trợ đăng bài, bình luận, thích, kết bạn, nhắn tin và thông báo theo thời gian thực qua SignalR.

## Overview

Okem Social là một ứng dụng web dạng monolith được xây dựng bằng ASP.NET Core 8.0, kết hợp mô hình MVC (phục vụ giao diện Razor) với một tập REST API riêng (`Controllers/Api`) dùng JWT cho các client khác (mobile, SPA...). Dữ liệu được lưu trong PostgreSQL thông qua Entity Framework Core, giao tiếp thời gian thực qua SignalR.

## Features

- **Xác thực & tài khoản**: Đăng ký, đăng nhập bằng email/mật khẩu (BCrypt), đăng nhập qua Google/Facebook (OAuth), JWT access token + refresh token, đăng xuất.
- **Hồ sơ người dùng**: Xem/cập nhật hồ sơ, đổi avatar/ảnh bìa, cài đặt tài khoản (thông báo, quyền riêng tư, tự phát video...).
- **Kết bạn**: Gửi/chấp nhận/huỷ lời mời kết bạn, hủy kết bạn, xem danh sách bạn bè và lời mời đến/đi.
- **Bảng tin & bài viết**: Tạo/sửa/xoá bài viết, upload media, chia sẻ (share) bài viết, xem bài viết theo người dùng, feed cá nhân.
- **Bình luận & thích**: Bình luận theo bài viết, xoá bình luận, toggle like cho bài viết.
- **Nhắn tin thời gian thực**: Tạo hội thoại 1-1/nhóm, gửi tin nhắn, đánh dấu đã đọc, đếm số tin chưa đọc — qua `ChatHub`.
- **Thông báo thời gian thực**: Lấy danh sách thông báo, đếm chưa đọc, đánh dấu đã đọc (một/tất cả) — qua `NotificationHub`.
- **Gọi thoại/video (signaling)**: `CallHub` phục vụ trao đổi tín hiệu WebRTC.
- **Media**: Upload ảnh/video (xử lý bằng SixLabors.ImageSharp), lưu tại `wwwroot/uploads`.

## Technology Stack

| Layer | Công nghệ |
|---|---|
| Backend Framework | ASP.NET Core 8.0 (MVC + Web API) |
| Ngôn ngữ | C# (.NET 8) |
| ORM | Entity Framework Core 8.0 |
| Database | PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`) |
| Real-time | ASP.NET Core SignalR (5 hub: Chat, Notification, Like, Comment, Call) |
| Authentication | Cookie Authentication (Web MVC) + JWT Bearer (API/SignalR); OAuth qua Google & Facebook |
| Mật khẩu | BCrypt.Net-Next |
| Xử lý ảnh | SixLabors.ImageSharp |
| Frontend | Razor Views (`.cshtml`), Tailwind CSS (CDN), Vanilla JavaScript, FontAwesome |
| Rate limiting | ASP.NET Core built-in Rate Limiter |
| Container | Docker / Docker Compose |

Ghi chú: `.csproj` có tham chiếu `StackExchange.Redis` / `Microsoft.Extensions.Caching.StackExchangeRedis` và `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, nhưng các package này **không được đăng ký/sử dụng** trong `Program.cs` hiện tại — do đó không được liệt kê là công nghệ đang hoạt động.

## Architecture

Kiến trúc dạng **Layered Monolith** (Controller → Service → Repository → DbContext), có thêm nhánh Web API song song với MVC:

```text
Client (Browser / API Consumer)
        ↓
Controllers (MVC: Home, Account, Posts... | Api: Auth, Posts, Users, Friends,...)
        ↓
Services (AuthService, UserService, JwtService, MediaService, NotificationService)
        ↓
Repositories (UserRepository, PostRepository, CommentRepository, LikeRepository,
              ConversationRepository, MessageRepository, NotificationRepository)
        ↓
ApplicationDbContext (Entity Framework Core)
        ↓
PostgreSQL

Song song: SignalR Hubs (ChatHub, LikeHub, CommentHub, NotificationHub, CallHub)
           dùng để đẩy cập nhật real-time tới client đang kết nối.
```

## Project Structure

```text
okemsocial/
├── Controllers/
│   ├── Api/              # REST API controllers (JWT-protected), trả JSON
│   └── *.cs               # MVC controllers, trả Razor View
├── Models/                 # EF Core entities (User, Post, Comment, Like, Message, Conversation,...)
├── DTOs/                   # Data Transfer Objects dùng cho API
├── Repositories/            # Repository pattern - truy cập dữ liệu qua DbContext
├── Services/                # Business logic (Auth, User, Jwt, Media, Notification)
├── Hubs/                    # SignalR Hubs (Chat, Like, Comment, Notification, Call)
├── Data/                    # ApplicationDbContext, DataSeeder
├── Migrations/               # EF Core migrations (PostgreSQL)
├── Views/                   # Razor Views theo từng Controller
├── wwwroot/                  # CSS, JS, uploads (ảnh/video)
├── Dockerfile
├── docker-compose.yml
└── appsettings.json
```

## Roles & Permissions

Hệ thống định nghĩa 2 vai trò trong `Models/Role.cs`:

| Role | Giá trị | Ghi chú |
|---|---|---|
| User | 0 | Vai trò mặc định khi đăng ký |
| Admin | 1 | Được lưu trên `User.Role`, hiện chưa thấy controller/policy nào áp dụng phân quyền `[Authorize(Roles = "Admin")]` trong source code hiện tại |

## API Documentation

Tất cả API nằm dưới `Controllers/Api`, phần lớn yêu cầu `[Authorize]` (JWT Bearer) trừ đăng ký/đăng nhập.

### Auth — `/api/auth`

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| POST | `/api/auth/register` | Đăng ký tài khoản | Không |
| POST | `/api/auth/login` | Đăng nhập, trả về access/refresh token | Không |
| POST | `/api/auth/refresh-token` | Làm mới access token | Không |
| POST | `/api/auth/logout` | Đăng xuất | Required |

### Users — `/api/users`

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| GET | `/api/users/me` | Lấy thông tin bản thân | Required |
| PUT | `/api/users/me` | Cập nhật hồ sơ | Required |
| PUT | `/api/users/me/avatar` | Cập nhật avatar | Required |
| GET | `/api/users/{id}` | Xem hồ sơ người dùng khác | Required |
| GET | `/api/users` | Tìm kiếm/danh sách người dùng | Required |
| GET | `/api/users/{id}/friends` | Danh sách bạn bè của user | Required |
| POST | `/api/users/{id}/friend-requests` | Gửi lời mời kết bạn | Required |
| POST | `/api/users/{id}/friend-requests/accept` | Chấp nhận lời mời | Required |
| DELETE | `/api/users/{id}/friend-requests` | Huỷ/từ chối lời mời | Required |
| DELETE | `/api/users/{id}/friends` | Huỷ kết bạn | Required |
| GET | `/api/users/me/friend-requests/incoming` | Lời mời đến | Required |
| GET | `/api/users/me/friend-requests/outgoing` | Lời mời đã gửi | Required |

### Friends — `/api/friends`

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| POST | `/api/friends/{targetUserId}` | Gửi lời mời kết bạn | Required |
| POST | `/api/friends/{fromUserId}/accept` | Chấp nhận lời mời | Required |
| DELETE | `/api/friends/{targetUserId}/request` | Huỷ lời mời đã gửi | Required |
| DELETE | `/api/friends/{targetUserId}` | Huỷ kết bạn | Required |

### Posts — `/api/posts`

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| GET | `/api/posts/feed` | Lấy bảng tin | Required |
| GET | `/api/posts/user/{userId}` | Bài viết theo người dùng | Required |
| POST | `/api/posts` | Tạo bài viết | Required |
| PUT | `/api/posts/{postId}` | Sửa bài viết | Required |
| DELETE | `/api/posts/{postId}` | Xoá bài viết | Required |
| POST | `/api/posts/{postId}/share` | Chia sẻ bài viết | Required |

### Comments — `/api/comments`

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| GET | `/api/comments/post/{postId}` | Lấy bình luận của bài viết | Required |
| POST | `/api/comments/post/{postId}` | Thêm bình luận | Required |
| DELETE | `/api/comments/{id}` | Xoá bình luận | Required |

### Likes — `/api/likes`

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| POST | `/api/likes/post/{postId}/toggle` | Bật/tắt thích bài viết | Required |

### Conversations & Messages

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| GET | `/api/conversations` | Danh sách hội thoại | Required |
| POST | `/api/conversations` | Tạo hội thoại (nhóm) | Required |
| POST | `/api/conversations/direct/{targetUserId}` | Tạo/lấy hội thoại 1-1 | Required |
| GET | `/api/conversations/{id}` | Chi tiết hội thoại | Required |
| PUT | `/api/conversations/{id}` | Cập nhật hội thoại | Required |
| POST | `/api/conversations/{id}/members` | Thêm thành viên | Required |
| DELETE | `/api/conversations/{id}/members/{userId}` | Xoá thành viên | Required |
| GET | `/api/conversations/unread-count` | Tổng tin chưa đọc | Required |
| POST | `/api/conversations/{id}/read` | Đánh dấu hội thoại đã đọc | Required |
| GET | `/api/conversations/{conversationId}/messages` | Lấy tin nhắn | Required |
| POST | `/api/conversations/{conversationId}/messages` | Gửi tin nhắn | Required |
| POST | `/api/conversations/{conversationId}/messages/{messageId}/read` | Đánh dấu tin đã đọc | Required |

### Notifications — `/api/notifications`

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| GET | `/api/notifications` | Danh sách thông báo | Required |
| GET | `/api/notifications/unread-count` | Số thông báo chưa đọc | Required |
| POST | `/api/notifications/{id}/read` | Đánh dấu đã đọc | Required |
| POST | `/api/notifications/read-all` | Đánh dấu tất cả đã đọc | Required |

### Media & Settings

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| POST | `/api/media/upload` | Upload ảnh/video | Required |
| GET | `/api/settings/me` | Lấy cài đặt tài khoản | Required |
| PUT | `/api/settings/me` | Cập nhật cài đặt tài khoản | Required |

### SignalR Hubs

| Hub | Path | Mục đích |
|---|---|---|
| ChatHub | `/hubs/chat` | Nhắn tin thời gian thực |
| LikeHub | `/hubs/likes` | Cập nhật lượt thích thời gian thực |
| CommentHub | `/hubs/comments` | Cập nhật bình luận thời gian thực |
| NotificationHub | `/hubs/notifications` | Đẩy thông báo thời gian thực |
| CallHub | `/hubs/call` | Signaling cho gọi thoại/video (WebRTC) |

Health check endpoint: `GET /health`.

## Authentication & Authorization

- **Web MVC**: Cookie Authentication (`HttpOnly`, `SameSite=Lax`, `Secure` khi Production), hỗ trợ đăng nhập qua Google và Facebook OAuth.
- **API/SignalR**: JWT Bearer token (access token + refresh token, lưu ở bảng `RefreshTokens`). Token có thể được gửi qua query string `?access_token=` khi kết nối tới các Hub SignalR (`/hubs/*`).
- Mật khẩu được băm bằng **BCrypt** trước khi lưu (`User.PasswordHash`), không lưu plain text.
- Có **Rate Limiting** toàn cục (mặc định 100 request/phút/partition) và cấu hình CORS `AllowAll`.
- Không có secret/API key thật nào được đưa vào tài liệu này.

## Database

- **Engine**: PostgreSQL
- **ORM**: Entity Framework Core 8.0 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Migration**: EF Core Migrations (thư mục `Migrations/`), tự động áp dụng khi ứng dụng khởi động (`DataSeeder.SeedAsync`), kèm seed dữ liệu mẫu.

Các entity chính (`DbSet` trong `ApplicationDbContext`): `User`, `FriendRequest`, `Post`, `Comment`, `Like`, `Media`, `Conversation`, `ConversationMember`, `Message`, `RefreshToken`, `Notification`.

```text
User
 ├── Post (1-n)
 │    ├── Comment (1-n)
 │    ├── Like (1-n)
 │    └── Media (1-n)
 ├── FriendRequest (FromUser / ToUser)
 ├── ConversationMember (n-n với Conversation)
 ├── Message (gửi trong Conversation)
 ├── RefreshToken (1-n)
 └── Notification (1-n)

Conversation
 ├── ConversationMember (1-n)
 └── Message (1-n)
```

> Lưu ý: `appsettings.json` và `docker-compose.yml` hiện chứa chuỗi kết nối/dịch vụ theo định dạng **SQL Server**, trong khi `Program.cs` và các migration thực tế sử dụng **Npgsql/PostgreSQL**. Đây là điểm chưa đồng bộ trong cấu hình mẫu của repo — khi cài đặt, cần dùng chuỗi kết nối PostgreSQL (xem mục Environment Variables) thay vì giá trị mặc định trong `appsettings.json`.

## Requirements

- .NET 8.0 SDK
- PostgreSQL (local hoặc qua Docker)
- `dotnet-ef` CLI (cho migration thủ công, không bắt buộc vì có auto-migrate)
- Docker & Docker Compose (nếu chạy bằng container — xem lưu ý ở mục Deployment)

## Installation

```bash
git clone https://github.com/hqcoder05/okemsocial.git
cd okemsocial
```

### Backend Setup

Cập nhật `ConnectionStrings:DefaultConnection` trong `appsettings.json` (hoặc qua biến môi trường) theo định dạng **Npgsql**, ví dụ:

```
Host=localhost;Port=5432;Database=OkemSocialDb;Username=postgres;Password=<your-password>
```

Sau đó chạy:

```bash
dotnet restore
dotnet run
```

Ứng dụng sẽ tự động migrate database và seed dữ liệu mẫu khi khởi động lần đầu (`DataSeeder.SeedAsync`).

## Environment Variables

Các biến/khoá cấu hình chính (không chứa giá trị thật):

```env
ConnectionStrings__DefaultConnection=   # Npgsql connection string tới PostgreSQL
Jwt__SecretKey=                          # Tối thiểu 32 ký tự
Jwt__Issuer=OkemSocial
Jwt__Audience=OkemSocialClient
Jwt__AccessTokenExpiryMinutes=60
Jwt__RefreshTokenExpiryDays=7
Google__ClientId=
Google__ClientSecret=
Facebook__AppId=
Facebook__AppSecret=
PORT=5070
```

## Running the Project

```bash
dotnet run
```

Mặc định lắng nghe trên `http://0.0.0.0:5070` (đọc từ biến môi trường `PORT`, mặc định `5070`).

## Demo Accounts (Dữ liệu mẫu)

Khi ứng dụng khởi động lần đầu, `DataSeeder` tự động tạo database và seed sẵn các tài khoản demo sau (nguồn: `Data/DataSeeder.cs`):

| Email | Mật khẩu |
|---|---|
| `demo@okemsocial.com` | `Password123@` |
| `quoc@okemsocial.com` | `Password123@` |
| `lan@okemsocial.com` | `Password123@` |
| `nam@okemsocial.com` | `Password123@` |
| `linh@okemsocial.com` | `Password123@` |

> Đây là dữ liệu seed mặc định dùng cho môi trường phát triển/demo, không phải bí mật production.

## Usage

1. Đăng ký/đăng nhập (email+mật khẩu hoặc Google/Facebook).
2. Cập nhật hồ sơ, avatar/ảnh bìa.
3. Gửi/chấp nhận lời mời kết bạn.
4. Đăng bài viết (kèm ảnh/video), thích, bình luận, chia sẻ.
5. Nhắn tin thời gian thực với bạn bè hoặc nhóm.
6. Nhận thông báo tức thì khi có tương tác.

## Testing

Testing is not currently implemented / documented.

## Deployment

- **Docker**: có `Dockerfile` (multi-stage build, publish `.NET 8` runtime image).
- **Docker Compose**: `docker-compose.yml` định nghĩa 2 service — `web` (ứng dụng) và `sqlserver` (SQL Server 2022). *Lưu ý: file compose này dùng SQL Server, không khớp với code hiện tại đang dùng Npgsql/PostgreSQL — cần cập nhật service database sang PostgreSQL trước khi dùng compose để chạy thực tế.*
- Ứng dụng đọc `PORT` từ biến môi trường và có health check tại `/health`, forwarded headers được cấu hình sẵn — phù hợp để deploy sau reverse proxy.

## Troubleshooting

- Nếu ứng dụng không kết nối được database, kiểm tra lại `ConnectionStrings:DefaultConnection` đúng định dạng PostgreSQL (Npgsql), không phải định dạng SQL Server hiện có trong `appsettings.json` mẫu.
- Nếu SignalR không nhận được sự kiện real-time khi dùng client ngoài trình duyệt, đảm bảo gửi JWT qua query string `?access_token=` khi kết nối `/hubs/*`.

## Author

**Hoàng Nguyễn Viết Quốc**
- Backend Developer — sinh viên năm cuối ngành Công nghệ Thông tin, Trường Đại học Giao thông Vận tải TP. HCM
- GitHub: [@hqcoder05](https://github.com/hqcoder05)
- Tech stack: Java, Spring Boot, C#, ASP.NET Core, PostgreSQL, SQL Server, Docker, JWT Authentication, SignalR, Linux/Unix

## License

Not specified in source code.
