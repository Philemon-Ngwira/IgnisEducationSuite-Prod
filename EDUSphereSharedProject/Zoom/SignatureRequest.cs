using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Zoom
{
    public class SignatureRequest
    {
        public string SdkKey { get; set; }
        public string SdkSecret { get; set; }
        public string MeetingNumber { get; set; }
        public int Role { get; set; } // 0 = attendee, 1 = host
    }
}
