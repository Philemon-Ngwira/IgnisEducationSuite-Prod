using EDUSphereSharedProject.AchievementModels;

namespace IgnisEducationSuite.Client.Pages.Achievements.Interfaces
{
    public interface IUserActivityService
    {
        Task LogActivityAsync(UserActivity userActivity, Activity doneActivity);
        Task SaveNewUserActivitiesAsync();
        Task<List<UserActivity>> GetUserActivitiesAsync(string userId);
    }
}
