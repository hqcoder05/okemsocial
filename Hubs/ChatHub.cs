using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using okem_social.Repositories;
using okem_social.Models;

namespace okem_social.Hubs;

[Authorize]
public class ChatHub(IMessageRepository messageRepo, IConversationRepository conversationRepo) : Hub
{
    private int CurrentUserId => int.Parse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    public override async Task OnConnectedAsync()
    {
        // Join all user's conversations
        var conversations = await conversationRepo.GetUserConversationsAsync(CurrentUserId);
        foreach (var conv in conversations)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conv.Id}");
        }

        await base.OnConnectedAsync();
    }

    public async Task SendMessage(int conversationId, string? content, string? attachmentUrl)
    {
        // Check membership
        if (!await conversationRepo.IsMemberAsync(conversationId, CurrentUserId))
        {
            throw new HubException("Not a member of this conversation");
        }

        // Build message
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = CurrentUserId,
            Content = content,
            AttachmentUrl = attachmentUrl
        };

        var savedMessage = await messageRepo.CreateAsync(message);

        // Base payload for all receivers
        var basePayload = new
        {
            id = savedMessage.Id,
            conversationId = savedMessage.ConversationId,
            sender = new
            {
                id = savedMessage.Sender!.Id,
                email = savedMessage.Sender.Email,
                fullName = savedMessage.Sender.FullName
            },
            content = savedMessage.Content,
            attachmentUrl = savedMessage.AttachmentUrl,
            createdAt = savedMessage.CreatedAt,
            isDeleted = savedMessage.IsDeleted
        };

        // ================================
        // 🔥 GỬI RIÊNG CHO NGƯỜI GỬI (Caller)
        // ================================
        await Clients.Caller.SendAsync("ReceiveMessage", new
        {
            basePayload.id,
            basePayload.conversationId,
            basePayload.sender,
            basePayload.content,
            basePayload.attachmentUrl,
            basePayload.createdAt,
            basePayload.isDeleted,
            isMine = true // ⭐ BÊN PHẢI
        });

        // =================================
        // 🔥 GỬI CHO CÁC THIẾT BỊ KHÁC & NGƯỜI KHÁC (OthersInGroup)
        // =================================
        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("ReceiveMessage", new
            {
                basePayload.id,
                basePayload.conversationId,
                basePayload.sender,
                basePayload.content,
                basePayload.attachmentUrl,
                basePayload.createdAt,
                basePayload.isDeleted,
                isMine = false // ⭐ BÊN TRÁI
            });
    }

    public async Task Typing(int conversationId)
    {
        if (!await conversationRepo.IsMemberAsync(conversationId, CurrentUserId))
            return;

        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserTyping", new
            {
                UserId = CurrentUserId,
                ConversationId = conversationId
            });
    }

    public async Task Seen(int conversationId, int messageId)
    {
        if (!await conversationRepo.IsMemberAsync(conversationId, CurrentUserId))
            return;

        await conversationRepo.UpdateLastReadAsync(conversationId, CurrentUserId);

        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("MessageSeen", new
            {
                UserId = CurrentUserId,
                ConversationId = conversationId,
                MessageId = messageId
            });
    }

    public async Task JoinConversation(int conversationId)
    {
        if (await conversationRepo.IsMemberAsync(conversationId, CurrentUserId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }
    }
}
