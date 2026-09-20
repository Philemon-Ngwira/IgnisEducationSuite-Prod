using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models.StoreProModels
{
    public class DiningMenuDTO
    {
        public Guid MenuId { get; set; }
        public Guid MealId { get; set; }
        public string DayOfWeek { get; set; }   = string.Empty;
        public string MainDish { get; set; } = string.Empty;
        public string SideDish { get; set; } = string.Empty;
        public string Drink { get; set; } = string.Empty;
        public string MealName { get; set; } = string.Empty;
    }
}
