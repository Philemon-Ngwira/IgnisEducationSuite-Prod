using EDUSphereSharedProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class UploadPreviewResult
    {
        public List<Teacher> TeacherPreview { get; set; } = new();
        public List<Student> Preview { get; set; } = new();
        public List<Parent> ParentPreview { get; set; } = new();
        public List<RowError> Errors { get; set; } = new();
    }
}
