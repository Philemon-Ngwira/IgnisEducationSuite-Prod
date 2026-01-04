using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models
{
    public partial class Lesson
    {
        [NotMapped]
        public int LessonCompleted { get; set; }
        [NotMapped]
        public string imageUrl { get; set; }
        [NotMapped]
        public Guid StudentID { get; set; }
        [NotMapped]
        public Guid SchoolID { get; set; }
    }
}
