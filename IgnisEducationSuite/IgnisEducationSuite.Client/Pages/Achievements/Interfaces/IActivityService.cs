using EDUSphereSharedProject.AchievementModels;
using System.Threading.Tasks;

namespace IgnisEducationSuite.Client.Pages.Achievements.Interfaces
{
    public interface IActivityService
    {
        Task<Activity> GetActivityByNameAsync(string activityName);
    }
}
