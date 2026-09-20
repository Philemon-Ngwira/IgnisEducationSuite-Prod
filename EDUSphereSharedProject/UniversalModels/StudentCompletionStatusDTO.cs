using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class StudentCompletionStatusDTO
    {
        public Guid StudentID { get; set; }
        public bool IsCompleted { get; set; }
    }
}
