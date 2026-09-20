using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels.DTOs
{
    public class FinanceLedgerRequestDTO
    {
        public Guid SchoolID { get; set; }
        public DateTime TermStart { get; set; }
        public DateTime TermEnd { get; set; }
        public Guid StudentFinanceID { get; set; }


    }
}
