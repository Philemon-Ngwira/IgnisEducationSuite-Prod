using EDUSphereSharedProject.AchievementModels;
using IgnisEducationSuite.Client.Pages.Achievements.Interfaces;
using IgnisEducationSuite.Client.Services;

namespace IgnisEducationSuite.Client.Pages.Achievements.Services
{
    public class BadgeService : IBadgeService
    {
        private readonly GenericServiceFactory _genericService;
        private readonly IUserActivityService _userActivityService;
        private readonly IActivityService _activityService;
        private readonly List<UserBadge> _userBadges;
        public BadgeService(GenericServiceFactory genericService, IUserActivityService userActivityService, IActivityService activityService)
        {
            _genericService = genericService;
            _userActivityService = userActivityService;
            _activityService = activityService;
            _userBadges = new List<UserBadge>();
        }
        public async Task<List<Badge>> GetBadgesAsync()
        { // Logic to get badges (e.g., from the database)
            var service = _genericService.GetService<Badge>();
            var result = await service.GetAllAsync("api/Dynamic/GetAllSystemBadges", true);
            if (result.IsSuccess)
            {
                var badges = result.Data.ToList();
                return badges;
            }
            return new List<Badge>();
        }
        public async Task AwardBadgeAsync(string userId, Badge badge, UserActivity userActivity)
        {
            bool alreadyHasBadge = _userBadges.Any(x => x.BadgeID == badge.BadgeId);
            if (alreadyHasBadge)
            {
                Console.WriteLine($"User {userId} already has badge {badge.BadgeId}");
                return;
            }
            UserBadge newUserBadge = new UserBadge
            {
                StudentID = Guid.Parse(userId),
                BadgeID = badge.BadgeId,
                DateEarned = DateTime.Now,
                UserBadgeID = Guid.NewGuid(),
            };
            Activity doneActivity = await _activityService.GetActivityByNameAsync(badge.BadgeName);
            await _userActivityService.LogActivityAsync(userActivity, doneActivity);
            var service = _genericService.GetService<UserBadge>();
            if (service != null)
            {
                var result = await service.PostAsync("api/Dynamic/SaveNewUserBadge", "userbadge", newUserBadge);
                if (result.IsSuccess)
                {
                    Console.WriteLine($"Badge {badge.BadgeId} awarded to user {userId}");
                }
                else
                {
                    Console.WriteLine($"Failed to award badge {badge.BadgeId} to user {userId}: {result.ErrorMessage}");
                }
            }
            else
            {
                Console.WriteLine("Service for UserBadge is null");
            }
        }
    }
}
