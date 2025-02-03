using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class GoogleBooksResponse
    {
        public int TotalItems { get; set; }
        public Book[]? Items { get; set; }
    }
}
