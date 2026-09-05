using EduSphereDomain.Data;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using Microsoft.EntityFrameworkCore;

namespace EduSphereDomain.Repositories.Scheduling
{
    public class ScheduleGenerationRepository : IScheduleGenerationRepository
    {
        private readonly PhoenixEdusphereContext _context;

        public ScheduleGenerationRepository(PhoenixEdusphereContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Saves a generated week for one section as a new term version:
        ///   1. soft-retire active rows for the section whose date range overlaps the new one,
        ///   2. delete the StudentClassSchedules rows that pointed at them,
        ///   3. insert the new ClassSchedule rows,
        ///   4. relink every student in the section to the new rows.
        ///
        /// Step 4 is essential and easy to miss: sp_GetStudentTimetable reaches a student's
        /// timetable only through StudentClassSchedules.ScheduleID. Insert new ClassSchedule rows
        /// without relinking and the schedule exists but is invisible to every student, while
        /// teachers (who read ClassSchedule directly) see it fine.
        /// </summary>
        public async Task<SaveGeneratedScheduleResult> SaveGeneratedScheduleAsync(Guid schoolId, SaveGeneratedScheduleRequest request)
        {
            // Guard before opening the transaction: trg_BlockNotRequiredClassSchedule ROLLBACKs the
            // entire transaction and throws 50001 if any inserted row references a notRequired
            // class. Catching it here gives a message naming the offending subjects instead of a
            // raw SQL error, and leaves the existing timetable untouched.
            var requestedClassIds = request.Slots
                .Where(s => s.ClassId.HasValue && s.ClassId.Value != Guid.Empty)
                .Select(s => s.ClassId!.Value)
                .Distinct()
                .ToList();

            var blocked = await _context.Classes
                .Where(c => requestedClassIds.Contains(c.ClassID) && c.notRequired == true)
                .Select(c => c.ClassName)
                .ToListAsync();

            if (blocked.Count > 0)
            {
                return new SaveGeneratedScheduleResult
                {
                    Succeeded = false,
                    Errors = { $"These subjects are marked as not required and cannot be scheduled: {string.Join(", ", blocked)}." },
                };
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Soft-retire any active rows for this section whose date range overlaps the new
                // one (treating null dates as open-ended), rather than blind-upserting and silently
                // overwriting a previous term's schedule in place.
                var overlapping = await _context.ClassSchedules
                    .Where(c =>
                        c.SchoolID == schoolId &&
                        c.AcademicLevel == request.AcademicLevel &&
                        c.AcademicLevelSection == request.AcademicLevelSection &&
                        c.IsActive == true &&
                        (c.StartDate ?? DateTime.MinValue) <= request.EndDate &&
                        (c.EndDate ?? DateTime.MaxValue) >= request.StartDate)
                    .ToListAsync();

                foreach (var row in overlapping)
                {
                    row.IsActive = false;
                }

                // 2. Drop the student links that pointed at the rows we just retired. Mirrors what
                // DeactivateAndCleanSchedules does when a schedule expires; nothing references
                // StudentClassSchedules, so these are safe to remove.
                var retiredIds = overlapping.Select(r => r.ClassScheduleID).ToList();
                if (retiredIds.Count > 0)
                {
                    var staleLinks = await _context.StudentClassSchedules
                        .Where(s => s.ScheduleID.HasValue && retiredIds.Contains(s.ScheduleID.Value))
                        .ToListAsync();

                    _context.StudentClassSchedules.RemoveRange(staleLinks);
                }

                await _context.SaveChangesAsync();

                // 3. Insert the new week.
                var dayIdByName = (await _context.DayofTheWeeks.ToListAsync())
                    .Where(d => d.DayName is not null)
                    .ToDictionary(d => d.DayName, d => d.DayID, StringComparer.OrdinalIgnoreCase);

                var section = await _context.LevelSections.FindAsync(request.AcademicLevelSection);
                var levelSectionName = section?.SectionCode;

                var newRows = new List<ClassSchedule>();
                foreach (var slot in request.Slots)
                {
                    if (!dayIdByName.TryGetValue(slot.Day.ToString(), out var dayId)) continue;

                    // Skip slots that hold neither a subject nor an activity — a free period is the
                    // absence of a row, not a row with nothing in it.
                    var hasClass = slot.ClassId.HasValue && slot.ClassId.Value != Guid.Empty;
                    if (!hasClass && slot.ScheduledActivityId is null) continue;

                    newRows.Add(new ClassSchedule
                    {
                        ClassScheduleID = Guid.NewGuid(),
                        ClassID = hasClass ? slot.ClassId : null,
                        TimeSlotID = slot.TimeSlotId,
                        DayOfTheWeekID = dayId,
                        IsActive = true,
                        StartDate = request.StartDate,
                        EndDate = request.EndDate,
                        SchoolID = schoolId,
                        IsDoublePeriod = slot.IsDoublePeriod,
                        IsFiller = slot.IsFiller,
                        ScheduledActivity = slot.ScheduledActivityId,
                        AcademicLevel = request.AcademicLevel,
                        AcademicLevelSection = request.AcademicLevelSection,
                        LevelSectionName = levelSectionName,
                    });
                }

                _context.ClassSchedules.AddRange(newRows);
                await _context.SaveChangesAsync();

                // 4. Relink students. EffectiveClassID is deliberately left null: overrides are now
                // resolved at read time by sp_GetStudentTimetable, so baking a ReplacementClassID in
                // here would freeze it and re-create the stale-override behaviour this replaced.
                var students = await _context.Students
                    .Where(s => s.AcademicLevel == request.AcademicLevel && s.LevelSectionID == request.AcademicLevelSection)
                    .Select(s => s.StudentID)
                    .ToListAsync();

                var newLinks = new List<StudentClassSchedule>();
                foreach (var studentId in students)
                {
                    foreach (var row in newRows)
                    {
                        newLinks.Add(new StudentClassSchedule
                        {
                            StudentClassScheduleID = Guid.NewGuid(),
                            StudentID = studentId,
                            ScheduleID = row.ClassScheduleID,
                            EffectiveClassID = null,
                        });
                    }
                }

                if (newLinks.Count > 0)
                {
                    _context.StudentClassSchedules.AddRange(newLinks);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return new SaveGeneratedScheduleResult
                {
                    Succeeded = true,
                    RetiredRowCount = overlapping.Count,
                    StudentLinkCount = newLinks.Count,
                };
            }
            catch (Exception ex)
            {
                // trg_BlockNotRequiredClassSchedule issues its own ROLLBACK before throwing, which
                // leaves this transaction already completed — rolling back again would throw and
                // mask the real error.
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Already rolled back by the database; nothing to undo.
                }

                return new SaveGeneratedScheduleResult { Succeeded = false, Errors = { $"Save failed: {ex.Message}" } };
            }
        }

        public async Task<List<ScheduledClassItem>> GetCurrentScheduleAsync(Guid schoolId, int academicLevel, Guid academicLevelSection)
        {
            var rows = await _context.ClassSchedules
                .Include(c => c.Class).ThenInclude(c => c.Teacher)
                .Include(c => c.TimeSlot)
                .Include(c => c.DayOfTheWeek)
                .Include(c => c.ScheduledActivityNavigation)
                .Where(c =>
                    c.SchoolID == schoolId &&
                    c.AcademicLevel == academicLevel &&
                    c.AcademicLevelSection == academicLevelSection &&
                    c.IsActive == true)
                .ToListAsync();

            return rows.Select(r => new ScheduledClassItem
            {
                ClassScheduleId = r.ClassScheduleID,
                DayName = r.DayOfTheWeek?.DayName,
                TimeSlotId = r.TimeSlotID ?? Guid.Empty,
                StartTime = r.TimeSlot?.StartTime is { } st ? TimeOnly.FromTimeSpan(st) : null,
                EndTime = r.TimeSlot?.EndTime is { } et ? TimeOnly.FromTimeSpan(et) : null,
                ClassId = r.ClassID,
                ClassName = r.Class?.ClassName,
                TeacherName = r.Class?.Teacher != null ? $"{r.Class.Teacher.FirstName} {r.Class.Teacher.LastName}".Trim() : null,
                IsDoublePeriod = r.IsDoublePeriod,
                ActivityId = r.ScheduledActivityNavigation?.ActivityID,
                ActivityName = r.ScheduledActivityNavigation?.ActivityName,
            }).ToList();
        }

        // ---------- Overrides ----------

        public async Task<List<OverrideListItem>> ListOverridesAsync(Guid schoolId)
        {
            var schoolLevelIds = await _context.AcademicLevels
                .Where(l => l.SchoolID == schoolId)
                .Select(l => l.AcademicLevelID)
                .ToListAsync();

            var overrides = await _context.TimetableOverrides
                .Where(o => o.AcademicLevelID.HasValue && schoolLevelIds.Contains(o.AcademicLevelID.Value))
                .ToListAsync();

            if (overrides.Count == 0) return new List<OverrideListItem>();

            var dayIds = overrides.Select(o => o.DayOfTheWeekID).Distinct().ToList();
            var timeSlotIds = overrides.Select(o => o.TimeSlotID).Distinct().ToList();
            var levelIds = overrides.Where(o => o.AcademicLevelID.HasValue).Select(o => o.AcademicLevelID!.Value).Distinct().ToList();
            var sectionIds = overrides.Where(o => o.LevelSectionID.HasValue).Select(o => o.LevelSectionID!.Value).Distinct().ToList();
            var replacementClassIds = overrides.Select(o => o.ReplacementClassID).Distinct().ToList();

            var days = await _context.DayofTheWeeks.Where(d => dayIds.Contains(d.DayID)).ToDictionaryAsync(d => d.DayID);
            var slots = await _context.TimeSlots.Where(s => timeSlotIds.Contains(s.TimeslotID)).ToDictionaryAsync(s => s.TimeslotID);
            var levels = await _context.AcademicLevels.Where(l => levelIds.Contains(l.AcademicLevelID)).ToDictionaryAsync(l => l.AcademicLevelID);
            var sections = await _context.LevelSections.Where(s => sectionIds.Contains(s.LevelSectionID)).ToDictionaryAsync(s => s.LevelSectionID);
            var classes = await _context.Classes.Where(c => replacementClassIds.Contains(c.ClassID)).ToDictionaryAsync(c => c.ClassID);

            return overrides.Select(o => new OverrideListItem
            {
                OverrideId = o.TimetableOverrideID,
                DayName = days.TryGetValue(o.DayOfTheWeekID, out var day) ? day.DayName : null,
                StartTime = slots.TryGetValue(o.TimeSlotID, out var slot) && slot.StartTime is { } st ? TimeOnly.FromTimeSpan(st) : null,
                EndTime = slots.TryGetValue(o.TimeSlotID, out var slot2) && slot2.EndTime is { } et ? TimeOnly.FromTimeSpan(et) : null,
                LevelName = o.AcademicLevelID.HasValue && levels.TryGetValue(o.AcademicLevelID.Value, out var level) ? level.LevelName : null,
                SectionCode = o.LevelSectionID.HasValue && sections.TryGetValue(o.LevelSectionID.Value, out var section) ? section.SectionCode : null,
                StudentGroup = o.StudentGroup,
                ReplacementClassName = classes.TryGetValue(o.ReplacementClassID, out var cls) ? cls.ClassName : null,
                Reason = o.Reason,
                EffectiveFrom = ToDateOnly(o.EffectiveFrom),
                EffectiveTo = ToDateOnly(o.EffectiveTo),
                IsActive = o.IsActive ?? false,
            }).ToList();
        }

        public async Task<OverrideDetail?> GetOverrideAsync(Guid overrideId)
        {
            var entity = await _context.TimetableOverrides.FirstOrDefaultAsync(o => o.TimetableOverrideID == overrideId);
            if (entity is null || entity.AcademicLevelID is null || entity.LevelSectionID is null) return null;

            return new OverrideDetail
            {
                OverrideId = entity.TimetableOverrideID,
                AcademicLevelId = entity.AcademicLevelID.Value,
                LevelSectionId = entity.LevelSectionID.Value,
                StudentGroup = entity.StudentGroup,
                DayOfTheWeekId = entity.DayOfTheWeekID,
                TimeSlotId = entity.TimeSlotID,
                ReplacementClassId = entity.ReplacementClassID,
                Reason = entity.Reason,
                EffectiveFrom = ToDateOnly(entity.EffectiveFrom),
                EffectiveTo = ToDateOnly(entity.EffectiveTo),
            };
        }

        public async Task<Guid> CreateOverrideAsync(Guid? createdBy, OverrideRequest request)
        {
            var (academicLevel, gradeSection) = await ResolveLegacyFieldsAsync(request.AcademicLevelId, request.LevelSectionId);

            var entity = new TimetableOverride
            {
                TimetableOverrideID = Guid.NewGuid(),
                AcademicLevel = academicLevel ?? 0,
                GradeSection = gradeSection ?? "",
                StudentGroup = request.StudentGroup,
                DayOfTheWeekID = request.DayOfTheWeekId,
                TimeSlotID = request.TimeSlotId,
                ReplacementClassID = request.ReplacementClassId,
                Reason = request.Reason,
                IsActive = true,
                EffectiveFrom = ToDateTime(request.EffectiveFrom),
                EffectiveTo = ToDateTime(request.EffectiveTo),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                LevelSectionID = request.LevelSectionId,
                AcademicLevelID = request.AcademicLevelId,
            };

            _context.TimetableOverrides.Add(entity);
            await _context.SaveChangesAsync();

            return entity.TimetableOverrideID;
        }

        public async Task<bool> UpdateOverrideAsync(Guid overrideId, OverrideRequest request)
        {
            var entity = await _context.TimetableOverrides.FirstOrDefaultAsync(o => o.TimetableOverrideID == overrideId);
            if (entity is null) return false;

            var (academicLevel, gradeSection) = await ResolveLegacyFieldsAsync(request.AcademicLevelId, request.LevelSectionId);

            entity.AcademicLevel = academicLevel ?? 0;
            entity.GradeSection = gradeSection ?? "";
            entity.StudentGroup = request.StudentGroup;
            entity.DayOfTheWeekID = request.DayOfTheWeekId;
            entity.TimeSlotID = request.TimeSlotId;
            entity.ReplacementClassID = request.ReplacementClassId;
            entity.Reason = request.Reason;
            entity.EffectiveFrom = ToDateTime(request.EffectiveFrom);
            entity.EffectiveTo = ToDateTime(request.EffectiveTo);
            entity.LevelSectionID = request.LevelSectionId;
            entity.AcademicLevelID = request.AcademicLevelId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOverrideAsync(Guid overrideId)
        {
            var entity = await _context.TimetableOverrides.FirstOrDefaultAsync(o => o.TimetableOverrideID == overrideId);
            if (entity is null) return false;

            _context.TimetableOverrides.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> OverrideOverlapsAsync(
            Guid academicLevelId, Guid levelSectionId, string studentGroup, Guid dayId, Guid timeSlotId,
            DateOnly? effectiveFrom, DateOnly? effectiveTo, Guid? excludingOverrideId)
        {
            var from = ToDateTime(effectiveFrom);
            var to = ToDateTime(effectiveTo);

            return await _context.TimetableOverrides.AnyAsync(o =>
                o.IsActive == true &&
                o.AcademicLevelID == academicLevelId &&
                o.LevelSectionID == levelSectionId &&
                o.StudentGroup == studentGroup &&
                o.DayOfTheWeekID == dayId &&
                o.TimeSlotID == timeSlotId &&
                (!excludingOverrideId.HasValue || o.TimetableOverrideID != excludingOverrideId.Value) &&
                (!o.EffectiveFrom.HasValue || !to.HasValue || o.EffectiveFrom.Value <= to.Value) &&
                (!o.EffectiveTo.HasValue || !from.HasValue || o.EffectiveTo.Value >= from.Value));
        }

        public async Task<bool> AcademicLevelBelongsToSchoolAsync(Guid academicLevelId, Guid schoolId)
        {
            return await _context.AcademicLevels.AnyAsync(l => l.AcademicLevelID == academicLevelId && l.SchoolID == schoolId);
        }

        public async Task<Guid?> GetOverrideSchoolIdAsync(Guid overrideId)
        {
            var entity = await _context.TimetableOverrides.FirstOrDefaultAsync(o => o.TimetableOverrideID == overrideId);
            if (entity?.AcademicLevelID is null) return null;

            var level = await _context.AcademicLevels.FindAsync(entity.AcademicLevelID.Value);
            return level?.SchoolID;
        }

        private async Task<(int? AcademicLevel, string? GradeSection)> ResolveLegacyFieldsAsync(Guid academicLevelId, Guid levelSectionId)
        {
            var level = await _context.AcademicLevels.FindAsync(academicLevelId);
            var section = await _context.LevelSections.FindAsync(levelSectionId);
            return (level?.LevelInt, section?.SectionCode);
        }

        // TimetableOverrides stores these as date columns mapped to DateTime; the DTO uses DateOnly.
        private static DateOnly? ToDateOnly(DateTime? value) => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

        private static DateTime? ToDateTime(DateOnly? value) => value?.ToDateTime(TimeOnly.MinValue);
    }
}
