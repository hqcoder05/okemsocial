using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.DTOs;
using okem_social.Models;
using okem_social.Repositories;

namespace okem_social.Controllers.Api;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsApiController(
    IConversationRepository conversationRepo,
    IMessageRepository messageRepo,
    IUserRepository userRepo) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<ConversationDto>>> GetConversations()
    {
        var conversations = await conversationRepo.GetUserConversationsAsync(CurrentUserId);
        var dtos = new List<ConversationDto>();

        foreach (var conv in conversations)
        {
            dtos.Add(await MapToConversationDto(conv));
        }

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<ConversationDto>> CreateConversation(CreateConversationDto dto)
    {
        if (dto.MemberIds.Count == 0)
            return BadRequest(new { message = "Cần ít nhất một thành viên." });

        // Add current user to members
        if (!dto.MemberIds.Contains(CurrentUserId))
            dto.MemberIds.Add(CurrentUserId);

        // Validate all members exist
        foreach (var memberId in dto.MemberIds)
        {
            var user = await userRepo.GetByIdAsync(memberId);
            if (user == null)
                return BadRequest(new { message = $"Không tìm thấy người dùng #{memberId}." });
        }

        var conversation = new Conversation
        {
            Name = dto.Name,
            IsGroup = dto.MemberIds.Count > 2
        };

        var created = await conversationRepo.CreateAsync(conversation, dto.MemberIds);
        return Ok(await MapToConversationDto(created));
    }

    [HttpPost("direct/{targetUserId}")]
    public async Task<ActionResult<ConversationDto>> GetOrCreateDirectConversation(int targetUserId)
    {
        if (targetUserId == CurrentUserId)
            return BadRequest(new { message = "Không thể tạo cuộc trò chuyện với chính mình." });

        var targetUser = await userRepo.GetByIdAsync(targetUserId);
        if (targetUser == null)
            return NotFound(new { message = "Không tìm thấy người dùng." });

        // Check if direct conversation already exists
        var userConvs = await conversationRepo.GetUserConversationsAsync(CurrentUserId);
        var existing = userConvs.FirstOrDefault(c => !c.IsGroup && c.Members.Any(m => m.UserId == targetUserId));
        if (existing != null)
        {
            return Ok(await MapToConversationDto(existing));
        }

        // Create new direct conversation
        var conversation = new Conversation
        {
            Name = targetUser.FullName,
            IsGroup = false
        };

        var created = await conversationRepo.CreateAsync(conversation, new List<int> { CurrentUserId, targetUserId });
        return Ok(await MapToConversationDto(created));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ConversationDto>> GetConversation(int id)
    {
        if (!await conversationRepo.IsMemberAsync(id, CurrentUserId))
            return Forbid();

        var conversation = await conversationRepo.GetByIdAsync(id, true);
        if (conversation == null)
            return NotFound();

        return Ok(await MapToConversationDto(conversation));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateConversation(int id, UpdateConversationDto dto)
    {
        if (!await conversationRepo.IsMemberAsync(id, CurrentUserId))
            return Forbid();

        var conversation = await conversationRepo.GetByIdAsync(id, false);
        if (conversation == null)
            return NotFound();

        if (!conversation.IsGroup)
            return BadRequest(new { message = "Không thể đổi tên cuộc trò chuyện 1-1." });

        conversation.Name = dto.Name;
        await conversationRepo.UpdateAsync(conversation);

        return Ok(new { message = "Cập nhật cuộc trò chuyện thành công." });
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] int userId)
    {
        if (!await conversationRepo.IsMemberAsync(id, CurrentUserId))
            return Forbid();

        var conversation = await conversationRepo.GetByIdAsync(id, false);
        if (conversation == null)
            return NotFound();

        if (!conversation.IsGroup)
            return BadRequest(new { message = "Không thể thêm thành viên vào cuộc trò chuyện 1-1." });

        await conversationRepo.AddMemberAsync(id, userId);
        return Ok(new { message = "Đã thêm thành viên." });
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        if (!await conversationRepo.IsMemberAsync(id, CurrentUserId))
            return Forbid();

        // CHỈ cho phép user tự rời nhóm (Rút lui), không được phép đá người khác
        if (CurrentUserId != userId)
            return Forbid("Không có quyền xóa thành viên khác khỏi nhóm.");

        var conversation = await conversationRepo.GetByIdAsync(id, false);
        if (conversation == null)
            return NotFound();

        if (!conversation.IsGroup)
            return BadRequest(new { message = "Không thể xóa thành viên khỏi cuộc trò chuyện 1-1." });

        await conversationRepo.RemoveMemberAsync(id, userId);
        return Ok(new { message = "Đã rời nhóm." });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await conversationRepo.GetUnreadCountAsync(CurrentUserId);
        return Ok(new { unreadCount = count });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        if (!await conversationRepo.IsMemberAsync(id, CurrentUserId))
            return Forbid();

        await conversationRepo.UpdateLastReadAsync(id, CurrentUserId);
        return Ok(new { message = "Đã đánh dấu đã đọc." });
    }

    private async Task<ConversationDto> MapToConversationDto(Conversation conv)
    {
        var messages = await messageRepo.GetConversationMessagesAsync(conv.Id, null, 1);
        var lastMsg = messages.FirstOrDefault();

        var member = await conversationRepo.GetMemberAsync(conv.Id, CurrentUserId);
        var lastRead = member?.LastReadAt ?? member?.JoinedAt ?? conv.CreatedAt;
        var unreadCount = conv.Messages.Count(m => m.CreatedAt > lastRead && m.SenderId != CurrentUserId);

        return new ConversationDto
        {
            Id = conv.Id,
            Name = conv.Name,
            IsGroup = conv.IsGroup,
            Members = conv.Members.Where(m => m.User != null).Select(m => new UserDto
            {
                Id = m.User!.Id,
                Email = m.User.Email,
                FullName = m.User.FullName,
                Nickname = m.User.Nickname,
                Bio = m.User.Bio,
                AvatarUrl = m.User.AvatarUrl,
                Role = m.User.Role.ToString(),
                CreatedAt = m.User.CreatedAt
            }).ToList(),
            LastMessage = lastMsg != null ? new MessageDto
            {
                Id = lastMsg.Id,
                ConversationId = lastMsg.ConversationId,
                Sender = new UserDto
                {
                    Id = lastMsg.Sender?.Id ?? lastMsg.SenderId,
                    Email = lastMsg.Sender?.Email ?? "",
                    FullName = lastMsg.Sender?.FullName ?? "",
                    Nickname = lastMsg.Sender?.Nickname,
                    Bio = lastMsg.Sender?.Bio,
                    AvatarUrl = lastMsg.Sender?.AvatarUrl,
                    Role = lastMsg.Sender?.Role.ToString() ?? "",
                    CreatedAt = lastMsg.Sender?.CreatedAt ?? DateTime.UtcNow
                },
                Content = lastMsg.Content,
                AttachmentUrl = lastMsg.AttachmentUrl,
                CreatedAt = lastMsg.CreatedAt,
                IsDeleted = lastMsg.IsDeleted,
                IsMine = lastMsg.SenderId == CurrentUserId
            } : null,
            UnreadCount = unreadCount,
            CreatedAt = conv.CreatedAt
        };
    }
}
