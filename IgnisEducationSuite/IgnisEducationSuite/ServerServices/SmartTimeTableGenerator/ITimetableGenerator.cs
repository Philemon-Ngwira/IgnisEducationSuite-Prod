using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public interface ITimetableGenerator
    {
            GenerationResult Generate(
            IReadOnlyList<TimeSlot> timeSlots,
            IReadOnlyList<SubjectScheduleConfig> schedules,
            IReadOnlyList<SubjectAdjacencyConstraints> adjacencyRules,
            IReadOnlyList<SubjectTimeConstraints> timeRules,
            IReadOnlyList<TimeTableActivity> activities,
    IReadOnlyList<TeacherScheduleConstraints> teacherConstraints
        );
    }

}
