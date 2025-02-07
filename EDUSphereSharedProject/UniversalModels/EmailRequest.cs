using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class EmailRequest
    {
        public string To { get; set; } = string.Empty;    // Recipient's email address
        public string Subject { get; set; } = string.Empty;// Subject of the email
        public string Body { get; set; } = string.Empty;    // Body content of the email (HTML or plain text)
        public string Reciepient { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = false;

        public string UserName { get; set; } = string.Empty;
        public string StudentID { get; set; } = string.Empty;
        public bool isFirstMail { get; set; } = false;
    }
}
