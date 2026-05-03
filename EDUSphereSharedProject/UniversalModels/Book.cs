using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class Book
    {
        public string Id { get; set; } = "";
        public VolumeInfo VolumeInfo { get; set; } = new VolumeInfo();
        public AccessInfo AccessInfo { get; set; } = new AccessInfo();

        [NotMapped]
        public string DefaultImg { get; set; } = "";
    }
}
