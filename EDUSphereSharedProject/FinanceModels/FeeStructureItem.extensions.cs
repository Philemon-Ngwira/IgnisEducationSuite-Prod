using EDUSphereSharedProject.FinanceModels.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels
{
    public partial class FeeStructureItem
    {
        [NotMapped]
        public StudentType AppliesTo { get; set; }
    }
}
