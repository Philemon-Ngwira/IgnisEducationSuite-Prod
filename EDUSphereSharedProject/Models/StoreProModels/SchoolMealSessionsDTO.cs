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
        public string Name { get; set; }
        public string DiningHallName { get; set; }
        public TimeSpan TimeStart { get; set; }
        public TimeSpan TimeEnd { get; set; }
    }
}
