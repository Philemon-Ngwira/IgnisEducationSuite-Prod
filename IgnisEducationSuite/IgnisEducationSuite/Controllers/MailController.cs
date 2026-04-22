using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.ServerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MailController : ControllerBase
    {
        private readonly EmailService _emailService;

        public MailController(EmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest emailRequest)
        {
            if (string.IsNullOrEmpty(emailRequest.To) || string.IsNullOrEmpty(emailRequest.Subject) || string.IsNullOrEmpty(emailRequest.Body))
            {
                return BadRequest("Invalid email data.");
            }

            try
            {
                await _emailService.SendEmailAsync(emailRequest);
                return Ok("Email sent successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> SendResetEmail([FromBody] EmailRequest emailRequest)
        {
            if (string.IsNullOrEmpty(emailRequest.To) || string.IsNullOrEmpty(emailRequest.Reciepient) || string.IsNullOrEmpty(emailRequest.Password))
            {
                return BadRequest("Invalid email data.");
            }

            try
            {
                await _emailService.SendPasswordResetEmailAsync(emailRequest.To, emailRequest.Reciepient,emailRequest.Password, emailRequest.UserName, emailRequest.StudentID, emailRequest.Role ?? "", emailRequest.SchoolName ?? "");
                return Ok("Email sent successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("SendReportCard")]
        public async Task<IActionResult> SendReportCard([FromBody] ReportCardEmailDTO reportCardEmailDTO)
        {
            if (reportCardEmailDTO != null)
            {
                await _emailService.SendReportCardEmail(reportCardEmailDTO.PdfBytes, reportCardEmailDTO.EmailTo);
                return Ok("Email Sent Succesfully.");
            }
            else
            {
                return BadRequest("Invalid Data");
            }
        }

        
    }
}
