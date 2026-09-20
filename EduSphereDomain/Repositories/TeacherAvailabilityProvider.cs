using EduSphereDomain.FinanceData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduSphereDomain.Repositories
{
    public class TeacherAvailabilityProvider : ITeacherAvailabilityProvider
    {
        private readonly PhoenixEdusphereFinanceContextProcedures _procedures;

        public TeacherAvailabilityProvider(PhoenixEdusphereFinanceContextProcedures context)
        {
            _procedures = context;
        }

        #region TimeTableGenerator Integration
        // Maps DayName strings from DB to C# DayOfWeek enum
        private static readonly Dictionary<string, DayOfWeek> DayNameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Monday"] = DayOfWeek.Monday,
            ["Tuesday"] = DayOfWeek.Tuesday,
            ["Wednesday"] = DayOfWeek.Wednesday,
            ["Thursday"] = DayOfWeek.Thursday,
            ["Friday"] = DayOfWeek.Friday,
            ["Saturday"] = DayOfWeek.Saturday,
            ["Sunday"] = DayOfWeek.Sunday
        };
        public async Task<HashSet<(DayOfWeek Day, TimeSpan StartTime)>> GetBusySlots(
        Guid teacherId, Guid schoolId)
        {
            var rows = await _procedures.usp_GetTeacherBusySlotsAsync(teacherId, schoolId);

            var result = new HashSet<(DayOfWeek, TimeSpan)>();

            foreach (var row in rows)
            {
                if (row.StartTime == null || row.DayName == null)
                    continue;

                if (DayNameMap.TryGetValue(row.DayName, out var dayOfWeek))
                    result.Add((dayOfWeek, row.StartTime.Value));
            }

            return result;
        }
        #endregion
    }
}
