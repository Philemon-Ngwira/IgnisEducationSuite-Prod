using EduSphereDomain.Repositories;
using EduSphereDomain.Repositories.ReportCards;
using EDUSphereSharedProject.UniversalModels.ReportCards;

namespace IgnisEducationSuite.ServerServices.ReportCards
{
    /// <summary>
    /// Class-based report card entry. A teacher picks one of the subject-classes they teach and
    /// enters marks for every enrolled student at once, rather than opening each student in turn —
    /// which is what allowed students to be skipped without anyone noticing.
    ///
    /// School AND scope are both resolved server-side from the signed-in user's initialization data,
    /// the same source AppState uses on the client. Nothing about scope is taken from the request,
    /// so a teacher cannot widen their own access by editing what the browser sends.
    /// </summary>
    public class ReportCardEntryOrchestrator
    {
        private static readonly string[] ReportCardTypes = { "Midterm", "EndTerm" };

        /// <summary>
        /// Roles that may opt in to seeing every class in the school. Deliberately excludes Dean and
        /// Principal: their step is the per-student review, not score entry.
        /// </summary>
        private static readonly string[] SchoolWideRoles = { "Admin", "SuperAdmin" };

        private readonly IReportCardEntryRepository _repository;
        private readonly EduSphereRepository _eduSphereRepository;

        public ReportCardEntryOrchestrator(
            IReportCardEntryRepository repository,
            EduSphereRepository eduSphereRepository)
        {
            _repository = repository;
            _eduSphereRepository = eduSphereRepository;
        }

        public async Task<ClassEntryScopeDto> ListClassesAsync(string requestingUserId, string reportCardType, bool includeAllClasses, string? activeRole)
        {
            if (!IsValidReportCardType(reportCardType))
                return new ClassEntryScopeDto { Errors = { $"'{reportCardType}' is not a valid report card type." } };

            var scope = await ResolveScopeAsync(requestingUserId, activeRole);
            if (scope.Error is not null)
                return new ClassEntryScopeDto { Errors = { scope.Error } };

            // Viewing every class is opt-in and only for school-wide roles. A teacher who also holds
            // an admin role still lands on their own classes by default.
            var viewingAll = includeAllClasses && scope.IsSchoolWide;
            var teacherFilter = viewingAll ? null : scope.TeacherId;

            if (teacherFilter is null && !scope.IsSchoolWide)
            {
                return new ClassEntryScopeDto
                {
                    HasTeacherRecord = false,
                    Errors = { "Your user account is not linked to a teacher record, so no classes could be loaded." },
                };
            }

            var classes = await _repository.ListClassesForEntryAsync(scope.SchoolId!.Value, teacherFilter, reportCardType);

            return new ClassEntryScopeDto
            {
                Classes = classes,
                CanViewAllClasses = scope.IsSchoolWide,
                IsViewingAllClasses = teacherFilter is null,
                HasTeacherRecord = scope.TeacherId is not null,
            };
        }

        public async Task<ClassResultsSheetDto> GetClassSheetAsync(string requestingUserId, Guid classId, string reportCardType, string? activeRole)
        {
            if (!IsValidReportCardType(reportCardType))
                return Failed($"'{reportCardType}' is not a valid report card type.");

            var scope = await ResolveScopeAsync(requestingUserId, activeRole);
            if (scope.Error is not null) return Failed(scope.Error);

            if (!await CanEnterClassAsync(scope, classId))
                return Failed("You do not teach this class.");

            var sheet = await _repository.GetClassSheetAsync(scope.SchoolId!.Value, classId, reportCardType);
            return sheet ?? Failed("Class not found.");
        }

        public async Task<SaveClassResultsResult> SaveClassResultsAsync(string requestingUserId, SaveClassResultsRequest request, string? activeRole)
        {
            if (!IsValidReportCardType(request.ReportCardType))
                return new SaveClassResultsResult { Errors = { $"'{request.ReportCardType}' is not a valid report card type." } };

            var scope = await ResolveScopeAsync(requestingUserId, activeRole);
            if (scope.Error is not null)
                return new SaveClassResultsResult { Errors = { scope.Error } };

            if (!await CanEnterClassAsync(scope, request.ClassId))
                return new SaveClassResultsResult { Errors = { "You do not teach this class." } };

            if (request.Entries.Count == 0)
                return new SaveClassResultsResult { Succeeded = true };

            if (request.Entries.GroupBy(e => e.StudentId).Any(g => g.Count() > 1))
                return new SaveClassResultsResult { Errors = { "The same student appeared more than once in this save." } };

            return await _repository.SaveClassResultsAsync(scope.SchoolId!.Value, request);
        }

        /// <summary>
        /// A school-wide role may enter any class in its school; anyone else may only enter a class
        /// their own Teacher row owns. Checked on the sheet and on save, not just when listing, so
        /// a hand-crafted request cannot reach another teacher's class.
        /// </summary>
        private async Task<bool> CanEnterClassAsync(ScopeResult scope, Guid classId)
        {
            if (scope.IsSchoolWide && await _repository.CanTeacherEnterClassAsync(scope.SchoolId!.Value, classId, null))
                return true;

            return scope.TeacherId is not null
                   && await _repository.CanTeacherEnterClassAsync(scope.SchoolId!.Value, classId, scope.TeacherId);
        }

        private static bool IsValidReportCardType(string? reportCardType) =>
            reportCardType is not null && ReportCardTypes.Contains(reportCardType);

        private static ClassResultsSheetDto Failed(string error) => new() { Errors = { error } };

        private readonly record struct ScopeResult(Guid? SchoolId, Guid? TeacherId, bool IsSchoolWide, string? Error);

        /// <summary>
        /// Resolves school, teacher record and whether the caller is acting with school-wide reach.
        ///
        /// The teacher lookup happens for EVERY caller, including administrators. An earlier version
        /// returned school-wide immediately on seeing an admin role, which meant a teacher who also
        /// held one saw every class in the school instead of their own.
        ///
        /// <paramref name="activeRole"/> is the role the user has selected in the "Current Role"
        /// switcher. It is an assertion, not an authorisation: it narrows the caller to that single
        /// role, and only after the role is confirmed to be one they actually hold. An unrecognised
        /// or unheld value falls back to their full role set, so it can never grant reach — only
        /// give it up. Someone with both roles who picks Teacher is therefore treated purely as a
        /// teacher, and sees only the classes they teach.
        /// </summary>
        private async Task<ScopeResult> ResolveScopeAsync(string requestingUserId, string? activeRole)
        {
            if (string.IsNullOrWhiteSpace(requestingUserId))
                return new ScopeResult(null, null, false, "Could not determine the requesting user.");

            var initData = await _eduSphereRepository.GetInitializationDataResults(requestingUserId);
            var data = initData?.FirstOrDefault();

            if (data?.SchoolID is null)
                return new ScopeResult(null, null, false, "Could not determine your school.");

            var heldRoles = (data.RoleName ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var effectiveRoles = !string.IsNullOrWhiteSpace(activeRole) && heldRoles.Contains(activeRole.Trim())
                ? new HashSet<string>(new[] { activeRole.Trim() }, StringComparer.OrdinalIgnoreCase)
                : heldRoles;

            var isSchoolWide = effectiveRoles.Overlaps(SchoolWideRoles);
            var teacherId = await _repository.GetTeacherIdForUserAsync(data.SchoolID.Value, requestingUserId);

            if (teacherId is null && !isSchoolWide)
            {
                return new ScopeResult(null, null, false,
                    "Your user account is not linked to a teacher record, so no classes could be loaded.");
            }

            return new ScopeResult(data.SchoolID.Value, teacherId, isSchoolWide, null);
        }
    }
}
