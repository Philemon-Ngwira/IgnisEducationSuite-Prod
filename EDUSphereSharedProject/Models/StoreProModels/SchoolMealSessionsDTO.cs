using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models.StoreProModels
{
    public class SchoolMealSessionsDTO
    {
        public Guid MealId { get; set; }
        public Guid DiningHallId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DiningHallName { get; set; } = string.Empty;
        public TimeSpan TimeStart { get; set; }
        public TimeSpan TimeEnd { get; set; }
    }
}
