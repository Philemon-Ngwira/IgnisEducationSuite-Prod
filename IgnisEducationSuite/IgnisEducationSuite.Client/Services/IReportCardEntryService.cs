using EDUSphereSharedProject.UniversalModels.ReportCards;

namespace IgnisEducationSuite.Client.Services
{
    public interface IReportCardEntryService
    {
        /// <param name="activeRole">
        /// The role currently selected in the "Current Role" switcher, for users holding more than
        /// one. The server verifies the caller actually holds it before honouring it, so passing it
        /// can only narrow scope — a user acting as Teacher sees just the classes they teach.
        /// </param>
        Task<ClassEntryScopeDto> ListClassesAsync(string reportCardType, bool includeAllClasses = false, string? activeRole = null);

        Task<ClassResultsSheetDto> GetClassSheetAsync(Guid classId, string reportCardType, string? activeRole = null);

        Task<SaveClassResultsResult> SaveClassResultsAsync(SaveClassResultsRequest request, string? activeRole = null);
    }
}
