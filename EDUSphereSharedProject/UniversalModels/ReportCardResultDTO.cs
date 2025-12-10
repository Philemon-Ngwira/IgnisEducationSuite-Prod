using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class ReportCardResultDTO
    {
        public string ClassName { get; set; }
        public double? Score { get; set; }
        public string Grade { get; set; }
        public string Final { get; set; }
    }
}
