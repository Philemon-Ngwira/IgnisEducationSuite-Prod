using EduSphereDomain.MessagingData;
using EDUSphereSharedProject.ChatModels;
using EDUSphereSharedProject.IdentitySharedModels;
using Microsoft.AspNetCore.SignalR;

namespace IgnisEducationSuite.Hubs
{
    public class ChatHub: Hub
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

        public async Task SendMessageToUser(string userId, Message message)
        {
            try
            {
                var newMessage = new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    UserId = message.UserId,
                    ReciepientId = userId,
                    Message = message.Content,
                    Timestamp = DateTime.Now
                };
                _context.ChatMessages.Add(newMessage);
                await _context.SaveChangesAsync();
                await Clients.User(userId).SendAsync("ReceiveMessage", newMessage);
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                throw;
            }

        }
        public async Task SendMessageToGroup(string groupName, Message message)
        {
            var newMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                GroupName = groupName,
                Message = message.Content,
                Timestamp = DateTime.Now,
                UserId = message.UserId,
                
            };
            _context.ChatMessages.Add(newMessage);
            await _context.SaveChangesAsync();
            await Clients.Group(groupName).SendAsync("ReceiveMessage", newMessage);
        }
        public async Task AddToGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
        public async Task RemoveFromGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
