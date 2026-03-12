using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels.DTOs
{
    public class MonthlyPaymentTrend
    {
        public string Month { get; set; }
        [Column(TypeName = "decimal(38,2)")]
        public decimal Amount { get; set; }
    }
}
