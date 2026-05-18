using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduSphereDomain.Repositories
{
    public interface ITeacherAvailabilityProvider
    {
        Task<HashSet<(DayOfWeek Day, TimeSpan StartTime)>> GetBusySlots(Guid teacherId, Guid schoolId);
    }
}
