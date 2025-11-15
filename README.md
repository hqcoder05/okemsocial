# Okem-Social

Mạng xã hội đầy đủ tính năng với ASP.NET Core MVC + RESTful API + SignalR Chat

## 🚀 Tính năng

### 1. Auth / Account ✅
- **POST** `/api/auth/register` - Đăng ký tài khoản
- **POST** `/api/auth/login` - Đăng nhập (JWT)
- **POST** `/api/auth/refresh-token` - Làm mới token
- **POST** `/api/auth/logout` - Đăng xuất

### 2. User / Profile ✅
- **GET** `/api/users/me` - Xem hồ sơ của mình
- **PUT** `/api/users/me` - Cập nhật hồ sơ
- **GET** `/api/users/{id}` - Xem hồ sơ người dùng
- **GET** `/api/users?keyword=...` - Tìm kiếm người dùng
- **PUT** `/api/users/me/avatar` - Upload avatar
- **GET** `/api/users/{id}/followers` - Danh sách followers
- **GET** `/api/users/{id}/following` - Danh sách following

### 3. Follow ✅
- **POST** `/api/follows/{targetUserId}` - Theo dõi
- **DELETE** `/api/follows/{targetUserId}` - Bỏ theo dõi

### 4. Post ✅
- **GET** `/api/posts/feed` - Newsfeed (bài viết của người mình follow)
- **GET** `/api/posts/user/{userId}` - Bài viết của một người
- **POST** `/api/posts` - Đăng bài mới
- **PUT** `/api/posts/{postId}` - Sửa bài viết
- **DELETE** `/api/posts/{postId}` - Xóa bài viết

### 5. Comment ✅
- **GET** `/api/posts/{postId}/comments` - Xem comments
- **POST** `/api/posts/{postId}/comments` - Thêm comment
- **DELETE** `/api/comments/{commentId}` - Xóa comment

### 6. Like ✅
- **POST** `/api/posts/{postId}/likes` - Like bài viết
- **DELETE** `/api/posts/{postId}/likes` - Unlike bài viết
- **GET** `/api/posts/{postId}/likes` - Xem danh sách likes

### 7. Media ✅
- **POST** `/api/media/upload?type=image|video` - Upload ảnh/video

### 8. Chat / Message ✅

#### Conversation API
- **GET** `/api/conversations` - Danh sách hội thoại
- **POST** `/api/conversations` - Tạo hội thoại (1-1 hoặc group)
- **GET** `/api/conversations/{id}` - Chi tiết hội thoại
- **PUT** `/api/conversations/{id}` - Đổi tên group
- **POST** `/api/conversations/{id}/members` - Thêm thành viên
- **DELETE** `/api/conversations/{id}/members/{userId}` - Xóa thành viên
- **GET** `/api/conversations/unread-count` - Tổng tin nhắn chưa đọc
- **POST** `/api/conversations/{id}/read` - Đánh dấu đã đọc

#### Message API
- **GET** `/api/conversations/{id}/messages?before=...` - Lấy tin nhắn
- **POST** `/api/conversations/{id}/messages` - Gửi tin nhắn

#### SignalR Hub
**URL**: `/hubs/chat`

**Methods**:
- `SendMessage(conversationId, content, attachmentUrl)` - Gửi tin nhắn realtime
- `Typing(conversationId)` - Thông báo đang gõ
- `Seen(conversationId, messageId)` - Đánh dấu đã xem
- `JoinConversation(conversationId)` - Tham gia room

**Events** (Client nhận):
- `ReceiveMessage` - Nhận tin nhắn mới
- `UserTyping` - Người dùng đang gõ
- `MessageSeen` - Tin nhắn đã được xem

## 🛠️ Tech Stack

- **Backend**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server 2022
- **ORM**: Entity Framework Core
- **Authentication**: JWT + Cookie (dual mode)
- **Realtime**: SignalR
- **Password**: BCrypt
- **Image Processing**: SixLabors.ImageSharp
- **Docker**: SQL Server container

## 📦 Cài đặt

### 1. Prerequisites
- .NET 8.0 SDK
- Docker Desktop
- Visual Studio 2022 hoặc VS Code

### 2. Clone & Setup

```bash
git clone https://github.com/hqcoder05/Okem-Social.git
cd Okem-Social
```

### 3. Cấu hình Database

Cập nhật connection string trong `appsettings.json` nếu cần:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=okem_social_db;User Id=sa;Password=Aa123456@;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

### 4. Start SQL Server

```bash
# Tạo file .env với SA_PASSWORD
echo SA_PASSWORD=Aa123456@ > .env

# Start SQL Server container
docker-compose up -d
```

### 5. Apply Migrations

```bash
dotnet ef database update
```

### 6. Run Application

```bash
dotnet run
```

App sẽ chạy tại:
- MVC: `https://localhost:5001`
- API: `https://localhost:5001/api`
- SignalR Hub: `wss://localhost:5001/hubs/chat`

## 📝 API Authentication

### MVC (Cookie)
Sử dụng form login tại `/Account/Login`

### API (JWT)

**1. Register/Login**:
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response**:
```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "base64string...",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "fullName": "John Doe",
    "role": "User"
  }
}
```

**2. Sử dụng Access Token**:
```http
GET /api/users/me
Authorization: Bearer eyJhbGci...
```

**3. Refresh Token**:
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "base64string..."
}
```

## 🔌 SignalR Connection

### JavaScript Example

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", {
        accessTokenFactory: () => localStorage.getItem("accessToken")
    })
    .build();

// Nhận tin nhắn
connection.on("ReceiveMessage", (message) => {
    console.log("New message:", message);
});

// Gửi tin nhắn
await connection.invoke("SendMessage", conversationId, "Hello!", null);

// Typing indicator
await connection.invoke("Typing", conversationId);

// Seen message
await connection.invoke("Seen", conversationId, messageId);

await connection.start();
```

## 📂 Project Structure

```
Okem-Social/
├── Controllers/
│   ├── Api/              # API Controllers
│   │   ├── AuthApiController.cs
│   │   ├── UsersApiController.cs
│   │   ├── FollowsApiController.cs
│   │   ├── PostsApiController.cs
│   │   ├── CommentsApiController.cs
│   │   ├── LikesApiController.cs
│   │   ├── MediaApiController.cs
│   │   ├── ConversationsApiController.cs
│   │   └── MessagesApiController.cs
│   ├── AccountController.cs   # MVC Auth
│   ├── ProfileController.cs
│   ├── UsersController.cs
│   └── HomeController.cs
├── Models/
│   ├── User.cs
│   ├── Follow.cs
│   ├── Post.cs
│   ├── Comment.cs
│   ├── Like.cs
│   ├── Media.cs
│   ├── Conversation.cs
│   ├── ConversationMember.cs
│   ├── Message.cs
│   └── RefreshToken.cs
├── DTOs/
│   └── ApiDtos.cs
├── Repositories/
│   ├── IUserRepository.cs / UserRepository.cs
│   ├── IPostRepository.cs / PostRepository.cs
│   ├── ICommentRepository.cs / CommentRepository.cs
│   ├── ILikeRepository.cs / LikeRepository.cs
│   ├── IConversationRepository.cs / ConversationRepository.cs
│   └── IMessageRepository.cs / MessageRepository.cs
├── Services/
│   ├── IAuthService.cs / AuthService.cs
│   ├── IUserService.cs / UserService.cs
│   ├── IJwtService.cs / JwtService.cs
│   └── IMediaService.cs / MediaService.cs
├── Hubs/
│   └── ChatHub.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Migrations/
├── Views/              # MVC Views
├── wwwroot/            # Static files
│   └── uploads/        # Uploaded media
├── Program.cs
├── appsettings.json
└── docker-compose.yml
```

## 🔑 Default Admin Account

```
Email: admin@okem.vn
Password: Admin!12345
```

## 📸 Upload Media

Upload ảnh/video trước, sau đó dùng URL trong post:

```http
POST /api/media/upload?type=image
Content-Type: multipart/form-data
Authorization: Bearer {token}

file: [binary]
```

Response:
```json
{
  "url": "/uploads/images/guid.jpg"
}
```

Sau đó tạo post:
```http
POST /api/posts
Authorization: Bearer {token}

{
  "caption": "Beautiful day!",
  "imageUrl": "/uploads/images/guid.jpg"
}
```

## 🚧 TODO / Future Features

- [ ] Notifications (realtime)
- [ ] Stories (24h posts)
- [ ] Hashtags
- [ ] Mentions (@user)
- [ ] Block/Report users
- [ ] Email verification
- [ ] Password reset
- [ ] OAuth (Google, Facebook)
- [ ] Admin dashboard
- [ ] Analytics

## 📄 License

MIT License - Copyright (c) 2025 Okem Social

## 👨‍💻 Author

**hqcoder05**
- GitHub: [@hqcoder05](https://github.com/hqcoder05)

---

**Happy Coding! 🎉**
