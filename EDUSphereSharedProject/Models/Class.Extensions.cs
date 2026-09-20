using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models
{
    public partial class Class
    {
        [NotMapped]
        public bool HasCustomPeriods { get; set; } = false;
        [NotMapped]
        public bool IsCore { get; set; }
    }
}
