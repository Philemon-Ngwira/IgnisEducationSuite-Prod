using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class SubjectStructureConstraints
    {
        public Guid SubjectId { get; set; }

        public int WeeklyPeriods { get; set; }


        // Optional caps
        public int MaxPeriodsPerDay { get; set; } = int.MaxValue;

        // Subjects this subject may NOT follow
        public List<Guid> CannotFollowSubjects { get; set; } = new();
        public int RequiredDoubleCount { get; set; }
        

    }

}
