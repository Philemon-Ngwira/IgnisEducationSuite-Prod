using EDUSphereSharedProject.AchievementModels;

namespace IgnisEducationSuite.Client.Pages.Achievements.Interfaces
{
    public interface IBadgeCriteriaService
    {
        Task<bool> CheckBadgeCriteriaAsync(string userID, Badge badge, int? NumberOfLessons = 0);
        bool HasConsistentDailyLessons(string userID, List<UserActivity> userActivities);
        bool HasConsistentHighScores(string userID, string activityType, decimal threshold, int days, List<UserActivity> userActivities);
        bool HasEarnedBadgesInAllCategories(string userID, List<UserActivity> userActivities);
        bool HasSpentHoursLearning(string userID, int hours, List<UserActivity> userActivities);
        bool HasAchievedGoals(string userID, int goals, List<UserActivity> userActivities);
        bool HasEarnedHighestBadgesInAllCategories(string userID, List<UserActivity> userActivities);
    }
}
