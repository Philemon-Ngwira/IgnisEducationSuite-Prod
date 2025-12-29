using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public interface ITimetableGenerator
    {
        List<TimeSlot> Generate(
        List<TimeSlot> slots,
        List<SubjectScheduleConfig> subjects,
        List<SubjectAdjacencyConstraints> adjacencyConstraints,
        TimeTableActivity? prepActivity = null);
    }

}
