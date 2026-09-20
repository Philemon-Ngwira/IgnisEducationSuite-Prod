using System;

namespace EDUSphereSharedProject.UniversalModels.ReportCards
{
    /// <summary>
    /// Outcome of a position recalculation.
    ///
    /// Carried in the body rather than expressed as a status code, because the page has to tell a
    /// refusal ("you are not an administrator") apart from a transport failure, and act differently
    /// on each. The stored procedure returns no rows, so this is the only thing the caller gets.
    /// </summary>
    public class PositionRecalculationResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = "";

        /// <summary>How long the recalculation took. Worth showing: on a large school this is a
        /// slow operation, and knowing it took thirty seconds is the difference between "it worked"
        /// and "did I break something".</summary>
        public TimeSpan? Duration { get; set; }

        public static PositionRecalculationResult Ok(string message, TimeSpan? duration = null) =>
            new() { Succeeded = true, Message = message, Duration = duration };

        public static PositionRecalculationResult Fail(string message) =>
            new() { Succeeded = false, Message = message };
    }

    /// <summary>
    /// Position-related settings for the signed-in user's school, read by Report Card Management to
    /// decide whether to offer the manual recalculation controls at all.
    /// </summary>
    public class PositionSettingsDto
    {
        /// <summary>True when a SuperAdmin has switched this school over to manual recalculation.
        /// The recalculate buttons are shown only when this is true; otherwise the school keeps its
        /// existing automatic behaviour and the buttons stay hidden.</summary>
        public bool ManualRecalculationEnabled { get; set; }
    }
}
