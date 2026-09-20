using EduSphereDomain.Data;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.ReportCards;
using Microsoft.EntityFrameworkCore;

namespace EduSphereDomain.Repositories.ReportCards
{
    public class ReportCardEntryRepository : IReportCardEntryRepository
    {
        private readonly PhoenixEdusphereContext _context;

        public ReportCardEntryRepository(PhoenixEdusphereContext context)
        {
            _context = context;
        }

        public async Task<Guid?> GetTeacherIdForUserAsync(Guid schoolId, string userId)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.SchoolID == schoolId && t.UserID == userId);

            return teacher?.TeacherID;
        }

        public async Task<List<TeacherClassSummaryDto>> ListClassesForEntryAsync(Guid schoolId, Guid? teacherId, string reportCardType)
        {
            var classesQuery = _context.Classes.Where(c => c.SChoolID == schoolId);

            if (teacherId.HasValue)
            {
                classesQuery = classesQuery.Where(c => c.TeacherID == teacherId.Value);
            }

            var classes = await classesQuery
                .Select(c => new
                {
                    c.ClassID,
                    c.ClassName,
                    c.LevelName,
                    c.AcademicLevel,
                    c.GradeSection,
                    c.GroupName,
                })
                .ToListAsync();

            if (classes.Count == 0) return new List<TeacherClassSummaryDto>();

            var classIds = classes.Select(c => c.ClassID).ToList();

            // Enrolment per class.
            var enrolments = await _context.StudentClasses
                .Where(sc => sc.ClassID != null && classIds.Contains(sc.ClassID.Value) && sc.StudentID != null)
                .Select(sc => new { ClassId = sc.ClassID!.Value, StudentId = sc.StudentID!.Value })
                .ToListAsync();

            var studentIds = enrolments.Select(e => e.StudentId).Distinct().ToList();

            // Report cards for those students, for this report type only.
            var headers = await _context.ReportCards
                .Where(r => r.SchoolID == schoolId
                            && r.StudentID != null
                            && studentIds.Contains(r.StudentID.Value)
                            && r.ReportCardType == reportCardType)
                .Select(r => new { r.ReportCardID, StudentId = r.StudentID!.Value, r.ApprovalStatus })
                .ToListAsync();

            var headerByStudent = headers
                .GroupBy(h => h.StudentId)
                .ToDictionary(g => g.Key, g => g.First());

            var reportCardIds = headers.Select(h => h.ReportCardID).ToList();

            // Recorded marks, keyed by (report card, class). A row with a null Score counts as not
            // entered — that distinction is the whole point of the class sheet.
            var details = await _context.ReportCardDetails
                .Where(d => d.ReportCardID != null
                            && reportCardIds.Contains(d.ReportCardID.Value)
                            && d.ClassID != null
                            && classIds.Contains(d.ClassID.Value))
                .Select(d => new { ReportCardId = d.ReportCardID!.Value, ClassId = d.ClassID!.Value, d.Score })
                .ToListAsync();

            var scoredKeys = details
                .Where(d => d.Score != null)
                .Select(d => (d.ReportCardId, d.ClassId))
                .ToHashSet();

            var enrolmentsByClass = enrolments
                .GroupBy(e => e.ClassId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.StudentId).Distinct().ToList());

            var summaries = new List<TeacherClassSummaryDto>();

            foreach (var cls in classes)
            {
                var students = enrolmentsByClass.TryGetValue(cls.ClassID, out var list) ? list : new List<Guid>();

                var withCard = 0;
                var openForEntry = 0;
                var entered = 0;

                foreach (var studentId in students)
                {
                    if (!headerByStudent.TryGetValue(studentId, out var header)) continue;

                    withCard++;

                    if (ReportCardStates.IsTeacherEntryOpen(header.ApprovalStatus)) openForEntry++;

                    if (scoredKeys.Contains((header.ReportCardID, cls.ClassID))) entered++;
                }

                summaries.Add(new TeacherClassSummaryDto
                {
                    ClassId = cls.ClassID,
                    ClassName = cls.ClassName,
                    LevelName = cls.LevelName,
                    AcademicLevel = cls.AcademicLevel,
                    GradeSection = cls.GradeSection,
                    GroupName = cls.GroupName,
                    EnrolledCount = students.Count,
                    WithReportCardCount = withCard,
                    OpenForEntryCount = openForEntry,
                    EnteredCount = entered,
                });
            }

            return summaries
                .OrderBy(s => s.AcademicLevel)
                .ThenBy(s => s.GradeSection)
                .ThenBy(s => s.ClassName)
                .ToList();
        }

        public async Task<ClassResultsSheetDto?> GetClassSheetAsync(Guid schoolId, Guid classId, string reportCardType)
        {
            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.ClassID == classId && c.SChoolID == schoolId);
            if (cls is null) return null;

            // Start from enrolment, not from report cards — a student missing a report card must
            // still appear, flagged, rather than silently vanishing from the sheet.
            var studentIds = await _context.StudentClasses
                .Where(sc => sc.ClassID == classId && sc.StudentID != null)
                .Select(sc => sc.StudentID!.Value)
                .Distinct()
                .ToListAsync();

            var students = await _context.Students
                .Where(s => studentIds.Contains(s.StudentID))
                .Select(s => new
                {
                    s.StudentID,
                    s.FirstName,
                    s.LastName,
                    s.StudentNumber,
                    s.GradeSection,
                })
                .ToListAsync();

            var headers = await _context.ReportCards
                .Where(r => r.SchoolID == schoolId
                            && r.StudentID != null
                            && studentIds.Contains(r.StudentID.Value)
                            && r.ReportCardType == reportCardType)
                .Select(r => new { r.ReportCardID, StudentId = r.StudentID!.Value, r.ApprovalStatus })
                .ToListAsync();

            var headerByStudent = headers
                .GroupBy(h => h.StudentId)
                .ToDictionary(g => g.Key, g => g.First());

            var reportCardIds = headers.Select(h => h.ReportCardID).ToList();

            var details = await _context.ReportCardDetails
                .Where(d => d.ReportCardID != null
                            && reportCardIds.Contains(d.ReportCardID.Value)
                            && d.ClassID == classId)
                .Select(d => new { d.ReportCardDetailID, ReportCardId = d.ReportCardID!.Value, d.Score, d.Grade })
                .ToListAsync();

            var detailByReportCard = details
                .GroupBy(d => d.ReportCardId)
                .ToDictionary(g => g.Key, g => g.First());

            var gradeBands = await _context.GradingScales
                .Where(g => g.SchoolID == schoolId)
                .OrderBy(g => g.LoweScore)
                .Select(g => new GradeBandDto
                {
                    LowerScore = g.LoweScore,
                    UpperScore = g.UpperScore,
                    Grade = g.Description,
                    GPA = g.GPA,
                    Comment = g.Comment,
                })
                .ToListAsync();

            var rows = students.Select(s =>
            {
                headerByStudent.TryGetValue(s.StudentID, out var header);

                var row = new ClassResultRowDto
                {
                    StudentId = s.StudentID,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    StudentNumber = s.StudentNumber,
                    GradeSection = s.GradeSection,
                    ReportCardId = header?.ReportCardID,
                    ApprovalStatus = header?.ApprovalStatus,
                    IsOpenForEntry = header is not null && ReportCardStates.IsTeacherEntryOpen(header.ApprovalStatus),
                };

                if (header is not null && detailByReportCard.TryGetValue(header.ReportCardID, out var detail))
                {
                    row.ReportCardDetailId = detail.ReportCardDetailID;
                    row.Score = detail.Score;
                    row.Grade = detail.Grade;
                }

                return row;
            })
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .ToList();

            var sheet = new ClassResultsSheetDto
            {
                ClassId = cls.ClassID,
                ClassName = cls.ClassName,
                LevelName = cls.LevelName,
                GradeSection = cls.GradeSection,
                ReportCardType = reportCardType,
                Rows = rows,
                GradeBands = gradeBands,
            };

            if (gradeBands.Count == 0)
            {
                sheet.Errors.Add("No grading scale is configured for this school, so scores cannot be graded. Ask an administrator to set one up.");
            }

            return sheet;
        }

        public async Task<bool> CanTeacherEnterClassAsync(Guid schoolId, Guid classId, Guid? teacherId)
        {
            var query = _context.Classes.Where(c => c.ClassID == classId && c.SChoolID == schoolId);

            if (teacherId.HasValue)
            {
                query = query.Where(c => c.TeacherID == teacherId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<SaveClassResultsResult> SaveClassResultsAsync(Guid schoolId, SaveClassResultsRequest request)
        {
            var result = new SaveClassResultsResult();

            var gradeBands = await _context.GradingScales
                .Where(g => g.SchoolID == schoolId)
                .ToListAsync();

            if (gradeBands.Count == 0)
            {
                result.Errors.Add("No grading scale is configured for this school, so scores cannot be graded.");
                return result;
            }

            var studentIds = request.Entries.Select(e => e.StudentId).Distinct().ToList();

            // Only students actually enrolled in this class may be written — a client cannot smuggle
            // in a student from another class.
            var enrolled = await _context.StudentClasses
                .Where(sc => sc.ClassID == request.ClassId && sc.StudentID != null && studentIds.Contains(sc.StudentID.Value))
                .Select(sc => sc.StudentID!.Value)
                .Distinct()
                .ToListAsync();

            var enrolledSet = enrolled.ToHashSet();

            var students = await _context.Students
                .Where(s => studentIds.Contains(s.StudentID))
                .Select(s => new { s.StudentID, s.FirstName, s.LastName })
                .ToListAsync();

            var nameByStudent = students.ToDictionary(
                s => s.StudentID,
                s => $"{s.FirstName} {s.LastName}".Trim());

            var headers = await _context.ReportCards
                .Where(r => r.SchoolID == schoolId
                            && r.StudentID != null
                            && studentIds.Contains(r.StudentID.Value)
                            && r.ReportCardType == request.ReportCardType)
                .ToListAsync();

            var headerByStudent = headers
                .GroupBy(h => h.StudentID!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var reportCardIds = headers.Select(h => h.ReportCardID).ToList();

            var existingDetails = await _context.ReportCardDetails
                .Where(d => d.ReportCardID != null
                            && reportCardIds.Contains(d.ReportCardID.Value)
                            && d.ClassID == request.ClassId)
                .ToListAsync();

            var detailByReportCard = existingDetails
                .GroupBy(d => d.ReportCardID!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            // Validate everything before writing anything, so a single bad score cannot leave half
            // a class saved.
            var toWrite = new List<(ReportCard Header, ClassResultEntryDto Entry, GradingScale? Band)>();

            foreach (var entry in request.Entries)
            {
                var name = nameByStudent.TryGetValue(entry.StudentId, out var n) ? n : "A student";

                if (!enrolledSet.Contains(entry.StudentId))
                {
                    result.Errors.Add($"{name} is not enrolled in this class.");
                    continue;
                }

                if (!headerByStudent.TryGetValue(entry.StudentId, out var header))
                {
                    result.SkippedNoReportCard.Add(name);
                    continue;
                }

                if (!ReportCardStates.IsTeacherEntryOpen(header.ApprovalStatus))
                {
                    result.SkippedLocked.Add(name);
                    continue;
                }

                GradingScale? band = null;

                if (entry.Score is { } score)
                {
                    band = gradeBands.FirstOrDefault(g => score >= g.LoweScore && score <= g.UpperScore);

                    if (band is null)
                    {
                        result.Errors.Add($"{name}: a score of {score} does not fall inside any grading band.");
                        continue;
                    }
                }

                toWrite.Add((header, entry, band));
            }

            if (result.Errors.Count > 0)
            {
                return result;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var (header, entry, band) in toWrite)
                {
                    detailByReportCard.TryGetValue(header.ReportCardID, out var detail);

                    if (entry.Score is null)
                    {
                        // Clearing a mark: blank the score rather than deleting the row, so the
                        // subject still appears on the report card as unmarked.
                        if (detail is not null)
                        {
                            detail.Score = null;
                            detail.Grade = null;
                            detail.GPA = null;
                            detail.FinalComment = null;
                            result.ClearedCount++;
                        }

                        continue;
                    }

                    if (detail is null)
                    {
                        detail = new ReportCardDetail
                        {
                            ReportCardDetailID = Guid.NewGuid(),
                            ReportCardID = header.ReportCardID,
                            ClassID = request.ClassId,
                        };

                        _context.ReportCardDetails.Add(detail);
                        detailByReportCard[header.ReportCardID] = detail;
                    }

                    detail.Score = entry.Score;
                    detail.Grade = band!.Description;
                    detail.GPA = band.GPA is null ? null : (double)band.GPA.Value;
                    detail.FinalComment = band.Comment;

                    result.SavedCount++;
                }

                await _context.SaveChangesAsync();

                await RecalculateHeaderGpaAsync(toWrite.Select(w => w.Header).Distinct().ToList());

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Succeeded = true;
                return result;
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Already rolled back by the database.
                }

                result.Succeeded = false;
                result.Errors.Add($"Save failed: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Recomputes each affected report card's overall GPA from ALL of its subject rows.
        ///
        /// The previous per-student dialog averaged only the subjects visible to whoever was saving,
        /// so a subject teacher's save overwrote the student's overall GPA with the average of just
        /// their own subjects — whichever teacher saved last won. Averaging every recorded subject
        /// here makes the result independent of who saved and when.
        /// </summary>
        private async Task RecalculateHeaderGpaAsync(List<ReportCard> headers)
        {
            if (headers.Count == 0) return;

            var reportCardIds = headers.Select(h => h.ReportCardID).ToList();

            var allDetails = await _context.ReportCardDetails
                .Where(d => d.ReportCardID != null && reportCardIds.Contains(d.ReportCardID.Value))
                .Select(d => new { ReportCardId = d.ReportCardID!.Value, d.GPA, d.Score })
                .ToListAsync();

            var gpasByReportCard = allDetails
                .Where(d => d.Score != null && d.GPA != null)
                .GroupBy(d => d.ReportCardId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.GPA!.Value).ToList());

            foreach (var header in headers)
            {
                if (gpasByReportCard.TryGetValue(header.ReportCardID, out var gpas) && gpas.Count > 0)
                {
                    header.GPA = (decimal)Math.Round(gpas.Sum() / gpas.Count, 2);
                }
                else
                {
                    // Every mark cleared — leave no stale average behind.
                    header.GPA = null;
                }
            }
        }
    }
}
