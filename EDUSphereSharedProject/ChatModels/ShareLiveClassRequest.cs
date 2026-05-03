using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.ChatModels
{
    public class ShareLiveClassRequest
    {
        public string Grade { get; set; }         = "";
        public string Subject { get; set; }       = "";
        public string TeacherId { get; set; }     = "";
        public string MeetingLink { get; set; }   = "";
        public string GradeSection { get; set; }  = "";
        public DateTime StartTime { get; set; } = DateTime.UtcNow;


    }
}
