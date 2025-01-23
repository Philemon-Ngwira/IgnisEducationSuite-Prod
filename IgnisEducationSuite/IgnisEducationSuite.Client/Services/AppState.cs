using EDUSphereSharedProject.AchievementModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IgnisEducationSuite.Client.Services
{
    public class AppState
    {
        public string UserID { get; set; } = string.Empty;
        public string SchoolID { get; set; } = string.Empty;
        public bool LicenseIsActive { get; set; } = false;
        public string UserRole { get; set; } = "Guest";

        public List<Badge> Badges { get; set; }
        public List<UserActivity> UserActivities { get; set; }
        public bool IsInitialized { get; private set; } = false;
        public event Action OnChange;

        public void UpdateDetails(string userID, string schoolID, bool licenseIsActive, string userRole)
        {
            UserID = userID;
            SchoolID = schoolID;
            LicenseIsActive = licenseIsActive;
            UserRole = userRole;
            IsInitialized = true;

            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}
