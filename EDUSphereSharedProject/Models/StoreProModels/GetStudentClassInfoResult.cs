using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models.StoreProModels
{
    public partial class GetStudentClassInfoResult
    {
        public Guid StudentClassID { get; set; }
        public Guid? StudentID { get; set; }
        public Guid? ClassID { get; set; }
        public string ClassName { get; set; }
        public string Teacher { get; set; }
    }
}
