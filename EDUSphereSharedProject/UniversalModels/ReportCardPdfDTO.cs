using EDUSphereSharedProject.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class ReportCardPdfDTO
    {
        // Student info
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public DateTime? IssuedDate { get; set; }
        public decimal? GPA { get; set; }

        // School info
        public string SchoolName { get; set; } = string.Empty;
        public string SchoolLogo { get; set; } = string.Empty;
        public string SchoolEmail { get; set; } = string.Empty;
        public string SchoolWebsite { get; set; } = string.Empty;
        public string SchoolPhone { get; set; } = string.Empty;

        // Report card results
        public List<ReportCardResultDTO> Results { get; set; } = new();

        // Attendance summary
        public List<AttendanceDTO> Attendances { get; set; } = new();

        // Summary / Top Six fields
        public int? PointsInBestSix { get; set; }
        public int? MarksInBestSix { get; set; }
        public int? PositionInClass { get; set; }

        // Dean / Principal comments
        public string DeanName { get; set; } = string.Empty;
        public string DeansComment { get; set; } = string.Empty;
        public string PrincipleName { get; set; } = string.Empty;
        public string PrinciplesComment { get; set; } = string.Empty;

        // Other metadata
        public string Term { get; set; } = string.Empty;
        public string ReportCardType { get; set; } = string.Empty;

        public bool isGCE { get; set; }

    }
}
