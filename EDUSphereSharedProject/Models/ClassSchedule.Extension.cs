using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models
{
    public partial class ClassSchedule
    {
        [NotMapped]
        public string DisplayLabel
        {
            get
            {
                if (Class != null)
                    return Class.ClassName;
                if (ScheduledActivityNavigation != null)
                    return ScheduledActivityNavigation.ActivityName;
                return "Inactive";
            }
        }
    }
}
