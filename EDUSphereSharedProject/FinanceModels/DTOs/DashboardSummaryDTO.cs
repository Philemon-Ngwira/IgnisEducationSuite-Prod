using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels.DTOs
{
    public class DashboardSummaryDTO
    {
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalInvoiced { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalCollected { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OutstandingBalance { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PaymentsToday { get; set; }
        public int? OverdueInvoices { get; set; }
    }
}
