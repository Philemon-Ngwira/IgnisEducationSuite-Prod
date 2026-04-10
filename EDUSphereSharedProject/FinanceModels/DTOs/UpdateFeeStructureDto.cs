using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels.DTOs
{
    public class UpdateFeeStructureDto
    {
        public FeeStructure FeeStructure { get; set; } = new();
        public List<FeeStructureItem> Added { get; set; } = new();
        public List<FeeStructureItem> Updated { get; set; } = new();
        public List<Guid> Deleted { get; set; } = new();
    }
}
