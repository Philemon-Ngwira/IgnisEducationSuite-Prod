using EduSphereDomain.ChatData;
using EduSphereDomain.Data;
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
        private readonly PhoenixEdusphereContext _mainContext;
        public MessagingController(MessagingContext context, PhoenixEdusphereContext phoenix)
        {
            _context = context;
            _mainContext = phoenix;
        }

        [HttpGet("getchats/{userId}")]
        public async Task<ActionResult<List<Chat>>> GetChats(string userId)
        {
            try
            {
                // ===============================
                // 1. Individual (1-to-1) chats
                // ===============================
                var userChats = await _context.ChatMessages
                    .Where(m => m.UserId == userId || m.ReciepientId == userId)
                    .GroupBy(m => m.UserId == userId ? m.ReciepientId : m.UserId)
                    .Select(g => new Chat
                    {
                        Reciepientid = g.OrderByDescending(m => m.Timestamp)
                                         .Select(m => m.UserId == userId ? m.ReciepientId : m.UserId)
                                         .FirstOrDefault(),

                        Name = _context.AspNetUsers
                                       .Where(u => u.Id == g.Key)
                                       .Select(u => u.UserName)
                                       .FirstOrDefault(),

                        IsGroup = false,

                        ProfilePic = _context.AspNetUsers
                                             .Where(u => u.Id == g.Key)
                                             .Select(u => u.ProfilePic)
                                             .FirstOrDefault(),

                        LastMessage = g.OrderByDescending(m => m.Timestamp)
                                       .Select(m => m.Message)
                                       .FirstOrDefault() ?? string.Empty,

                        LastMessageTimestamp = g.OrderByDescending(m => m.Timestamp)
                                                .Select(m => m.Timestamp.Value)
                                                .FirstOrDefault()
                    })
                    .ToListAsync();

                // ===============================
                // 2. Resolve student group identity (if student)
                // ===============================
                string? userGroupIdentifier = null;

                var student = await _mainContext.Students
                    .Where(s => s.UserID == userId)
                    .Select(s => new { s.LevelName, s.GradeSection })
                    .FirstOrDefaultAsync();

                if (student != null)
                {
                    userGroupIdentifier = $"{student.LevelName}_{student.GradeSection ?? ""}";
                }

                // ===============================
                // 3. Group chats (students + creators)
                // ===============================
                var groupChats = await _context.ChatMessages
                    .Where(m =>
                        !string.IsNullOrEmpty(m.GroupName) &&
                        (
                            // Student-based membership
                            (userGroupIdentifier != null && m.GroupIdentifier == userGroupIdentifier)
                            // Creator-based access (teachers/admins)
                            || m.UserId == userId
                        )
                    )
                    .GroupBy(m => m.GroupName)
                    .Select(g => new Chat
                    {
                        Reciepientid = null,
                        Name = g.Key,
                        IsGroup = true,
                        ProfilePic = Array.Empty<byte>(),

                        LastMessage = g.OrderByDescending(m => m.Timestamp)
                                       .Select(m => m.Message)
                                       .FirstOrDefault() ?? string.Empty,

                        LastMessageTimestamp = g.OrderByDescending(m => m.Timestamp)
                                                .Select(m => m.Timestamp.Value)
                                                .FirstOrDefault()
                    })
                    .ToListAsync();

                // ===============================
                // 4. Merge & return
                // ===============================
                var allChats = userChats
                    .Concat(groupChats)
                    .OrderByDescending(c => c.LastMessageTimestamp)
                    .ToList();

                return allChats;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
