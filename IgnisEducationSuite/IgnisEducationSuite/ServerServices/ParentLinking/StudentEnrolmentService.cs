using EduSphereDomain.Data;
using EduSphereDomain.Repositories;
using EDUSphereSharedProject.UniversalModels.ParentLinking;
using Microsoft.EntityFrameworkCore;

namespace IgnisEducationSuite.ServerServices.ParentLinking
{
    /// <summary>
    /// Enrolment side of creating a student: matching them to their classes, and offering the group
    /// values that matching actually recognises.
    ///
    /// The bulk upload path populates LevelName and GroupName, which is what makes
    /// SyncStudentsToClasses able to match a student. Individual creation set neither, and nothing
    /// in the app ever invoked the procedure — so a student added one at a time was never enrolled
    /// in any class.
    /// </summary>
    public class StudentEnrolmentService
    {
        private readonly PhoenixEdusphereContext _context;
        private readonly PhoenixEdusphereContextProcedures _procedures;
        private readonly EduSphereRepository _repository;

        public StudentEnrolmentService(
            PhoenixEdusphereContext context,
            PhoenixEdusphereContextProcedures procedures,
            EduSphereRepository repository)
        {
            _context = context;
            _procedures = procedures;
            _repository = repository;
        }

        /// <summary>
        /// Runs SyncStudentsToClasses, then reports what the given student ended up enrolled in.
        ///
        /// Note the procedure takes no parameters: it sweeps every student in the database on each
        /// call. It is guarded by NOT EXISTS so it is idempotent and safe to call repeatedly, but it
        /// is whole-table work for a single new student. If student creation ever becomes frequent,
        /// this is the thing to parameterise by StudentID.
        /// </summary>
        public async Task<SyncStudentClassesResult> SyncAndReportAsync(string requestingUserId, Guid? studentId)
        {
            var result = new SyncStudentClassesResult();

            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
            {
                result.Errors.Add("Could not determine your school.");
                return result;
            }

            try
            {
                await _procedures.SyncStudentsToClassesAsync();
                result.Succeeded = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Class matching failed: {ex.Message}");
                return result;
            }

            if (studentId is null) return result;

            // Report back what the student actually matched, so "created but enrolled in nothing"
            // is visible immediately rather than at reporting time.
            var matched = await (
                from sc in _context.StudentClasses
                join c in _context.Classes on sc.ClassID equals c.ClassID
                where sc.StudentID == studentId.Value
                select c.ClassName).ToListAsync();

            result.StudentClassCount = matched.Count;
            result.MatchedClassNames = matched.Where(n => n != null).OrderBy(n => n).ToList()!;

            return result;
        }

        /// <summary>
        /// The group tokens that class matching will actually recognise, taken from the school's own
        /// classes rather than typed by hand.
        ///
        /// SyncStudentsToClasses matches a group class with
        /// <c>ClassName LIKE '%(' + s.GroupName + ')%'</c>, so the token must appear verbatim inside
        /// the parentheses of a class name. These options are derived by reading those parentheses,
        /// which means an option shown here is guaranteed to match at least one class.
        /// </summary>
        public async Task<List<StudentGroupOptionDto>> ListGroupOptionsAsync(string requestingUserId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null) return new List<StudentGroupOptionDto>();

            var groupClasses = await _context.Classes
                .Where(c => c.SChoolID == schoolId && c.ClassName != null && c.ClassName.Contains("(Group"))
                .Select(c => c.ClassName!)
                .ToListAsync();

            var options = new Dictionary<string, StudentGroupOptionDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var className in groupClasses)
            {
                var token = ExtractGroupToken(className);
                if (token is null) continue;

                if (!options.TryGetValue(token, out var option))
                {
                    option = new StudentGroupOptionDto { GroupName = token };
                    options[token] = option;
                }

                if (!option.ClassNames.Contains(className))
                {
                    option.ClassNames.Add(className);
                }
            }

            return options.Values
                .OrderBy(o => o.GroupName)
                .ToList();
        }

        /// <summary>
        /// Pulls "Group A" out of "Mathematics (Group A)". Returns null when the parentheses are
        /// malformed, so a badly named class is skipped rather than producing an option that could
        /// never match.
        /// </summary>
        private static string? ExtractGroupToken(string className)
        {
            var open = className.LastIndexOf('(');
            if (open < 0) return null;

            var close = className.IndexOf(')', open + 1);
            if (close < 0) return null;

            var token = className.Substring(open + 1, close - open - 1).Trim();
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }

        private async Task<Guid?> ResolveSchoolIdAsync(string requestingUserId)
        {
            if (string.IsNullOrWhiteSpace(requestingUserId)) return null;

            var initData = await _repository.GetInitializationDataResults(requestingUserId);
            return initData?.FirstOrDefault()?.SchoolID;
        }
    }
}
