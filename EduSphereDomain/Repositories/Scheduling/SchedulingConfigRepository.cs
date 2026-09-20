using EduSphereDomain.Data;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using Microsoft.EntityFrameworkCore;

// The retired generator's DTO of the same name still lives in the TimeTabling namespace until its
// last consumers (the old Smart generator page and the SchedulingTester harness) are removed.
using SubjectScheduleConfigEntity = EDUSphereSharedProject.Models.SubjectScheduleConfig;

namespace EduSphereDomain.Repositories.Scheduling
{
    public class SchedulingConfigRepository : ISchedulingConfigRepository
    {
        private readonly PhoenixEdusphereContext _context;

        public SchedulingConfigRepository(PhoenixEdusphereContext context)
        {
            _context = context;
        }

        // ---------- Reference data ----------

        private static readonly string[] WeekdayOrder =
            { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        public async Task<List<DayOptionDto>> ListDaysAsync()
        {
            var days = await _context.DayofTheWeeks.ToListAsync();

            return days
                .OrderBy(d => Array.IndexOf(WeekdayOrder, d.DayName) is var i && i >= 0 ? i : int.MaxValue)
                .Select(d => new DayOptionDto { DayId = d.DayID, DayName = d.DayName })
                .ToList();
        }

        // ---------- Timetable Activities ----------

        public async Task<List<ActivityDto>> ListActivitiesAsync(Guid schoolId)
        {
            var activities = await _context.TimeTableActivities
                .Where(a => a.SchoolID == schoolId)
                .OrderBy(a => a.ActivityName)
                .ToListAsync();

            var (slots, days) = await LoadPreferredLookupsAsync(activities);
            return activities.Select(a => MapActivity(a, slots, days)).ToList();
        }

        public async Task<ActivityDto?> GetActivityAsync(Guid activityId)
        {
            var activity = await _context.TimeTableActivities.FirstOrDefaultAsync(a => a.ActivityID == activityId);
            if (activity is null) return null;

            var (slots, days) = await LoadPreferredLookupsAsync(new[] { activity });
            return MapActivity(activity, slots, days);
        }

        public async Task<Guid> CreateActivityAsync(Guid schoolId, ActivityRequest request)
        {
            var activity = new TimeTableActivity
            {
                ActivityID = Guid.NewGuid(),
                SchoolID = schoolId,
                ActivityName = request.ActivityName,
                DefaultDuration = request.DefaultDuration,
                OptionalNotes = request.OptionalNotes,
                MustBeMorning = request.MustBeMorning,
                MustBeAfternoon = request.MustBeAfternoon,
                PreferredTimeSlotID = request.PreferredTimeSlotId,
                PreferredDayID = request.PreferredDayId,
            };

            _context.TimeTableActivities.Add(activity);
            await _context.SaveChangesAsync();

            return activity.ActivityID;
        }

        public async Task<bool> UpdateActivityAsync(Guid activityId, ActivityRequest request)
        {
            var activity = await _context.TimeTableActivities.FirstOrDefaultAsync(a => a.ActivityID == activityId);
            if (activity is null) return false;

            activity.ActivityName = request.ActivityName;
            activity.DefaultDuration = request.DefaultDuration;
            activity.OptionalNotes = request.OptionalNotes;
            activity.MustBeMorning = request.MustBeMorning;
            activity.MustBeAfternoon = request.MustBeAfternoon;
            activity.PreferredTimeSlotID = request.PreferredTimeSlotId;
            activity.PreferredDayID = request.PreferredDayId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteActivityAsync(Guid activityId)
        {
            var activity = await _context.TimeTableActivities.FirstOrDefaultAsync(a => a.ActivityID == activityId);
            if (activity is null) return false;

            _context.TimeTableActivities.Remove(activity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActivityHasDependenciesAsync(Guid activityId)
        {
            return await _context.ClassSchedules.AnyAsync(c => c.ScheduledActivity == activityId);
        }

        private async Task<(Dictionary<Guid, TimeSlot> Slots, Dictionary<Guid, DayofTheWeek> Days)>
            LoadPreferredLookupsAsync(IEnumerable<TimeTableActivity> activities)
        {
            var slotIds = activities.Where(a => a.PreferredTimeSlotID.HasValue).Select(a => a.PreferredTimeSlotID!.Value).Distinct().ToList();
            var dayIds = activities.Where(a => a.PreferredDayID.HasValue).Select(a => a.PreferredDayID!.Value).Distinct().ToList();

            var slots = slotIds.Count == 0
                ? new Dictionary<Guid, TimeSlot>()
                : await _context.TimeSlots.Where(s => slotIds.Contains(s.TimeslotID)).ToDictionaryAsync(s => s.TimeslotID);

            var days = dayIds.Count == 0
                ? new Dictionary<Guid, DayofTheWeek>()
                : await _context.DayofTheWeeks.Where(d => dayIds.Contains(d.DayID)).ToDictionaryAsync(d => d.DayID);

            return (slots, days);
        }

        private static ActivityDto MapActivity(
            TimeTableActivity activity,
            Dictionary<Guid, TimeSlot> slots,
            Dictionary<Guid, DayofTheWeek> days)
        {
            TimeSlot? slot = activity.PreferredTimeSlotID.HasValue && slots.TryGetValue(activity.PreferredTimeSlotID.Value, out var s) ? s : null;
            DayofTheWeek? day = activity.PreferredDayID.HasValue && days.TryGetValue(activity.PreferredDayID.Value, out var d) ? d : null;

            return new ActivityDto
            {
                ActivityId = activity.ActivityID,
                SchoolId = activity.SchoolID,
                ActivityName = activity.ActivityName,
                DefaultDuration = activity.DefaultDuration,
                OptionalNotes = activity.OptionalNotes,
                MustBeMorning = activity.MustBeMorning,
                MustBeAfternoon = activity.MustBeAfternoon,
                PreferredTimeSlotId = activity.PreferredTimeSlotID,
                PreferredTimeSlotLabel = slot is null ? null : $"{slot.StartTime:hh\\:mm} - {slot.EndTime:hh\\:mm}",
                PreferredDayId = activity.PreferredDayID,
                PreferredDayName = day?.DayName,
            };
        }

        // ---------- Time Slots ----------

        public async Task<List<TimeSlotDto>> ListTimeSlotsAsync(Guid schoolId)
        {
            var slots = await _context.TimeSlots
                .Where(s => s.SchoolID == schoolId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            return slots.Select(MapTimeSlot).ToList();
        }

        public async Task<TimeSlotDto?> GetTimeSlotAsync(Guid timeslotId)
        {
            var slot = await _context.TimeSlots.FirstOrDefaultAsync(s => s.TimeslotID == timeslotId);
            return slot is null ? null : MapTimeSlot(slot);
        }

        public async Task<Guid> CreateTimeSlotAsync(Guid schoolId, TimeSlotRequest request)
        {
            var slot = new TimeSlot
            {
                TimeslotID = Guid.NewGuid(),
                SchoolID = schoolId,
                StartTime = request.StartTime.ToTimeSpan(),
                EndTime = request.EndTime.ToTimeSpan(),
                Description = request.Description,
                SlotType = request.SlotType,
                MaxOccupancy = request.MaxOccupancy,
            };

            _context.TimeSlots.Add(slot);
            await _context.SaveChangesAsync();

            return slot.TimeslotID;
        }

        public async Task<bool> UpdateTimeSlotAsync(Guid timeslotId, TimeSlotRequest request)
        {
            var slot = await _context.TimeSlots.FirstOrDefaultAsync(s => s.TimeslotID == timeslotId);
            if (slot is null) return false;

            slot.StartTime = request.StartTime.ToTimeSpan();
            slot.EndTime = request.EndTime.ToTimeSpan();
            slot.Description = request.Description;
            slot.SlotType = request.SlotType;
            slot.MaxOccupancy = request.MaxOccupancy;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTimeSlotAsync(Guid timeslotId)
        {
            var slot = await _context.TimeSlots.FirstOrDefaultAsync(s => s.TimeslotID == timeslotId);
            if (slot is null) return false;

            _context.TimeSlots.Remove(slot);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TimeSlotOverlapsAsync(Guid schoolId, TimeOnly start, TimeOnly end, Guid? excludingTimeslotId)
        {
            var startSpan = start.ToTimeSpan();
            var endSpan = end.ToTimeSpan();

            return await _context.TimeSlots.AnyAsync(s =>
                s.SchoolID == schoolId &&
                (!excludingTimeslotId.HasValue || s.TimeslotID != excludingTimeslotId.Value) &&
                s.StartTime.HasValue && s.EndTime.HasValue &&
                s.StartTime.Value < endSpan && startSpan < s.EndTime.Value);
        }

        /// <summary>
        /// A time slot in use by a saved schedule or referenced as an activity's preferred slot
        /// cannot be deleted — ClassSchedule.TimeSlotID has no cascade, so the delete would fail at
        /// the database with a constraint error rather than a readable message.
        /// </summary>
        public async Task<bool> TimeSlotHasDependenciesAsync(Guid timeslotId)
        {
            if (await _context.ClassSchedules.AnyAsync(c => c.TimeSlotID == timeslotId)) return true;
            if (await _context.TimeTableActivities.AnyAsync(a => a.PreferredTimeSlotID == timeslotId)) return true;
            return await _context.TimetableOverrides.AnyAsync(o => o.TimeSlotID == timeslotId);
        }

        // ---------- Subject scheduling policy ----------

        public async Task<List<SubjectScheduleConfigDto>> ListPolicyAsync(Guid schoolId)
        {
            // Left-join from Classes so every subject shows up, even ones not configured yet —
            // the admin needs to see which subjects still need policy set, not just the ones that do.
            //
            // notRequired classes are excluded outright: the AFTER INSERT trigger
            // trg_BlockNotRequiredClassSchedule rolls back the whole save and throws 50001 if one
            // ever reaches ClassSchedule, so they must never enter the candidate pool.
            var classes = await _context.Classes
                .Include(c => c.Teacher)
                .Where(c => c.SChoolID == schoolId && (c.notRequired == null || c.notRequired == false))
                .ToListAsync();

            var configs = await _context.SubjectScheduleConfigs
                .Where(c => c.SchoolID == schoolId)
                .ToListAsync();

            var configByClassId = configs.ToDictionary(c => c.ClassID);

            return classes.Select(c => MapPolicy(c, configByClassId.TryGetValue(c.ClassID, out var config) ? config : null)).ToList();
        }

        public async Task<SubjectScheduleConfigDto?> GetPolicyAsync(Guid classId)
        {
            var cls = await _context.Classes.Include(c => c.Teacher).FirstOrDefaultAsync(c => c.ClassID == classId);
            if (cls is null) return null;

            var config = await _context.SubjectScheduleConfigs.FirstOrDefaultAsync(c => c.ClassID == classId);
            return MapPolicy(cls, config);
        }

        public async Task UpsertPolicyAsync(Guid classId, Guid schoolId, SubjectScheduleConfigRequest request)
        {
            var config = await _context.SubjectScheduleConfigs.FirstOrDefaultAsync(c => c.ClassID == classId);

            if (config is null)
            {
                config = new SubjectScheduleConfigEntity
                {
                    SubjectScheduleConfigID = Guid.NewGuid(),
                    ClassID = classId,
                    SchoolID = schoolId,
                    CreatedAt = DateTime.UtcNow,
                };
                _context.SubjectScheduleConfigs.Add(config);
            }
            else
            {
                config.ModifiedAt = DateTime.UtcNow;
            }

            config.IsCore = request.IsCore;
            config.WeeklyPeriods = request.WeeklyPeriods;
            config.RequiredDoubles = request.RequiredDoubles;
            config.TimePreference = (byte)request.TimePreference;

            await _context.SaveChangesAsync();
        }

        public async Task<int> UpsertPolicyBulkAsync(Guid schoolId, List<SubjectSchedulePolicyItem> items)
        {
            if (items.Count == 0) return 0;

            var classIds = items.Select(i => i.ClassId).Distinct().ToList();

            var existing = await _context.SubjectScheduleConfigs
                .Where(c => classIds.Contains(c.ClassID))
                .ToListAsync();

            var byClassId = existing.ToDictionary(c => c.ClassID);
            var now = DateTime.UtcNow;

            foreach (var item in items)
            {
                if (byClassId.TryGetValue(item.ClassId, out var config))
                {
                    config.ModifiedAt = now;
                }
                else
                {
                    config = new SubjectScheduleConfigEntity
                    {
                        SubjectScheduleConfigID = Guid.NewGuid(),
                        ClassID = item.ClassId,
                        SchoolID = schoolId,
                        CreatedAt = now,
                    };
                    _context.SubjectScheduleConfigs.Add(config);
                }

                config.IsCore = item.IsCore;
                config.WeeklyPeriods = item.WeeklyPeriods;
                config.RequiredDoubles = item.RequiredDoubles;
                config.TimePreference = (byte)item.TimePreference;
            }

            await _context.SaveChangesAsync();
            return items.Count;
        }

        public async Task<HashSet<Guid>> FilterClassIdsInSchoolAsync(Guid schoolId, List<Guid> classIds)
        {
            if (classIds.Count == 0) return new HashSet<Guid>();

            var found = await _context.Classes
                .Where(c => c.SChoolID == schoolId && classIds.Contains(c.ClassID))
                .Select(c => c.ClassID)
                .ToListAsync();

            return found.ToHashSet();
        }

        private static SubjectScheduleConfigDto MapPolicy(Class cls, SubjectScheduleConfigEntity? config) => new()
        {
            ClassId = cls.ClassID,
            SchoolId = cls.SChoolID,
            SubjectName = cls.ClassName,
            LevelName = cls.LevelName,
            AcademicLevel = cls.AcademicLevel,
            GradeSection = cls.GradeSection,
            GroupName = cls.GroupName,
            TeacherId = cls.TeacherID,
            TeacherName = cls.Teacher != null ? $"{cls.Teacher.FirstName} {cls.Teacher.LastName}".Trim() : null,
            IsCore = config?.IsCore ?? false,
            WeeklyPeriods = config?.WeeklyPeriods ?? 0,
            RequiredDoubles = config?.RequiredDoubles ?? 0,
            TimePreference = config is null ? SubjectTimePreference.Any : (SubjectTimePreference)config.TimePreference,
        };

        // ---------- Adjacency ----------

        public async Task<List<SubjectAdjacencyRuleDto>> ListAdjacencyAsync(Guid schoolId)
        {
            var rules = await _context.SubjectAdjacencyRules
                .Include(r => r.Class)
                .Include(r => r.CannotFollowClass)
                .Where(r => r.SchoolID == schoolId)
                .ToListAsync();

            return rules.Select(MapAdjacency).ToList();
        }

        public async Task<SubjectAdjacencyRuleDto?> GetAdjacencyRuleAsync(Guid subjectAdjacencyRuleId)
        {
            var rule = await _context.SubjectAdjacencyRules
                .Include(x => x.Class)
                .Include(x => x.CannotFollowClass)
                .FirstOrDefaultAsync(x => x.SubjectAdjacencyRuleID == subjectAdjacencyRuleId);

            return rule is null ? null : MapAdjacency(rule);
        }

        public async Task<bool> AdjacencyRuleExistsAsync(Guid classId, Guid cannotFollowClassId)
        {
            return await _context.SubjectAdjacencyRules.AnyAsync(r =>
                r.ClassID == classId && r.CannotFollowClassID == cannotFollowClassId);
        }

        public async Task AddAdjacencyRuleAsync(Guid schoolId, Guid classId, Guid cannotFollowClassId)
        {
            _context.SubjectAdjacencyRules.Add(new SubjectAdjacencyRule
            {
                SubjectAdjacencyRuleID = Guid.NewGuid(),
                ClassID = classId,
                CannotFollowClassID = cannotFollowClassId,
                SchoolID = schoolId,
                CreatedAt = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync();
        }

        public async Task<bool> RemoveAdjacencyRuleAsync(Guid subjectAdjacencyRuleId)
        {
            var rule = await _context.SubjectAdjacencyRules.FirstOrDefaultAsync(r => r.SubjectAdjacencyRuleID == subjectAdjacencyRuleId);
            if (rule is null) return false;

            _context.SubjectAdjacencyRules.Remove(rule);
            await _context.SaveChangesAsync();
            return true;
        }

        private static SubjectAdjacencyRuleDto MapAdjacency(SubjectAdjacencyRule r) => new()
        {
            SubjectAdjacencyRuleId = r.SubjectAdjacencyRuleID,
            SchoolId = r.SchoolID,
            ClassId = r.ClassID,
            SubjectName = r.Class?.ClassName,
            CannotFollowClassId = r.CannotFollowClassID,
            CannotFollowSubjectName = r.CannotFollowClass?.ClassName,
        };

        private static TimeSlotDto MapTimeSlot(TimeSlot slot) => new()
        {
            TimeslotId = slot.TimeslotID,
            SchoolId = slot.SchoolID,
            StartTime = slot.StartTime.HasValue ? TimeOnly.FromTimeSpan(slot.StartTime.Value) : null,
            EndTime = slot.EndTime.HasValue ? TimeOnly.FromTimeSpan(slot.EndTime.Value) : null,
            Description = slot.Description,
            SlotType = slot.SlotType,
            MaxOccupancy = slot.MaxOccupancy,
        };
    }
}
