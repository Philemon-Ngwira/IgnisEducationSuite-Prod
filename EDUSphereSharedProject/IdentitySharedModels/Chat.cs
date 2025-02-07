using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.IdentitySharedModels
{
    public class Chat
    {
        public string Name { get; set; }
        public bool IsGroup { get; set; }
        public byte[] ProfilePic { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageTimestamp { get; set; }
        [NotMapped]
        public string UserName { get; set; }
        [NotMapped]
        public string id { get; set; }
        [NotMapped]
        public string Reciepientid { get; set; }
        [NotMapped]
        public string Userid { get; set; }
    }
}
