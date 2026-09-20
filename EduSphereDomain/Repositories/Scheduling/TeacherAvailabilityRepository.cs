using EduSphereDomain.Data;
using Microsoft.EntityFrameworkCore;

namespace EduSphereDomain.Repositories.Scheduling
{
    /// <summary>
    /// Reads teacher commitments straight from ClassSchedule rather than through
    /// usp_GetTeacherBusySlots. Two reasons: the stored procedure is only exposed on the Finance
    /// context in this codebase, and doing it here lets the query exclude the section being
    /// regenerated (see ITeacherAvailabilityRepository) and load every teacher in one round trip
    /// instead of one query per teacher.
    /// </summary>
    public class TeacherAvailabilityRepository : ITeacherAvailabilityRepository
    {
        private static readonly Dictionary<string, DayOfWeek> DayNameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Monday"] = DayOfWeek.Monday,
            ["Tuesday"] = DayOfWeek.Tuesday,
            ["Wednesday"] = DayOfWeek.Wednesday,
            ["Thursday"] = DayOfWeek.Thursday,
            ["Friday"] = DayOfWeek.Friday,
            ["Saturday"] = DayOfWeek.Saturday,
            ["Sunday"] = DayOfWeek.Sunday,
        };

        private readonly PhoenixEdusphereContext _context;

        public TeacherAvailabilityRepository(PhoenixEdusphereContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<Guid, HashSet<(DayOfWeek Day, TimeSpan StartTime)>>> GetBusySlotsBySchoolAsync(
            Guid schoolId,
            int? excludeAcademicLevel = null,
            Guid? excludeAcademicLevelSection = null)
        {
            var query =
                from cs in _context.ClassSchedules
                join c in _context.Classes on cs.ClassID equals c.ClassID
                join d in _context.DayofTheWeeks on cs.DayOfTheWeekID equals d.DayID
                join ts in _context.TimeSlots on cs.TimeSlotID equals ts.TimeslotID
                where cs.SchoolID == schoolId
                      && cs.IsActive == true
                      && c.TeacherID != null
                      && ts.StartTime != null
                select new
                {
                    TeacherId = c.TeacherID!.Value,
                    d.DayName,
                    StartTime = ts.StartTime!.Value,
                    cs.AcademicLevel,
                    cs.AcademicLevelSection,
                };

            if (excludeAcademicLevel.HasValue && excludeAcademicLevelSection.HasValue)
            {
                query = query.Where(x =>
                    x.AcademicLevel != excludeAcademicLevel.Value ||
                    x.AcademicLevelSection != excludeAcademicLevelSection.Value);
            }

            var rows = await query.ToListAsync();

            var result = new Dictionary<Guid, HashSet<(DayOfWeek, TimeSpan)>>();

            foreach (var row in rows)
            {
                if (row.DayName is null || !DayNameMap.TryGetValue(row.DayName, out var day)) continue;

                if (!result.TryGetValue(row.TeacherId, out var busy))
                {
                    busy = new HashSet<(DayOfWeek, TimeSpan)>();
                    result[row.TeacherId] = busy;
                }

                busy.Add((day, row.StartTime));
            }

            return result;
        }
    }
}
