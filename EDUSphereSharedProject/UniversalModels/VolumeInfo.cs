using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class VolumeInfo
    {
        public string Title { get; set; }
        public string[] Authors { get; set; }
        public string Publisher { get; set; }
        public string Description { get; set; }
        public ImageLinks ImageLinks { get; set; } 

        public string PublishedDate { get; set; }

        public string PreviewLink { get; set; }
        public List<IndustryIdentifier> IndustryIdentifiers { get; set; } 
    }
}
