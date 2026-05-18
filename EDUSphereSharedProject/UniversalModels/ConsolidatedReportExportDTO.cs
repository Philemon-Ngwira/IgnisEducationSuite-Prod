using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class ConsolidatedReportExportDTO
    {

        public string StudentName { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string GradeSection { get; set; } = string.Empty;
        public string Points { get; set; } = string.Empty;
        public string PositionInClass { get; set; } = string.Empty;
        public string SubjectList { get; set; } = string.Empty;
    }
}
