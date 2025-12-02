using EDUSphereSharedProject.Zoom;
using IgnisEducationSuite.ServerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using static IgnisEducationSuite.Client.Pages.Shared.LiveClasses;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZoomController : ControllerBase
    {

        private readonly ZoomService _zoomService;

        public ZoomController(ZoomService zoomService)
        {
            _zoomService = zoomService;
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
        

        //[HttpPost("signature")]
        //public IActionResult GetSignature([FromBody] SignatureRequest req)
        //{
        //    var ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds - 30000;

        //    var message = $"{req.MeetingNumber}{req.Role}{ts}";
        //    var encoding = new UTF8Encoding();
        //    var keyBytes = encoding.GetBytes(req.SdkSecret);
        //    var messageBytes = encoding.GetBytes(message);

        //    using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
        //    var hash = hmac.ComputeHash(messageBytes);
        //    var hashString = Convert.ToBase64String(hash);

        //    var token = $"{req.SdkKey}.{req.MeetingNumber}.{ts}.{req.Role}.{hashString}";
        //    var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

        //    return Ok(new { signature });
        //}

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
