using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class UserNameRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public int Index { get; set; } = 0;
    }
    public class UsernameResult
    {
        public int Index { get; set; }          // Keeps mapping order
        public string Username { get; set; } = string.Empty;
    }

}
