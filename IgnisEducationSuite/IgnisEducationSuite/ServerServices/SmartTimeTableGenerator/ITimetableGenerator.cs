using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public interface ITimetableGenerator
    {
        Task<List<TimeSlot>> Generate(

        List<TimeSlot> slots,
        List<SubjectScheduleConfig> subjects,
        List<SubjectAdjacencyConstraints> adjacencyConstraints,
        TimeTableActivity? prepActivity = null,
         TeacherConflictChecker? checker = null);
    }

}
