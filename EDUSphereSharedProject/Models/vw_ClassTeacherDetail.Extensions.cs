using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models
{
    public partial class vw_ClassTeacherDetail
    {
        [NotMapped]
        public string LevelName { get; set; }
        [NotMapped]
        public string sectionName { get; set; } 
    }
}
