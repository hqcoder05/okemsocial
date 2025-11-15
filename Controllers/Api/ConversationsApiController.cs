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
            return BadRequest(new { message = "At least one member required" });

        // Add current user to members
        if (!dto.MemberIds.Contains(CurrentUserId))
            dto.MemberIds.Add(CurrentUserId);

        // Validate all members exist
        foreach (var memberId in dto.MemberIds)
        {
            var user = await userRepo.GetByIdAsync(memberId);
            if (user == null)
                return BadRequest(new { message = $"User {memberId} not found" });
        }

        var conversation = new Conversation
        {
            Name = dto.Name,
            IsGroup = dto.MemberIds.Count > 2
        };

        var created = await conversationRepo.CreateAsync(conversation, dto.MemberIds);
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
            return BadRequest(new { message = "Cannot rename 1-1 conversation" });

        conversation.Name = dto.Name;
        await conversationRepo.UpdateAsync(conversation);

        return Ok(new { message = "Conversation updated successfully" });
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
            return BadRequest(new { message = "Cannot add members to 1-1 conversation" });

        await conversationRepo.AddMemberAsync(id, userId);
        return Ok(new { message = "Member added successfully" });
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        if (!await conversationRepo.IsMemberAsync(id, CurrentUserId))
            return Forbid();

        var conversation = await conversationRepo.GetByIdAsync(id, false);
        if (conversation == null)
            return NotFound();

        if (!conversation.IsGroup)
            return BadRequest(new { message = "Cannot remove members from 1-1 conversation" });

        await conversationRepo.RemoveMemberAsync(id, userId);
        return Ok(new { message = "Member removed successfully" });
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
        return Ok(new { message = "Marked as read" });
    }

    private async Task<ConversationDto> MapToConversationDto(Conversation conv)
    {
        var messages = await messageRepo.GetConversationMessagesAsync(conv.Id, null, 1);
        
        return new ConversationDto
        {
            Id = conv.Id,
            Name = conv.Name,
            IsGroup = conv.IsGroup,
            Members = conv.Members.Select(m => new UserDto
            {
                Id = m.User!.Id,
                Email = m.User.Email,
                FullName = m.User.FullName,
                Role = m.User.Role.ToString(),
                CreatedAt = m.User.CreatedAt
            }).ToList(),
            LastMessage = messages.FirstOrDefault() != null ? new MessageDto
            {
                Id = messages.First().Id,
                ConversationId = messages.First().ConversationId,
                Sender = new UserDto
                {
                    Id = messages.First().Sender!.Id,
                    Email = messages.First().Sender.Email,
                    FullName = messages.First().Sender.FullName,
                    Role = messages.First().Sender.Role.ToString(),
                    CreatedAt = messages.First().Sender.CreatedAt
                },
                Content = messages.First().Content,
                AttachmentUrl = messages.First().AttachmentUrl,
                CreatedAt = messages.First().CreatedAt,
                IsDeleted = messages.First().IsDeleted
            } : null,
            UnreadCount = 0, // TODO: Calculate properly
            CreatedAt = conv.CreatedAt
        };
    }
}
