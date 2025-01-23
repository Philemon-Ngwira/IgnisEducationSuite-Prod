using EDUSphereSharedProject.AchievementModels;
using IgnisEducationSuite.Client.Pages.Achievements.Interfaces;
using IgnisEducationSuite.Client.Services;

namespace IgnisEducationSuite.Client.Pages.Achievements.Services
{
    public class ActivityService : IActivityService
    {
        private readonly GenericServiceFactory _genericService;
        public ActivityService(GenericServiceFactory genericServiceFactory)
        {
            _genericService = genericServiceFactory;
        }
        public async Task<Activity> GetActivityByNameAsync(string activityName)
        {
            Activity selectedActivity = new();
            var service = _genericService.GetService<Activity>();
            var result = await service.GetAllAsync("api/Dynamic/GetSystemActivities", true);
            if (result.IsSuccess)
            {
                var activity = result.Data.Where(x => x.Activity1 == activityName).FirstOrDefault();
                selectedActivity = activity;
            }
            return selectedActivity;
        }
    }
}
