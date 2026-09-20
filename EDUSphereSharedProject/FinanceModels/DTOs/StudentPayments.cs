using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels.DTOs
{
    public class StudentPayments
    {
        public Guid PaymentId { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public Guid InvoiceId { get; set; }
        public string InvoiceType { get; set; } = "";
        public DateTime TermStartDate { get; set; }
        public DateTime TermEndDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } = "";
        public DateTime PaymentDate { get; set; }
        public DateTime PaymentCreatedAt { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal InvoiceAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal InvoicePaidAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalFees { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingAmount { get; set; }
        public string StudentFinanceStatus { get; set; } = "";
    }
}
