using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using okem_social.Data;
using okem_social.DTOs;
using okem_social.Models;
using okem_social.Repositories;
using okem_social.Services;

namespace okem_social.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController(
    IUserRepository userRepo,
    IJwtService jwtService,
    ApplicationDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        // Check if email exists
        var existing = await userRepo.GetByEmailAsync(dto.Email);
        if (existing != null)
        {
            return BadRequest(new { message = "Email already exists" });
        }

        var user = new User
        {
            Email = dto.Email,
            FullName = dto.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = Role.User
        };

        // Save user to database
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();
        await jwtService.CreateRefreshTokenAsync(user.Id, refreshToken);

        return Ok(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            }
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await userRepo.GetByEmailAsync(dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();
        await jwtService.CreateRefreshTokenAsync(user.Id, refreshToken);

        return Ok(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            }
        });
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken(RefreshTokenDto dto)
    {
        var validToken = await jwtService.ValidateRefreshTokenAsync(dto.RefreshToken);
        if (validToken == null || validToken.User == null)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }

        // Revoke old token
        await jwtService.RevokeRefreshTokenAsync(dto.RefreshToken);

        // Generate new tokens
        var accessToken = jwtService.GenerateAccessToken(validToken.User);
        var newRefreshToken = jwtService.GenerateRefreshToken();
        await jwtService.CreateRefreshTokenAsync(validToken.User.Id, newRefreshToken);

        return Ok(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            User = new UserDto
            {
                Id = validToken.User.Id,
                Email = validToken.User.Email,
                FullName = validToken.User.FullName,
                Role = validToken.User.Role.ToString(),
                CreatedAt = validToken.User.CreatedAt
            }
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        await jwtService.RevokeRefreshTokenAsync(dto.RefreshToken);
        return Ok(new { message = "Logged out successfully" });
    }
}
