using EduSphereDomain.ChatData;
using EduSphereDomain.MessagingData;
using EDUSphereSharedProject.ChatModels;
using EDUSphereSharedProject.IdentitySharedModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagingController : ControllerBase
    {
        private readonly MessagingContext _context;
        public MessagingController(MessagingContext context)
        {
            _context = context;
        }

        [HttpGet("getchats/{userId}")]
        public async Task<ActionResult<List<Chat>>> GetChats(string userId)
        {
            try
            {
                // Fetch individual user chats
                var userChats = await _context.ChatMessages
                    .Where(m => m.UserId == userId || m.ReciepientId == userId)
                    .GroupBy(m => m.UserId == userId ? m.ReciepientId : m.UserId) // Group by the counterpart's ID
                    .Select(g => new Chat
                    {
                        Name = _context.AspNetUsers
                            .Where(u => u.Id == g.Key) // Get the counterpart's name
                            .Select(u => u.UserName)
                            .FirstOrDefault(),
                        IsGroup = false,
                        ProfilePic = _context.AspNetUsers
                            .Where(u => u.Id == g.Key) // Get the counterpart's profile pic
                            .Select(u => u.ProfilePic)
                            .FirstOrDefault(),
                        LastMessage = g.OrderByDescending(m => m.Timestamp)
                            .Select(m => m.Message)
                            .FirstOrDefault() ?? string.Empty, // Default to empty if no messages
                        LastMessageTimestamp = g.OrderByDescending(m => m.Timestamp.Value)
                            .Select(m => m.Timestamp.Value)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                // Fetch group chats
                var groupChats = await _context.ChatMessages
                    .Where(m => m.GroupName != null && (m.UserId == userId || m.ReciepientId == userId))
                    .GroupBy(m => m.GroupName)
                    .Select(g => new Chat
                    {
                        Name = g.Key, // Group name
                        IsGroup = true,
                        ProfilePic = new byte[0], // Assuming a default profile pic for groups
                        LastMessage = g.OrderByDescending(m => m.Timestamp)
                            .Select(m => m.Message)
                            .FirstOrDefault() ?? string.Empty, // Default to empty if no messages
                        LastMessageTimestamp = g.OrderByDescending(m => m.Timestamp.Value)
                            .Select(m => m.Timestamp.Value)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                // Combine user and group chats
                var allChats = userChats.Concat(groupChats).ToList();

                // Return an empty list if no chats exist
                if (allChats == null || !allChats.Any())
                {
                    return new List<Chat>();
                }

                return allChats;
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                var _ = ex.Message;
                throw;
            }
        }


        [HttpGet("getmessages/{chatName}")]
        public async Task<ActionResult<List<Message>>> GetMessages(string chatName)
        {
            var messages = await _context.ChatMessages.Where(m => m.UserId.ToString() == chatName || m.ReciepientId.ToString() == chatName || m.GroupName == chatName)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
            var messagestoSend = new List<Message>();
            foreach (var message in messages)
            {
                Message newMessage = new Message
                {
                    Content = message.Message,
                    UserId = message.UserId,
                    RecipientId = message.ReciepientId,
                    GroupName = message.GroupName,
                    Timestamp = message.Timestamp,
                };
                messagestoSend.Add(newMessage);
            }
            return messagestoSend;
        }


    }
}
