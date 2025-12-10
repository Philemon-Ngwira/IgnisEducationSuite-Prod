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
        public string DayOfWeek { get; set; }
        public string MainDish { get; set; }
        public string SideDish { get; set; }
        public string Drink { get; set; }
        public string MealName { get; set; }
    }
}
