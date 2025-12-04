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
                        Reciepientid = g.OrderByDescending(m => m.Timestamp.Value)
                        .Select(m => m.ReciepientId.ToString())
                        .FirstOrDefault(),
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
             .Where(m => !string.IsNullOrEmpty(m.GroupName))
             .GroupBy(m => m.GroupName)
             .Select(g => new Chat
             {
                 Reciepientid = null, // No single recipient for groups

                 Name = g.Key, // Group name
                 IsGroup = true,
                 ProfilePic = new byte[0], // Default group profile

                 LastMessage = g.OrderByDescending(m => m.Timestamp)
                                .Select(m => m.Message)
                                .FirstOrDefault() ?? string.Empty,

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


        [HttpGet("getmessages/{userId}/{chatIdentifier}")]
        public async Task<ActionResult<List<Message>>> GetMessages(string userId, string chatIdentifier)
        {
            List<ChatMessage> messages;

            // Check if this is a group chat (by convention, group names are strings that are not GUIDs)
            var isGroupChat = Guid.TryParse(chatIdentifier, out Guid recipientGuid) == false;

            if (isGroupChat)
            {
                // Fetch all messages for the group
                messages = await _context.ChatMessages
                    .Where(m => m.GroupName == chatIdentifier)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();
            }
            else
            {
                // 1-on-1 chat: fetch messages between two users
                messages = await _context.ChatMessages
                    .Where(m => (m.UserId.ToString() == userId && m.ReciepientId.ToString() == chatIdentifier)
                             || (m.UserId.ToString() == chatIdentifier && m.ReciepientId.ToString() == userId))
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();
            }

            // Map to DTO
            var messagesToSend = messages.Select(m => new Message
            {
                Content = m.Message,
                UserId = m.UserId,
                RecipientId = m.ReciepientId,
                GroupName = m.GroupName,
                Timestamp = m.Timestamp
            }).ToList();

            return messagesToSend;
        }


    }
}
