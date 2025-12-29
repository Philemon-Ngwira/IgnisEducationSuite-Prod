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
                    request.Activities.First()
                );
                var state = new TimetableState(
          result,
          request.Schedules,
          request.AdjacencyRules
      );

                var initialReport = new TimetableValidator().Analyze(
            state,
            request.Schedules.ToDictionary(s => s.SubjectId),
            request.AdjacencyRules.ToDictionary(a => a.SubjectId)
        );

                Console.WriteLine("\nInitial violations:");
                foreach (var v in initialReport.InvariantViolations)
                    Console.WriteLine(" - " + v);

                var c = request.Schedules.Where(x => x.IsCoreSubject).Select(x => x.SubjectId).ToList();
                var coreSubjects = c.ToHashSet();
                var repairAndOptimize = new TimetableBuilder(coreSubjects);
                var finalReport = new TimetableValidator().Analyze(
             state,
            request.Schedules.ToDictionary(s => s.SubjectId),
            request.AdjacencyRules.ToDictionary(a => a.SubjectId)
       );

                var data = MapToGeneratedSlotPreview(result, request.classes);

                // Just return the flat list directly
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
