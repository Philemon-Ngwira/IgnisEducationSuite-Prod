using System.ComponentModel.DataAnnotations;

namespace EDUSphereSharedProject.UniversalModels.ReportCards
{
    /// <summary>
    /// One subject-class a teacher is responsible for — "Mathematics, Form 1 B" — with entry
    /// progress for the selected report type, so a teacher can see at a glance which of their
    /// classes still need marks.
    /// </summary>
    public class TeacherClassSummaryDto
    {
        public Guid ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? LevelName { get; set; }
        public int? AcademicLevel { get; set; }
        public string? GradeSection { get; set; }
        public string? GroupName { get; set; }

        /// <summary>Students enrolled in this class.</summary>
        public int EnrolledCount { get; set; }

        /// <summary>Of those, how many have a report card for this term/type at all.</summary>
        public int WithReportCardCount { get; set; }

        /// <summary>Of those, how many are still open for teacher entry.</summary>
        public int OpenForEntryCount { get; set; }

        /// <summary>How many already have a score recorded for this subject.</summary>
        public int EnteredCount { get; set; }

        /// <summary>Enrolled students with no report card for this term — late enrolments that an
        /// admin has to add before their marks can be recorded.</summary>
        public int MissingReportCardCount => Math.Max(0, EnrolledCount - WithReportCardCount);

        public bool IsComplete => OpenForEntryCount > 0 && EnteredCount >= OpenForEntryCount;
    }

    /// <summary>
    /// The classes a caller may enter marks for, plus what their account is allowed to see.
    /// A user who has a teacher record always defaults to their OWN classes, even when they also
    /// hold an administrative role — seeing every class in the school is opt-in, never the default.
    /// </summary>
    public class ClassEntryScopeDto
    {
        public List<TeacherClassSummaryDto> Classes { get; set; } = new();

        /// <summary>True when the caller holds a school-wide role and may switch to every class.</summary>
        public bool CanViewAllClasses { get; set; }

        /// <summary>True when the list currently holds every class in the school rather than the caller's own.</summary>
        public bool IsViewingAllClasses { get; set; }

        /// <summary>False when the account has no linked Teacher row.</summary>
        public bool HasTeacherRecord { get; set; }

        public List<string> Errors { get; set; } = new();
    }

    /// <summary>One student's row on a class mark sheet.</summary>
    public class ClassResultRowDto
    {
        public Guid StudentId { get; set; }
        public string? StudentNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? GradeSection { get; set; }

        public Guid? ReportCardId { get; set; }
        public string? ApprovalStatus { get; set; }
        public Guid? ReportCardDetailId { get; set; }

        /// <summary>Null means no mark has been recorded — deliberately distinct from a score of 0.</summary>
        public double? Score { get; set; }
        public string? Grade { get; set; }

        /// <summary>False when this student has no report card for the term (a late enrolment).
        /// Such rows are shown but cannot be entered — the school must have the student added first.</summary>
        public bool HasReportCard => ReportCardId is not null;

        /// <summary>False once the report card has moved past teacher entry.</summary>
        public bool IsOpenForEntry { get; set; }

        public bool IsEnterable => HasReportCard && IsOpenForEntry;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <summary>A whole class's mark sheet for one report type.</summary>
    public class ClassResultsSheetDto
    {
        public Guid ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? LevelName { get; set; }
        public string? GradeSection { get; set; }
        public string? ReportCardType { get; set; }

        public List<ClassResultRowDto> Rows { get; set; } = new();
        public List<GradeBandDto> GradeBands { get; set; } = new();
        public List<string> Errors { get; set; } = new();

        public int EnterableCount => Rows.Count(r => r.IsEnterable);
        public int EnteredCount => Rows.Count(r => r.IsEnterable && r.Score is not null);
        public int MissingCount => EnterableCount - EnteredCount;
    }

    /// <summary>The school's grading scale, so the sheet can show the grade as a score is typed
    /// without a server round trip. The server still recomputes on save — this is display only.</summary>
    public class GradeBandDto
    {
        public double? LowerScore { get; set; }
        public double? UpperScore { get; set; }
        public string? Grade { get; set; }
        public decimal? GPA { get; set; }
        public string? Comment { get; set; }
    }

    public class ClassResultEntryDto
    {
        [Required]
        public Guid StudentId { get; set; }

        /// <summary>Null clears any previously recorded mark for this student and subject.</summary>
        public double? Score { get; set; }
    }

    public class SaveClassResultsRequest
    {
        [Required]
        public Guid ClassId { get; set; }

        [Required, StringLength(50)]
        public string ReportCardType { get; set; } = "";

        public List<ClassResultEntryDto> Entries { get; set; } = new();
    }

    public class SaveClassResultsResult
    {
        public bool Succeeded { get; set; }
        public int SavedCount { get; set; }
        public int ClearedCount { get; set; }

        /// <summary>Students whose marks could not be recorded because they have no report card for
        /// this term. Named so the teacher can tell the office exactly who to add.</summary>
        public List<string> SkippedNoReportCard { get; set; } = new();

        /// <summary>Students whose report card has already moved past teacher entry.</summary>
        public List<string> SkippedLocked { get; set; } = new();

        public List<string> Errors { get; set; } = new();
    }
}
