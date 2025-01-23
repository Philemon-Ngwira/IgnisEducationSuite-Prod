using EDUSphereSharedProject.AchievementModels;

namespace IgnisEducationSuite.Client.Pages.Achievements.Interfaces
{
    public interface IBadgeService
    {
        Task<List<Badge>> GetBadgesAsync();
        Task AwardBadgeAsync(string userId, Badge badge, UserActivity userActivity);
    }
}
