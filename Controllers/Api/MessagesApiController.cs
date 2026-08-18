using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using okem_social.DTOs;
using okem_social.Hubs;
using okem_social.Models;
using okem_social.Repositories;

namespace okem_social.Controllers.Api;

[ApiController]
[Route("api/conversations/{conversationId}/messages")]
[Authorize]
public class MessagesApiController(
    IMessageRepository messageRepo,
    IConversationRepository conversationRepo,
    IHubContext<ChatHub> chatHub) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(
        int conversationId,
        [FromQuery] DateTime? before = null,
        [FromQuery] int take = 50)
    {
        if (!await conversationRepo.IsMemberAsync(conversationId, CurrentUserId))
            return Forbid();

        var messages = await messageRepo.GetConversationMessagesAsync(conversationId, before, take);

        var member = await conversationRepo.GetMemberAsync(conversationId, CurrentUserId);
        var lastReadAt = member?.LastReadAt;

        return Ok(messages.Select(m => new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Sender = new UserDto
            {
                Id = m.Sender?.Id ?? m.SenderId,
                Email = m.Sender?.Email ?? "",
                FullName = m.Sender?.FullName ?? "",
                Nickname = m.Sender?.Nickname,
                Bio = m.Sender?.Bio,
                AvatarUrl = m.Sender?.AvatarUrl,
                Role = m.Sender?.Role.ToString() ?? "",
                CreatedAt = m.Sender?.CreatedAt ?? DateTime.UtcNow
            },
            Content = m.Content,
            AttachmentUrl = m.AttachmentUrl,
            CreatedAt = m.CreatedAt,
            IsDeleted = m.IsDeleted,
            IsRead = lastReadAt.HasValue && m.CreatedAt <= lastReadAt.Value,
            ReadAt = lastReadAt.HasValue && m.CreatedAt <= lastReadAt.Value ? lastReadAt : null,
            IsMine = m.SenderId == CurrentUserId
        }).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<MessageDto>> SendMessage(int conversationId, SendMessageDto dto)
    {
        if (!await conversationRepo.IsMemberAsync(conversationId, CurrentUserId))
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Content) && string.IsNullOrWhiteSpace(dto.AttachmentUrl))
            return BadRequest(new { message = "Message must have content or attachment" });

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = CurrentUserId,
            Content = dto.Content,
            AttachmentUrl = dto.AttachmentUrl
        };

        var created = await messageRepo.CreateAsync(message);

        var resultDto = new MessageDto
        {
            Id = created.Id,
            ConversationId = created.ConversationId,
            Sender = new UserDto
            {
                Id = created.Sender?.Id ?? CurrentUserId,
                Email = created.Sender?.Email ?? "",
                FullName = created.Sender?.FullName ?? "",
                Nickname = created.Sender?.Nickname,
                Bio = created.Sender?.Bio,
                AvatarUrl = created.Sender?.AvatarUrl,
                Role = created.Sender?.Role.ToString() ?? "",
                CreatedAt = created.Sender?.CreatedAt ?? DateTime.UtcNow
            },
            Content = created.Content,
            AttachmentUrl = created.AttachmentUrl,
            CreatedAt = created.CreatedAt,
            IsDeleted = created.IsDeleted,
            IsMine = true
        };

        // Realtime broadcast to SignalR chat group
        await chatHub.Clients.Group($"conversation_{conversationId}")
            .SendAsync("ReceiveMessage", resultDto);

        return Ok(resultDto);
    }

    [HttpPost("{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(int conversationId, int messageId)
    {
        if (!await conversationRepo.IsMemberAsync(conversationId, CurrentUserId))
            return Forbid();

        var message = await messageRepo.GetByIdAsync(messageId);
        if (message == null || message.ConversationId != conversationId)
            return NotFound();

        await conversationRepo.UpdateLastReadAsync(conversationId, CurrentUserId);

        return Ok();
    }
}