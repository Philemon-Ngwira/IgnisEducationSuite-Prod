using System.Security.Claims;
using EDUSphereSharedProject.UniversalModels.ParentLinking;
using IgnisEducationSuite.ServerServices.ParentLinking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    /// <summary>
    /// Students with no parent on record, and attaching them to one. School is resolved from the
    /// identity cookie, so no endpoint here accepts a school id from the client.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ParentLinkingController : ControllerBase
    {
        private readonly ParentLinkingService _service;
        private readonly StudentEnrolmentService _enrolment;
        private readonly ILogger<ParentLinkingController> _logger;

        public ParentLinkingController(
            ParentLinkingService service,
            StudentEnrolmentService enrolment,
            ILogger<ParentLinkingController> logger)
        {
            _service = service;
            _enrolment = enrolment;
            _logger = logger;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>Dashboard headline count. Never throws outward — a failed tile must not take the
        /// whole dashboard down with it.</summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            if (CurrentUserId is not { } userId) return Unauthorized();

            try
            {
                return Ok(await _service.GetSummaryAsync(userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load unparented student summary");
                return Ok(new UnparentedSummaryDto());
            }
        }

        [HttpGet("unparented")]
        public async Task<IActionResult> ListUnparented(
            [FromQuery] int? academicLevel = null,
            [FromQuery] string? gradeSection = null,
            [FromQuery] string? search = null)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _service.ListUnparentedAsync(userId, academicLevel, gradeSection, search));
        }

        [HttpGet("parents")]
        public async Task<IActionResult> ListParents([FromQuery] string? search = null)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _service.ListParentsAsync(userId, search));
        }

        [HttpPost("link")]
        public async Task<IActionResult> Link([FromBody] LinkStudentsToParentRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();

            try
            {
                return Ok(await _service.LinkStudentsAsync(userId, request));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to link {Count} student(s) to parent {ParentId}",
                    request.StudentIds.Count, request.ParentId);

                return Ok(new LinkStudentsToParentResult
                {
                    Succeeded = false,
                    Errors = { "Linking failed unexpectedly. No students were changed." },
                });
            }
        }

        [HttpPost("unlink")]
        public async Task<IActionResult> Unlink([FromBody] UnlinkStudentRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _service.UnlinkStudentAsync(userId, request.StudentId));
        }

        /// <summary>
        /// Matches students to their classes via SyncStudentsToClasses and reports what the named
        /// student ended up enrolled in. Called right after creating a student individually — the
        /// bulk path's equivalent step that individual creation never had.
        /// </summary>
        [HttpPost("sync-classes")]
        public async Task<IActionResult> SyncClasses([FromQuery] Guid? studentId = null)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();

            try
            {
                return Ok(await _enrolment.SyncAndReportAsync(userId, studentId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Class sync failed for student {StudentId}", studentId);

                return Ok(new SyncStudentClassesResult
                {
                    Succeeded = false,
                    Errors = { "Class matching failed. The student was saved but may not be enrolled in any class." },
                });
            }
        }

        /// <summary>Group tokens that class matching recognises, derived from the school's own class names.</summary>
        [HttpGet("group-options")]
        public async Task<IActionResult> GroupOptions()
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _enrolment.ListGroupOptionsAsync(userId));
        }
    }
}
