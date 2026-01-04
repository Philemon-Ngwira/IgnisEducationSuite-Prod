using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models
{
    public partial class TimeTableActivity
    {
        [NotMapped]
        public List<string> CannotFollow { get; set; } = new();
        [NotMapped]
        public bool MustRespectTime { get; set; }
        [NotMapped]
        public TimeSpan? AfternoonStart { get; set; }
        [NotMapped]
        public TimeSpan StartFrom { get; set; }
    }
}
