using EDUSphereSharedProject.AchievementModels;
using IgnisEducationSuite.Client.Pages.Achievements.Interfaces;
using IgnisEducationSuite.Client.Services;

namespace IgnisEducationSuite.Client.Pages.Achievements.Services
{
    public class UserActivityService : IUserActivityService
    {
        private readonly GenericServiceFactory _genericService;
        private readonly List<UserActivity> _userActivities;
        private readonly List<UserActivity> _newUserActivities;
        public UserActivityService(GenericServiceFactory genericService)
        {
            _genericService = genericService;
            _userActivities = new List<UserActivity>();
            _newUserActivities = new List<UserActivity>();
        }
        public async Task LogActivityAsync(UserActivity userActivity, Activity doneActivity)
        {

            if (doneActivity == null)
            {

            }
            else
            {
                userActivity.Score = doneActivity.Points;
                userActivity.RelatedActivity = doneActivity.ActivityID;
            }
            _newUserActivities.Add(userActivity);
        }
        public async Task SaveNewUserActivitiesAsync()
        {
            var service = _genericService.GetService<UserActivity>();
            if (service != null)
            {
                foreach (var activity in _newUserActivities)
                {
                    var result = await service.PostAsync("api/Dynamic/SaveNewUserActivity", "useractivity", activity);
                    if (!result.IsSuccess)
                    {
                        //Console.WriteLine($"Failed to save activity {activity.ActivityId}: {result.ErrorMessage}");
                    }
                }
                _newUserActivities.Clear();
            }
            else
            {
                Console.WriteLine("Service for UserActivity is null");
            }
        }
        public async Task<List<UserActivity>> GetUserActivitiesAsync(string userId)
        { 

            var service = _genericService.GetService<UserActivity>();
            var result = await service.GetAllAsync($"api/Dynamic/GetAllUserActivities/{userId}", true);
            if (result.IsSuccess)
            {
                return result.Data.ToList();
            }
            else
            {
                return new List<UserActivity>();
            }

        }
    }
}
