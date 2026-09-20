using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models.StoreProModels
{
    public class MaintainanceRequestsDTO
    {
        public Guid RequestID { get; set; }
        public Guid HostelID { get; set; }
        public Guid? RoomID { get; set; }
        public string ProblemDescription { get; set; } = string.Empty;
        public string ReportedBy { get; set; } = string.Empty;
        public DateTime? DateReported { get; set; }
        public DateTime? DateResolved { get; set; }
        public string Status { get; set; } = string.Empty;
        public string HostelName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
    }
}
