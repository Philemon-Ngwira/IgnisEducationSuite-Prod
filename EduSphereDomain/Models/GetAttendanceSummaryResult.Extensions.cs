using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduSphereDomain.Models
{
    public partial class GetAttendanceSummaryResult
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Term { get; set; }
        public decimal? GPA { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime TermStartDate { get; set; }
        public DateTime TermEndDate { get; set; }
        public int GradeLevel { get; set; }
        public int ExpectedAttendances { get; set; }
        public int AttendanceCount { get; set; }
    }
}
