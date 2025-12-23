using EduSphereDomain.Repositories;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using IgnisEducationSuite.ServerServices.SmartTimeTableGenerator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeTableController : ControllerBase
    {
        private readonly ITimetableGenerator _generator;
        private readonly ILogger<TimeTableController> _logger;
        private readonly EduSphereRepository _repository;
        public TimeTableController(
            ITimetableGenerator generator,
            ILogger<TimeTableController> logger,
            EduSphereRepository repository
            )
        {
            _generator = generator;
            _logger = logger;
            _repository = repository;
        }
        [HttpPost("generate")]
        public async Task<ActionResult<GenerateTimetableResponse>> Generate([FromBody] GenerateTimetableRequest request)
        {
            try
            {
                var teacherIds = request.Schedules
                    .Select(s => s.TeacherId)
                    .Distinct()
                    .ToList();

                var teacherConstraints = await _repository.GetTeacherConstraintsAsync(teacherIds);

                var result = _generator.Generate(
                    request.TimeSlots,
                    request.Schedules,
                    request.AdjacencyRules,
                    request.TimeRules,
                    request.Activities,
                    teacherConstraints
                );

                if (!result.Success)
                    return Ok(new GenerateTimetableResponse
                    {
                        Success = false,
                        Error = result.GetErrorMessage()
                    });

                // Just return the flat list directly
                return Ok(new GenerateTimetableResponse
                {
                    Success = true,
                    Slots = result.Slots
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Timetable generation failed");
                return StatusCode(500, new GenerateTimetableResponse
                {
                    Success = false,
                    Error = "Internal generation error"
                });
            }
        }


    }
}
