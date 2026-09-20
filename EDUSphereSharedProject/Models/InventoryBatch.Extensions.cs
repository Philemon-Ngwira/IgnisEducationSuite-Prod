using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models
{
    public partial class InventoryBatch
    {
        [NotMapped]
        public string FoodName { get; set; } = "";
        [NotMapped] public string Unit { get; set; } = string.Empty;

        [NotMapped]
        public DateTime? RecievedDateAltered { get; set; }
        [NotMapped]
        public bool hasExpiry { get; set; }
        [NotMapped]
        public int ShelfLifeDays { get; set; }
        [NotMapped]
        public string Category { get; set; } = string.Empty;
    }
}
