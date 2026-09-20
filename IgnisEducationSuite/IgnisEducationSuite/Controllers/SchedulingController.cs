using System.Security.Claims;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using IgnisEducationSuite.ServerServices.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    /// <summary>
    /// Replaces the old TimeTableController. The school is resolved from the signed-in user's
    /// identity cookie rather than taken from the request, so these endpoints cannot be pointed at
    /// another school's data.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SchedulingController : ControllerBase
    {
        private readonly SchedulingManagementOrchestrator _orchestrator;
        private readonly ILogger<SchedulingController> _logger;

        public SchedulingController(SchedulingManagementOrchestrator orchestrator, ILogger<SchedulingController> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ---------- Reference data ----------

        [HttpGet("days")]
        public async Task<IActionResult> ListDays()
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.ListDaysAsync(userId));
        }

        // ---------- Timetable Activities ----------

        [HttpGet("activities")]
        public async Task<IActionResult> ListActivities()
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.ListActivitiesAsync(userId));
        }

        [HttpPost("activities")]
        public async Task<IActionResult> CreateActivity([FromBody] ActivityRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.CreateActivityAsync(userId, request));
        }

        [HttpPut("activities/{id:guid}")]
        public async Task<IActionResult> UpdateActivity(Guid id, [FromBody] ActivityRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.UpdateActivityAsync(userId, id, request));
        }

        [HttpDelete("activities/{id:guid}")]
        public async Task<IActionResult> DeleteActivity(Guid id)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.DeleteActivityAsync(userId, id));
        }

        // ---------- Time Slots ----------

        [HttpGet("time-slots")]
        public async Task<IActionResult> ListTimeSlots()
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.ListTimeSlotsAsync(userId));
        }

        [HttpPost("time-slots")]
        public async Task<IActionResult> CreateTimeSlot([FromBody] TimeSlotRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.CreateTimeSlotAsync(userId, request));
        }

        [HttpPut("time-slots/{id:guid}")]
        public async Task<IActionResult> UpdateTimeSlot(Guid id, [FromBody] TimeSlotRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.UpdateTimeSlotAsync(userId, id, request));
        }

        [HttpDelete("time-slots/{id:guid}")]
        public async Task<IActionResult> DeleteTimeSlot(Guid id)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.DeleteTimeSlotAsync(userId, id));
        }

        // ---------- Subject scheduling policy ----------

        [HttpGet("policy")]
        public async Task<IActionResult> ListPolicy()
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.ListPolicyAsync(userId));
        }

        [HttpPut("policy/{classId:guid}")]
        public async Task<IActionResult> UpsertPolicy(Guid classId, [FromBody] SubjectScheduleConfigRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.UpsertPolicyAsync(userId, classId, request));
        }

        [HttpPut("policy")]
        public async Task<IActionResult> BulkUpsertPolicy([FromBody] BulkSubjectScheduleConfigRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();

            try
            {
                return Ok(await _orchestrator.UpsertPolicyBulkAsync(userId, request));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk subject policy save failed for {Count} subject(s)", request.Items.Count);

                return Ok(new BulkSubjectScheduleConfigResult
                {
                    Succeeded = false,
                    Errors = { "Save failed unexpectedly. No changes were applied." },
                });
            }
        }

        // ---------- Adjacency ----------

        [HttpGet("adjacency")]
        public async Task<IActionResult> ListAdjacency()
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.ListAdjacencyAsync(userId));
        }

        [HttpPost("adjacency")]
        public async Task<IActionResult> AddAdjacency([FromBody] AddAdjacencyRuleRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.AddAdjacencyRuleAsync(userId, request.ClassId, request.CannotFollowClassId));
        }

        [HttpDelete("adjacency/{id:guid}")]
        public async Task<IActionResult> RemoveAdjacency(Guid id)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.RemoveAdjacencyRuleAsync(userId, id));
        }

        // ---------- Generation ----------

        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] GenerateScheduleRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();

            try
            {
                return Ok(await _orchestrator.PreviewGenerationAsync(userId, request));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Timetable generation failed for level {Level} section {Section}",
                    request.AcademicLevel, request.AcademicLevelSection);

                return Ok(new GenerateScheduleResult
                {
                    Success = false,
                    Errors = { "Generation failed unexpectedly. The timetable was not changed." },
                });
            }
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateScheduleRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.ValidateEditedScheduleAsync(userId, request));
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] SaveGeneratedScheduleRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();

            try
            {
                return Ok(await _orchestrator.SaveScheduleAsync(userId, request));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Timetable save failed for level {Level} section {Section}",
                    request.AcademicLevel, request.AcademicLevelSection);

                return Ok(new SaveGeneratedScheduleResult
                {
                    Succeeded = false,
                    Errors = { "Save failed unexpectedly. The previous timetable is unchanged." },
                });
            }
        }

        [HttpGet("current/{academicLevel:int}/{academicLevelSection:guid}")]
        public async Task<IActionResult> GetCurrent(int academicLevel, Guid academicLevelSection)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.GetCurrentScheduleAsync(userId, academicLevel, academicLevelSection));
        }

        // ---------- Overrides ----------

        [HttpGet("overrides")]
        public async Task<IActionResult> ListOverrides()
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.ListOverridesAsync(userId));
        }

        [HttpGet("overrides/{id:guid}")]
        public async Task<IActionResult> GetOverride(Guid id)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();

            var detail = await _orchestrator.GetOverrideAsync(userId, id);
            return detail is null ? NotFound() : Ok(detail);
        }

        [HttpPost("overrides")]
        public async Task<IActionResult> CreateOverride([FromBody] OverrideRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.CreateOverrideAsync(userId, request));
        }

        [HttpPut("overrides/{id:guid}")]
        public async Task<IActionResult> UpdateOverride(Guid id, [FromBody] OverrideRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.UpdateOverrideAsync(userId, id, request));
        }

        [HttpDelete("overrides/{id:guid}")]
        public async Task<IActionResult> DeleteOverride(Guid id)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _orchestrator.DeleteOverrideAsync(userId, id));
        }
    }

}
