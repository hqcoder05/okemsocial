using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.DTOs;
using okem_social.Models;
using okem_social.Repositories;

namespace okem_social.Controllers.Api;

[ApiController]
[Route("api/conversations/{conversationId}/messages")]
[Authorize]
public class MessagesApiController(
    IMessageRepository messageRepo,
    IConversationRepository conversationRepo) : ControllerBase
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
                Id = m.Sender!.Id,
                Email = m.Sender.Email,
                FullName = m.Sender.FullName,
                Role = m.Sender.Role.ToString(),
                CreatedAt = m.Sender.CreatedAt
            },
            Content = m.Content,
            AttachmentUrl = m.AttachmentUrl,
            CreatedAt = m.CreatedAt,
            IsDeleted = m.IsDeleted,
            IsRead = lastReadAt.HasValue && m.CreatedAt <= lastReadAt.Value,
            ReadAt = lastReadAt.HasValue && m.CreatedAt <= lastReadAt.Value ? lastReadAt : null,
            // QUAN TRỌNG: Thêm flag để frontend dễ kiểm tra
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

        return Ok(new MessageDto
        {
            Id = created.Id,
            ConversationId = created.ConversationId,
            Sender = new UserDto
            {
                Id = created.Sender!.Id,
                Email = created.Sender.Email,
                FullName = created.Sender.FullName,
                Role = created.Sender.Role.ToString(),
                CreatedAt = created.Sender.CreatedAt
            },
            Content = created.Content,
            AttachmentUrl = created.AttachmentUrl,
            CreatedAt = created.CreatedAt,
            IsDeleted = created.IsDeleted,
            IsMine = true // Tin nhắn mới luôn là của mình
        });
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