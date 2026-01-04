using EduSphereDomain.Repositories;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using IgnisEducationSuite.ServerServices.SmartTimeTableGenerator;
using IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mono.TextTemplating;
using Org.BouncyCastle.Utilities;
using static MudBlazor.Defaults;

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
        public async Task<ActionResult<GenerateTimetableResponse>> Generate(
     [FromBody] GenerateTimetableRequest request)
        {
            try
            {
                // -----------------------------
                // 1. Generate initial timetable
                // -----------------------------
                var generatedSlots = _generator.Generate(
                    request.TimeSlots,
                    request.Schedules,
                    request.AdjacencyRules,
                    request.Activities.FirstOrDefault()
                );

                // -----------------------------
                // 2. Build mutable state
                // -----------------------------
                var state = new TimetableState(
                    generatedSlots,
                    request.Schedules,
                    request.AdjacencyRules
                );

                // -----------------------------
                // 3. Validate raw generator output (optional but useful)
                // -----------------------------
                var initialReport = new TimetableValidator().Analyze(
                    state,
                    request.Schedules.ToDictionary(s => s.SubjectId),
                    request.AdjacencyRules.ToDictionary(a => a.SubjectId)
                );

                if (initialReport.InvariantViolations.Any())
                {
                    _logger.LogWarning(
                        "Initial generator violations: {Violations}",
                        string.Join(", ", initialReport.InvariantViolations)
                    );
                }

                // -----------------------------
                // 4. Repair + optimize
                // -----------------------------
                var coreSubjects = request.Schedules
                    .Where(s => s.IsCoreSubject)
                    .Select(s => s.SubjectId)
                    .ToHashSet();

                var builder = new TimetableBuilder(coreSubjects);

                builder.Build(
                    state,
                    request.Activities.FirstOrDefault()
                );

                // -----------------------------
                // 5. Validate final state
                // -----------------------------
                var finalReport = new TimetableValidator().Analyze(
                    state,
                    request.Schedules.ToDictionary(s => s.SubjectId),
                    request.AdjacencyRules.ToDictionary(a => a.SubjectId)
                );

                if (finalReport.InvariantViolations.Any())
                {
                    _logger.LogError(
                        "Final timetable violations after repair: {Violations}",
                        string.Join(", ", finalReport.InvariantViolations)
                    );


                }

                // -----------------------------
                // 6. Return FINAL slots (not generator output)
                // -----------------------------
                var data = MapToGeneratedSlotPreview(
                    state.Slots,
                    request.classes
                   
                );

                return Ok(new GenerateTimetableResponse
                {
                    Success = true,
                    Slots = data
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

        [HttpPost("validatesmart")]
        public ActionResult<TimeTableReportDTO> Validate(
    TimetableValidationRequest request)
        {
            var state = TimetableStateFactory.FromGeneratedSlots(
                request.Slots,
                request.Subjects,
                request.Adjacency);

            var validator = new TimetableValidator();

            var report = validator.Analyze(
                state,
                request.Subjects,
                request.Adjacency);
            TimeTableReportDTO reportDTO = new()
            {
                InvariantViolations = report.InvariantViolations,
                Metrics = report.Metrics,
            };
            return Ok(reportDTO);
        }
        private List<GeneratedSlotPreview> MapToGeneratedSlotPreview(List<TimeSlot> timeSlots, List<Class> classes)
        {
            var previews = new List<GeneratedSlotPreview>();

            foreach (var slot in timeSlots)
            {
                // Look up the class and teacher info if needed
                var cls = classes.FirstOrDefault(c => c.ClassID == slot.SubjectId);
                var teacherName = cls?.Teacher.LastName ?? "";

                previews.Add(new GeneratedSlotPreview
                {
                    ClassId = cls?.ClassID,
                    SubjectId = slot.SubjectId,
                    SubjectName = slot.SubjectName,
                    TeacherName = teacherName,
                    Slot = slot,
                    DayOfWeek = slot.Day.ToString()
                });
            }

            return previews;
        }


    }
}
