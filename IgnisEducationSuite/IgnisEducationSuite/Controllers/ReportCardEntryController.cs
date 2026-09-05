using System.Security.Claims;
using EDUSphereSharedProject.UniversalModels.ReportCards;
using IgnisEducationSuite.ServerServices.ReportCards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    /// <summary>
    /// Class-based report card entry. The school and the caller's entitlements are resolved from the
    /// identity cookie — no endpoint accepts a school id or teacher id from the client.
    ///
    /// The optional <c>role</c> query value mirrors the "Current Role" switcher for users who hold
    /// more than one role. It can only ever narrow what the caller reaches: the orchestrator honours
    /// it after confirming they actually hold that role, and ignores it otherwise.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportCardEntryController : ControllerBase
    {
        private readonly ReportCardEntryOrchestrator _orchestrator;
        private readonly ILogger<ReportCardEntryController> _logger;

        public ReportCardEntryController(
            ReportCardEntryOrchestrator orchestrator,
            ILogger<ReportCardEntryController> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>
        /// The classes the caller may enter marks for, with progress per class. Defaults to the
        /// caller's own classes; <paramref name="all"/> is honoured only for school-wide roles.
        /// </summary>
        [HttpGet("classes/{reportCardType}")]
        public async Task<IActionResult> ListClasses(string reportCardType, [FromQuery] bool all = false, [FromQuery] string? role = null)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.ListClassesAsync(userId, reportCardType, all, role));
        }

        /// <summary>Every student enrolled in the class, whether or not they have a report card.</summary>
        [HttpGet("sheet/{classId:guid}/{reportCardType}")]
        public async Task<IActionResult> GetSheet(Guid classId, string reportCardType, [FromQuery] string? role = null)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.GetClassSheetAsync(userId, classId, reportCardType, role));
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] SaveClassResultsRequest request, [FromQuery] string? role = null)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();

            try
            {
                return Ok(await _orchestrator.SaveClassResultsAsync(userId, request, role));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Class result save failed for class {ClassId} ({ReportCardType})",
                    request.ClassId, request.ReportCardType);

                return Ok(new SaveClassResultsResult
                {
                    Succeeded = false,
                    Errors = { "Save failed unexpectedly. No marks were changed." },
                });
            }
        }
    }
}
