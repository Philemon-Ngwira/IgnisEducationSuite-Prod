using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels
{
    public partial class Payment
    {
        [NotMapped]
        public string InvoiceType { get; set; } = "";
    }
}
