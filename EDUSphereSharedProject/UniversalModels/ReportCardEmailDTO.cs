using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class ReportCardEmailDTO
    {
        public string EmailTo { get; set; }
        public byte[] PdfBytes { get; set; }
    }
}
