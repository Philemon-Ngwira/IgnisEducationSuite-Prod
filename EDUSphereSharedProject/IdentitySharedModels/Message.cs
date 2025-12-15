using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.IdentitySharedModels
{
    public class Message
    {
        public string Content { get; set; }
        public string UserId { get; set; }
        public string RecipientId { get; set; }
        public string GroupName { get; set; }
        public DateTime? Timestamp { get; set; }

        public string GroupIdentifier { get; set; }
    }
}
