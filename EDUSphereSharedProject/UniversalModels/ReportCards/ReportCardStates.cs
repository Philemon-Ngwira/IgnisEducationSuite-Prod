namespace EDUSphereSharedProject.UniversalModels.ReportCards
{
    /// <summary>
    /// ReportCard.ApprovalStatus holds a ReportCardState id. These ids were previously hardcoded as
    /// string literals inside the report card razor pages; this is the single place they live now,
    /// so the entry gate cannot disagree between the UI and the server.
    /// </summary>
    public static class ReportCardStates
    {
        /// <summary>Subject teachers (and admins) may enter and change scores.</summary>
        public const string TeacherEntryOpen = "DCB75F3C-FF7C-4472-AAEB-667DB7BC86A0";

        /// <summary>Scores are frozen; the Dean is reviewing and commenting.</summary>
        public const string DeanEntryOpen = "40CF47A6-705F-4432-9DF7-38776A7DE5C0";

        /// <summary>Scores are frozen; the Principal is reviewing and commenting.</summary>
        public const string PrincipalEntryOpen = "4B571B0A-9CC0-42E4-BDB6-5A143124BC52";

        public static bool IsTeacherEntryOpen(string? approvalStatus) =>
            string.Equals(approvalStatus?.Trim(), TeacherEntryOpen, StringComparison.OrdinalIgnoreCase);
    }
}
