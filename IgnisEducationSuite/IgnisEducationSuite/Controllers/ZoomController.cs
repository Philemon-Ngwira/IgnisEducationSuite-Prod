using EduSphereDomain.Data;
using EduSphereDomain.MessagingData;
using EDUSphereSharedProject.ChatModels;
using EDUSphereSharedProject.Zoom;
using IgnisEducationSuite.Hubs;
using IgnisEducationSuite.ServerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static IgnisEducationSuite.Client.Pages.Shared.LiveClasses;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZoomController : ControllerBase
    {

        private readonly ZoomService _zoomService;
        private readonly MessagingContext _Chatcontext;
        private readonly PhoenixEdusphereContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        public ZoomController(ZoomService zoomService, MessagingContext context, PhoenixEdusphereContext edusphereContext, IHubContext<ChatHub> hubContext)
        {
            _zoomService = zoomService;
            _Chatcontext = context;
            _context = edusphereContext;
            _hubContext = hubContext;
        }
        [HttpPost("signature")]
        public IActionResult GenerateSignature([FromBody] SignatureRequest request)
        {
            request.SdkKey = _zoomService.GetSdkKey();
            request.SdkSecret = _zoomService.GetSdkSecret();
            var result = _zoomService.GenerateSignature(request.MeetingNumber, request.Role, request.SdkKey, request.SdkSecret);
            return Ok(new SignatureResponse
            {
                signature = result,
                SdkKey = request.SdkKey,
                SdkSecret = request.SdkSecret
            });
        }

        [HttpPost("ShareToChat")]
        public async Task<IActionResult> ShareToChat([FromBody] ShareLiveClassRequest request)
        {
            var groupName = $"Grade {request.Grade} {request.Subject} Live Class ({request.StartTime:MMM dd, HH:mm})";

            // 1. Create group in DB
            var group = new ChatGroup
            {
                GroupID = Guid.NewGuid(),
                GroupName = groupName,
                CreatedDate = DateTime.UtcNow,
            };
            _Chatcontext.ChatGroups.Add(group);
            await _Chatcontext.SaveChangesAsync();

            // 2. Get students
            var students = await _context.Students
                .Where(s => s.AcademicLevel == int.Parse(request.Grade) &&
                            s.StudentClasses.Any(sub => sub.Class.ClassName == request.Subject))
                .Select(s => s.UserID)
                .ToListAsync();

            // 3. Add teacher
            students.Add(request.TeacherId);

            // 4. Add group members
            foreach (var userId in students)
            {
                _Chatcontext.GroupMembers.Add(new GroupMember
                {
                    GroupMemberID = Guid.NewGuid(),
                    GroupID = group.GroupID,
                    GroupName = group.GroupName,
                    UserId = userId,
                    DateAdded = DateTime.UtcNow,
                });
            }
            await _Chatcontext.SaveChangesAsync();

            // 5. Send system message via SignalR
            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                Message = $"📢 Your {request.Subject} live class is scheduled.\nJoin here: {request.MeetingLink}",
                UserId = request.TeacherId,
                GroupName = groupName,
                Timestamp = DateTime.UtcNow
            };
            _Chatcontext.ChatMessages.Add(message);
            await _Chatcontext.SaveChangesAsync();

            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", message);

            return Ok(new { GroupName = groupName });
        }


        [HttpPost("createmeeting")]
        public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingRequest request)
        {


            // Replace "teacher@example.com" with the teacher's Zoom account email
            var teacherEmail = request.TeacherEmail; // TODO: get from your DB / context

            try
            {
                var meeting = await _zoomService.CreateMeetingAsync(teacherEmail, request.Topic);

                return Ok(new ZoomMeetingResponse
                {
                    id = meeting.id,
                    JoinUrl = meeting.JoinUrl,
                    Password = meeting.Password,
                    MeetingNumber = meeting.MeetingNumber
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, $"Zoom API error: {ex.Message}");
            }
        }




        public class CreateMeetingRequest
        {
            public string Topic { get; set; }
            public DateTime StartTime { get; set; }
            public int Duration { get; set; }
            public string Password { get; set; }

            public string TeacherEmail { get; set; }
        }

        public class CreateMeetingResponse
        {
            public string MeetingNumber { get; set; }
            public string JoinUrl { get; set; }
        }
    }
}
