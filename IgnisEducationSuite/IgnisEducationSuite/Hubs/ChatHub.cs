using EduSphereDomain.MessagingData;
using EDUSphereSharedProject.ChatModels;
using EDUSphereSharedProject.IdentitySharedModels;
using EDUSphereSharedProject.UniversalModels.Chat;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IgnisEducationSuite.Hubs
{
    public class ChatHub : Hub
    {
        private readonly MessagingContext _context;

        public ChatHub(MessagingContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        /// <summary>
        /// The user id SignalR has resolved for this connection, or null when the connection is not
        /// authenticated.
        ///
        /// Clients.User(id) routes on exactly this value, so if it is null or differs from the
        /// caller's own account id, direct messages addressed to them can never be delivered — they
        /// are saved and only appear after a refresh. The client checks this on connect and says so
        /// plainly, rather than leaving it to be diagnosed from the symptom.
        /// </summary>
        public Task<string?> WhoAmI() => Task.FromResult(Context.UserIdentifier);

        /// <summary>
        /// Persists a direct message and pushes it to the recipient.
        ///
        /// Only the recipient is pushed to. The sender adds their own copy once this method returns,
        /// which it only does after the save succeeds — so the sender never depends on SignalR's
        /// user-id mapping resolving for their own account.
        /// </summary>
        public async Task SendMessageToUser(string userId, Message message)
        {
            var stored = new ChatMessage
            {
                Id = Guid.NewGuid(),
                UserId = message.UserId,
                ReciepientId = userId,
                Message = message.Content,
                Timestamp = DateTime.Now,
            };

            _context.ChatMessages.Add(stored);
            await _context.SaveChangesAsync();

            await Clients.User(userId).SendAsync("ReceiveMessage", await ToPayloadAsync(stored));
        }

        public async Task SendMessageToGroup(string groupName, Message message)
        {
            var stored = new ChatMessage
            {
                Id = Guid.NewGuid(),
                GroupName = groupName,
                GroupIdentifier = string.IsNullOrWhiteSpace(message.GroupIdentifier) ? groupName : message.GroupIdentifier,
                Message = message.Content,
                Timestamp = DateTime.Now,
                UserId = message.UserId,
            };

            _context.ChatMessages.Add(stored);
            await _context.SaveChangesAsync();

            // OthersInGroup, not Group: the sender is a member of the group and adds their own copy
            // once this call returns, so including them here would show the message twice.
            await Clients.OthersInGroup(groupName).SendAsync("ReceiveMessage", await ToPayloadAsync(stored));
        }

        public async Task AddToGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task RemoveFromGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Maps the stored entity onto the DTO the client actually listens for, resolving the
        /// sender's display name so a group chat can show who spoke without the client holding
        /// every user in the school.
        ///
        /// The entity and the DTO do NOT have matching property names — ChatMessage.Message vs
        /// Content, and ChatMessage.ReciepientId (misspelt) vs RecipientId. Sending the entity
        /// straight down the wire meant the client deserialised a message whose content and
        /// recipient were both empty, so it never matched the open conversation and only appeared
        /// after a refresh reloaded it from the database.
        /// </summary>
        private async Task<ChatMessageDto> ToPayloadAsync(ChatMessage stored)
        {
            string? senderName = null;

            if (!string.IsNullOrWhiteSpace(stored.UserId))
            {
                var sender = await _context.AspNetUsers
                    .Where(u => u.Id == stored.UserId)
                    .Select(u => new { u.FirstName, u.LastName, u.UserName })
                    .FirstOrDefaultAsync();

                if (sender is not null)
                {
                    var fullName = $"{sender.FirstName} {sender.LastName}".Trim();
                    senderName = string.IsNullOrWhiteSpace(fullName) ? sender.UserName : fullName;
                }
            }

            return new ChatMessageDto
            {
                Id = stored.Id,
                Content = stored.Message ?? "",
                UserId = stored.UserId ?? "",
                RecipientId = stored.ReciepientId ?? "",
                GroupName = stored.GroupName ?? "",
                GroupIdentifier = stored.GroupIdentifier ?? "",
                Timestamp = stored.Timestamp,
                SenderName = senderName,
                Attachment = string.IsNullOrWhiteSpace(stored.AttachmentPath) ? null : new ChatAttachmentDto
                {
                    Name = stored.AttachmentName,
                    ContentType = stored.AttachmentContentType,
                    SizeBytes = stored.AttachmentSizeBytes,
                },
            };
        }
    }
}
