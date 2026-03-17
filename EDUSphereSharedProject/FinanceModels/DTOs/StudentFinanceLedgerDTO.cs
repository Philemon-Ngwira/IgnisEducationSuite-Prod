using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels.DTOs
{
    public class StudentFinanceLedgerDTO
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string EntryType { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public string InvoiceNumber { get; set; }
        [Column(TypeName = "decimal(38,2)")]
        public decimal? RunningBalance { get; set; }
    }
}
