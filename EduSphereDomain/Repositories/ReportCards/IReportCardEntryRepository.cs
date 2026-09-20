using EDUSphereSharedProject.UniversalModels.ReportCards;

namespace EduSphereDomain.Repositories.ReportCards
{
    public interface IReportCardEntryRepository
    {
        /// <summary>Resolves the Teacher row linked to a user account, or null when the user is not a teacher.</summary>
        Task<Guid?> GetTeacherIdForUserAsync(Guid schoolId, string userId);

        /// <summary>
        /// The subject-classes this teacher teaches, with entry progress for the given report type.
        /// Pass a null teacherId for an admin, who sees every class in the school.
        /// </summary>
        Task<List<TeacherClassSummaryDto>> ListClassesForEntryAsync(Guid schoolId, Guid? teacherId, string reportCardType);

        /// <summary>Every student enrolled in the class, whether or not they have a report card.</summary>
        Task<ClassResultsSheetDto?> GetClassSheetAsync(Guid schoolId, Guid classId, string reportCardType);

        /// <summary>True when the class belongs to this school and (for a teacher) is taught by them.</summary>
        Task<bool> CanTeacherEnterClassAsync(Guid schoolId, Guid classId, Guid? teacherId);

        Task<SaveClassResultsResult> SaveClassResultsAsync(Guid schoolId, SaveClassResultsRequest request);
    }
}
