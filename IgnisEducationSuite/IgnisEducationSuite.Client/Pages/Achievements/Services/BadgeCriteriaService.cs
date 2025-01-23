using EDUSphereSharedProject.AchievementModels;
using IgnisEducationSuite.Client.Pages.Achievements.Interfaces;

namespace IgnisEducationSuite.Client.Pages.Achievements.Services
{
    public class BadgeCriteriaService:IBadgeCriteriaService
    {
        private readonly IUserActivityService _userActivityService;

        public BadgeCriteriaService(IUserActivityService userActivityService)
        {
            _userActivityService = userActivityService;
        }

        public async Task<bool> CheckBadgeCriteriaAsync(string userID, Badge badge, int? NumberOfLessons = 0)
        {
            var userActivities = await _userActivityService.GetUserActivitiesAsync(userID);

            switch (badge.BadgeName)
            {
                case "Lesson Starter":
                    return userActivities.Count(ua => ua.UserId == userID && ua.ActivityType == "Lesson") >= 5;

                case "Lesson Enthusiast":
                    return userActivities.Count(ua => ua.UserId == userID && ua.ActivityType == "Lesson") >= 10;

                case "Consistent Learner":
                    return HasConsistentDailyLessons(userID, userActivities);

                case "Lesson Master":
                    return userActivities.Count(ua => ua.UserId == userID && ua.ActivityType == "Lesson") >= AllLessonsCount(NumberOfLessons);

                case "Timely Submitter":
                    return userActivities.Count(ua => ua.UserId == userID && ua.ActivityType == "Assignment" && ua.Score >= 100) >= 5;

                case "Assignment Starter":
                    return userActivities.Count(ua => ua.UserId == userID && ua.ActivityType == "Assignment") >= 5;

                case "Consistent Performer":
                    return HasConsistentHighScores(userID, "Assignment", 90, 30, userActivities);

                case "Assignment Perfectionist":
                    return userActivities.Count(ua => ua.UserId == userID && ua.ActivityType == "Assignment" && ua.Score >= 90) >= 5;

                case "Perfect Attendance":
                    return userActivities.Count(ua => ua.UserId == userID && ua.ActivityType == "Attendance" && ua.ActivityDate >= DateTime.Now.AddMonths(-1)) >= 30;

                case "Punctual Participant":
                    return userActivities.Count(ua => ua.UserId == userID && ua.ActivityType == "Attendance" && ua.Score >= 100) >= 5;

                case "Regular Attendee":
                    return userActivities.Count(ua => ua.UserId == userID && ua.ActivityType == "Attendance") >= 10;

                case "Attendance Hero":
                    return HasConsistentHighScores(userID, "Attendance", 95, 90, userActivities);

                case "Consistent High Scorer":
                    return HasConsistentHighScores(userID, "Exams", 90, 30, userActivities);

                case "Test Champ":
                    return userActivities.Count(ua => ua.UserId == userID && ua.ActivityType == "Exams" && ua.Score == 100) >= 1;

                case "Knowledge Master":
                case "Knowledge Apprentice":
                    return userActivities.Count(ua => ua.UserId == userID && ua.ActivityType == "Exams" && ua.Score >= 90) >= 5;

                case "All Rounder":
                    return HasEarnedBadgesInAllCategories(userID, userActivities);

                case "Marathon Learner":
                    return HasSpentHoursLearning(userID, 5, userActivities);

                case "Goal Setter":
                    return HasAchievedGoals(userID, 5, userActivities);

                case "Overachiever":
                    return HasEarnedHighestBadgesInAllCategories(userID, userActivities);

                default:
                    return false;
            }
        }

        public bool HasConsistentDailyLessons(string userID, List<UserActivity> userActivities)
        {
            var now = DateTime.Now.Date;
            var last30Days = userActivities
                .Where(ua => ua.UserId == userID && ua.ActivityType == "Lesson" && ua.ActivityDate >= now.AddDays(-30))
                .GroupBy(ua => ua.ActivityDate)
                .Count();

            return last30Days == 30;
        }

        public bool HasConsistentHighScores(string userID, string activityType, decimal threshold, int days, List<UserActivity> userActivities)
        {
            var now = DateTime.Now.Date;
            var highScores = userActivities
                .Where(ua => ua.UserId == userID && ua.ActivityType == activityType && ua.Score >= threshold && ua.ActivityDate >= now.AddDays(-days))
                .Count();

            return highScores == days;
        }

        public bool HasEarnedBadgesInAllCategories(string userID, List<UserActivity> userActivities)
        {
            // Logic to check if user has earned at least one badge in all categories
            return false; // Placeholder
        }

        public bool HasSpentHoursLearning(string userID, int hours, List<UserActivity> userActivities)
        {
            // Logic to check if user has spent a certain number of consecutive hours learning
            return false; // Placeholder
        }

        public bool HasAchievedGoals(string userID, int goals, List<UserActivity> userActivities)
        {
            // Logic to check if user has set and achieved a certain number of personal goals
            return false; // Placeholder
        }

        public bool HasEarnedHighestBadgesInAllCategories(string userID, List<UserActivity> userActivities)
        {
            // Logic to check if user has earned the highest level badges in all categories
            return false; // Placeholder
        }

        private int AllLessonsCount(int? numberoflessons)
        {
            // Placeholder for actual count logic
            return (int)numberoflessons;
        }

    }
}
