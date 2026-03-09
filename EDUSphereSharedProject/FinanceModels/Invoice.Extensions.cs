using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels
{
    public partial class Invoice
    {
        public decimal GetOutstandingAmount()
        {
            return Amount - PaidAmount;
        }
        public bool IsOverdue()
        {
            return DateTime.Now > DueDate && GetOutstandingAmount() > 0;
        }
    }
}
