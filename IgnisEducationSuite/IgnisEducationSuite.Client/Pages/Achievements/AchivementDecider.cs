using EDUSphereSharedProject.AchievementModels;
using IgnisEducationSuite.Client.Pages.Achievements.Interfaces;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IgnisEducationSuite.Client.Pages.Achievements
{
    public class AchievementDecider
    {
        private readonly IBadgeService _badgeService;
        private readonly IUserActivityService _userActivityService;
        private readonly IBadgeCriteriaService _badgeCriteriaService;
        public int NumberOfLessons = 0;
        public AchievementDecider(
            IBadgeService badgeService,
            IUserActivityService userActivityService,
            IBadgeCriteriaService badgeCriteriaService)
        {
            _badgeService = badgeService;
            _userActivityService = userActivityService;
            _badgeCriteriaService = badgeCriteriaService;
        }

        public async Task<List<Badge>> EvaluateAndAwardBadges(string userID, string activityType, int? score = null)
        {
            // Create a new user activity record
            var userActivity = new UserActivity
            {
                ActivityDate = DateTime.Now,
                ActivityId = Guid.NewGuid(),
                ActivityType = activityType,
                UserId = userID,
                Score = score ?? 0,
            };

            // Log the activity without related activity for now
            await _userActivityService.LogActivityAsync(userActivity, null);

            // Retrieve all available badges
            var badges = await _badgeService.GetBadgesAsync();
            var awardedBadges = new List<Badge>();

            // Evaluate and award badges
            foreach (var badge in badges)
            {
                if (await _badgeCriteriaService.CheckBadgeCriteriaAsync(userID, badge))
                {
                    await _badgeService.AwardBadgeAsync(userID, badge, userActivity);
                    awardedBadges.Add(badge);
                }
            }

            // Save new user activities after awarding badges
            await _userActivityService.SaveNewUserActivitiesAsync();

            return awardedBadges;
        }


    }
}
