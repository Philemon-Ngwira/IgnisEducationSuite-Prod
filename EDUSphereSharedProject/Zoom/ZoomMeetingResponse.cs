using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Zoom
{
    public class ZoomMeetingResponse
    {
        public long id { get; set; }          // Zoom meeting ID
        public string JoinUrl { get; set; }     // Zoom join link
        public string Password { get; set; }    // Generated meeting password
        public string MeetingNumber { get; set; } // Zoom meeting number

        public string CreatorEmail { get; set; } // Email of the meeting creator
    }

}
