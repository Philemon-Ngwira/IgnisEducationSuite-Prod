using EduSphereDomain.Data;
using EduSphereDomain.Repositories;
using EDUSphereSharedProject.UniversalModels.ParentLinking;
using Microsoft.EntityFrameworkCore;

namespace IgnisEducationSuite.ServerServices.ParentLinking
{
    /// <summary>
    /// Finding students with no parent on record, and attaching them to one.
    ///
    /// The link itself is just Student.ParentID — the same field CreateUserDialog sets when a parent
    /// is created with children selected. This service exists so the gap can be found and closed
    /// without going through user creation, for the common case where the parent already exists.
    ///
    /// School is resolved from the signed-in user, so no caller can reach another school's students.
    /// </summary>
    public class ParentLinkingService
    {
        private readonly PhoenixEdusphereContext _context;
        private readonly EduSphereRepository _repository;

        public ParentLinkingService(PhoenixEdusphereContext context, EduSphereRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        public async Task<UnparentedSummaryDto> GetSummaryAsync(string requestingUserId)
        {
            var summary = new UnparentedSummaryDto();

            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null) return summary;

            var students = await _context.Students
                .Where(s => s.SchoolID == schoolId)
                .Select(s => new { s.ParentID, s.AcademicLevel, s.LevelName, s.GradeSection })
                .ToListAsync();

            summary.TotalStudents = students.Count;

            var unparented = students.Where(s => s.ParentID == null).ToList();
            summary.UnparentedCount = unparented.Count;

            summary.ByLevel = unparented
                .GroupBy(s => new { s.AcademicLevel, s.LevelName, s.GradeSection })
                .Select(g => new UnparentedByLevelDto
                {
                    AcademicLevel = g.Key.AcademicLevel,
                    LevelName = g.Key.LevelName,
                    GradeSection = g.Key.GradeSection,
                    UnparentedCount = g.Count(),
                })
                .OrderByDescending(g => g.UnparentedCount)
                .ThenBy(g => g.AcademicLevel)
                .ToList();

            return summary;
        }

        public async Task<List<UnparentedStudentDto>> ListUnparentedAsync(
            string requestingUserId, int? academicLevel, string? gradeSection, string? search)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null) return new List<UnparentedStudentDto>();

            var query = _context.Students.Where(s => s.SchoolID == schoolId && s.ParentID == null);

            if (academicLevel.HasValue)
            {
                query = query.Where(s => s.AcademicLevel == academicLevel.Value);
            }

            if (!string.IsNullOrWhiteSpace(gradeSection))
            {
                query = query.Where(s => s.GradeSection == gradeSection);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    (s.FirstName != null && s.FirstName.Contains(search)) ||
                    (s.LastName != null && s.LastName.Contains(search)) ||
                    (s.StudentNumber != null && s.StudentNumber.Contains(search)));
            }

            var students = await query
                .OrderBy(s => s.AcademicLevel)
                .ThenBy(s => s.GradeSection)
                .ThenBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new UnparentedStudentDto
                {
                    StudentId = s.StudentID,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    StudentNumber = s.StudentNumber,
                    AcademicLevel = s.AcademicLevel,
                    LevelName = s.LevelName,
                    GradeSection = s.GradeSection,
                })
                .ToListAsync();

            return students;
        }

        /// <summary>
        /// Existing parents, with the children already attached to them. The child list is what lets
        /// an admin recognise a family and spot that an unparented student is a sibling.
        /// </summary>
        public async Task<List<ParentOptionDto>> ListParentsAsync(string requestingUserId, string? search)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null) return new List<ParentOptionDto>();

            var query = _context.Parents.Where(p => p.SchoolID == schoolId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    (p.FirstName != null && p.FirstName.Contains(search)) ||
                    (p.LastName != null && p.LastName.Contains(search)) ||
                    (p.Email != null && p.Email.Contains(search)) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(search)));
            }

            var parents = await query
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Select(p => new
                {
                    p.ParentID,
                    p.FirstName,
                    p.LastName,
                    p.Email,
                    p.PhoneNumber,
                    p.UserId,
                })
                .ToListAsync();

            if (parents.Count == 0) return new List<ParentOptionDto>();

            var parentIds = parents.Select(p => p.ParentID).ToList();

            var children = await _context.Students
                .Where(s => s.ParentID != null && parentIds.Contains(s.ParentID.Value))
                .Select(s => new { ParentId = s.ParentID!.Value, s.FirstName, s.LastName })
                .ToListAsync();

            var childrenByParent = children
                .GroupBy(c => c.ParentId)
                .ToDictionary(g => g.Key, g => g.Select(c => $"{c.FirstName} {c.LastName}".Trim()).ToList());

            return parents.Select(p => new ParentOptionDto
            {
                ParentId = p.ParentID,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                PhoneNumber = p.PhoneNumber,
                HasUserAccount = !string.IsNullOrWhiteSpace(p.UserId),
                LinkedChildNames = childrenByParent.TryGetValue(p.ParentID, out var names) ? names : new List<string>(),
                LinkedChildrenCount = childrenByParent.TryGetValue(p.ParentID, out var list) ? list.Count : 0,
            }).ToList();
        }

        /// <summary>
        /// Attaches students to a parent.
        ///
        /// Students that already have a parent are skipped and reported rather than reassigned: this
        /// screen exists to fill gaps, and silently moving a child from one family to another because
        /// of a mis-click is not a recoverable mistake.
        /// </summary>
        public async Task<LinkStudentsToParentResult> LinkStudentsAsync(string requestingUserId, LinkStudentsToParentRequest request)
        {
            var result = new LinkStudentsToParentResult();

            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
            {
                result.Errors.Add("Could not determine your school.");
                return result;
            }

            if (request.StudentIds.Count == 0)
            {
                result.Errors.Add("Select at least one student to link.");
                return result;
            }

            var parent = await _context.Parents
                .FirstOrDefaultAsync(p => p.ParentID == request.ParentId && p.SchoolID == schoolId);

            if (parent is null)
            {
                result.Errors.Add("Parent not found in this school.");
                return result;
            }

            var students = await _context.Students
                .Where(s => request.StudentIds.Contains(s.StudentID) && s.SchoolID == schoolId)
                .ToListAsync();

            if (students.Count != request.StudentIds.Count)
            {
                result.Errors.Add("One or more students were not found in this school.");
                return result;
            }

            foreach (var student in students)
            {
                if (student.ParentID is not null && student.ParentID != request.ParentId)
                {
                    result.SkippedAlreadyLinked.Add($"{student.FirstName} {student.LastName}".Trim());
                    continue;
                }

                if (student.ParentID == request.ParentId) continue;

                student.ParentID = request.ParentId;
                result.LinkedCount++;
            }

            if (result.LinkedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            result.Succeeded = true;
            return result;
        }

        /// <summary>Detaches a student, putting them back on this screen's list. Used to correct a
        /// wrong link, since the link step deliberately refuses to overwrite one.</summary>
        public async Task<bool> UnlinkStudentAsync(string requestingUserId, Guid studentId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null) return false;

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentID == studentId && s.SchoolID == schoolId);

            if (student is null) return false;

            student.ParentID = null;
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<Guid?> ResolveSchoolIdAsync(string requestingUserId)
        {
            if (string.IsNullOrWhiteSpace(requestingUserId)) return null;

            var initData = await _repository.GetInitializationDataResults(requestingUserId);
            return initData?.FirstOrDefault()?.SchoolID;
        }
    }
}
