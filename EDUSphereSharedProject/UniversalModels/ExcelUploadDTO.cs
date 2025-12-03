using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class ExcelUploadDTO
    {
        public MultipartFormDataContent File { get; set; }
        public string SchoolID { get; set; }
    }
}
