using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels
{
    public partial class FinanceLedger
    {
        [NotMapped]
        public decimal RunningBalance { get; set; } = 0;
    }
}
