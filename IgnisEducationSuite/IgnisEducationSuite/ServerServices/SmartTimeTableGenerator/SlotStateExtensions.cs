namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public static class SlotStateExtensions
    {
        public static bool IsMorning(this SlotState slot)
        {
            var start = slot.Slot.StartTime!.Value;
            return start < slot.Slot.SchoolMorningEnd;
        }

        public static bool IsAfternoon(this SlotState slot)
        {
            var start = slot.Slot.StartTime!.Value;
            return start >= slot.Slot.SchoolAfternoonStart;
        }
    }
}
