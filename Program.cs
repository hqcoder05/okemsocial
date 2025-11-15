using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using okem_social.Data;
using okem_social.Models;
using okem_social.Hubs;

// DI cho Repo/Service
using okem_social.Repositories;
using okem_social.Services;

var builder = WebApplication.CreateBuilder(args);

// Bật chế độ timestamp legacy cho Npgsql (tránh lỗi với DateTime)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Lắng nghe trên tất cả interface
var port = Environment.GetEnvironmentVariable("PORT") ?? "5070";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// EF Core + PostgreSQL (Supabase)
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Auth configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey not configured");

// Authentication: Cookie cho MVC, JWT cho API/SignalR
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    // Hỗ trợ SignalR: nhận token từ query ?access_token=
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// SignalR với cấu hình tối ưu cho calling
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB cho WebRTC signaling
});

builder.Services.AddControllersWithViews();

// Đăng ký Repository/Service
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ILikeRepository, LikeRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// Global exception handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

        logger.LogError(exception, "Unhandled exception occurred");

        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    });
});

// Exception page & HSTS cho Production
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Redirect HTTP -> HTTPS khi không phải Development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map các Hub SignalR
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<LikeHub>("/hubs/likes");
app.MapHub<CommentHub>("/hubs/comments");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<CallHub>("/hubs/call"); // ← THÊM DÒNG NÀY cho chức năng gọi thoại/video

app.Run();


// ==========================================
// HOẶC nếu bạn dùng Startup.cs pattern:
// ==========================================

// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // ... các services khác

        // SignalR với cấu hình tối ưu
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);

            // Tăng message size nếu cần (cho video calling)
            options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
        });

        // CORS nếu cần (cho development)
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // ... middleware khác

        app.UseRouting();

        // CORS
        app.UseCors("AllowAll");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapRazorPages();

            // SignalR Hubs
            endpoints.MapHub<ChatHub>("/hubs/chat");
            endpoints.MapHub<CallHub>("/hubs/call");
        });
    }
}