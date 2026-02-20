using EDUSphereSharedProject.AchievementModels;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.Models.StoreProModels;
using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using System.Linq;

public class AppState
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RolesAndLicensingService _rolesAndLicensing;
    private readonly GenericServiceFactory _genericService;

    // --- User & School Info ---
    public string UserID { get; private set; } = string.Empty;
    public string UserRole { get; set; } = "Guest";
    public string SchoolID { get; private set; } = string.Empty;
    public string SchoolName { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public string SchoolLogo { get; private set; } = string.Empty;
    public List<string> UserRoles { get; private set; } = new List<string>();
    public bool HideStudentDashboard { get; private set; }
    public bool LicenseIsActive { get; private set; }
    public SchoolCurrency Currency { get; private set; } = new();
   
    public usp_GetPharmacyLicenseStatusResult License { get; private set; } = new();

    // --- Global Data ---
    public List<Badge> Badges { get; private set; } = new();
    public List<UserActivity> UserActivities { get; private set; } = new();
    public List<AcademicLevel> AcademicLevels { get; private set; } = new();
    public int NewAssignmentsCount { get; private set; }

    // --- Initialization State ---
    public bool IsFullyInitialized { get; private set; } = false;

    public event Action OnChange;

    public AppState(IServiceProvider serviceProvider,
                    RolesAndLicensingService rolesAndLicensing,
                    GenericServiceFactory genericService)
    {
        _serviceProvider = serviceProvider;
        _rolesAndLicensing = rolesAndLicensing;
        _genericService = genericService;
    }

    /// <summary>
    /// Fully initializes AppState: essential + non-critical data.
    /// </summary>
    public async Task<bool> InitializeAsync(string userName, bool isAuthenticated, NavigationManager nav)
    {
        if (!isAuthenticated || string.IsNullOrEmpty(userName))
        {
            Console.WriteLine("[AppState] User not authenticated. Initialization aborted.");
            return false;
        }

        if (IsFullyInitialized) return true;

        UserID = userName;
        // Role priority
        var rolePriority = new List<string>
            {
                "Admin",
                "Teacher",
                "Dean",
                "Principal",
                "KitchenStaff",
                "Parent",
                "Student",
                "SuperAdmin",
                "Clinic Staff",
                "TransportStaff"
            };

        try
        {
            // 1️⃣ Load Initialization Data
            var initService = _genericService.GetService<GetInitializationDataResult>();
            var initResult = await initService.GetAllAsync($"api/Dynamic/GetInitializationData/{UserID}", true);

            if (!initResult.IsSuccess || !initResult.Data.Any())
            {
                await Task.Delay(1000);
                nav.NavigateTo("/", true);
                return false;
            }
            var data = initResult.Data.First();
            UserRoles = data.RoleName?
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Trim())
                        .ToList() ?? new();

            if (UserRoles.Count == 1 && (UserRoles[0] == "SuperAdmin" || UserRoles[0] == "Parent"))
            {
                UserRole = UserRoles[0];
            }
            else
            {
                UserRole = rolePriority.FirstOrDefault(role => UserRoles.Contains(role)) ?? "Guest";
            }

            SchoolID = data.SchoolID.ToString();
            SchoolName = data.SchoolName ?? "";
            UserEmail = data.Email ?? "";
            FirstName = data.FirstName ?? "";
            LastName = data.LastName ?? "";
            SchoolLogo = data.SchoolLogo?.Length > 0 ? Convert.ToBase64String(data.SchoolLogo) : string.Empty;
            Currency.Currency = data.SchoolCurrencyName;
            Currency.CurrencyCode = data.CurrencyCode;
            Currency.CurrencyCountry = data.CurrencyCountry;
            Currency.CurrencySymbol = data.CurrencySymbol;
            HideStudentDashboard = data.HideStudentDashboard == 1;

            if (UserRole != "SuperAdmin")
            {
                //{
                //    // 2️⃣ Load License
                //var licenseService = _genericService.GetService<usp_GetPharmacyLicenseStatusResult>();
                //var licenseResult = await licenseService.GetAllAsync($"api/Dynamic/GetLicenseStatus/{SchoolID}", true);
                //License = licenseResult.IsSuccess && licenseResult.Data.Any()
                //    ? licenseResult.Data.First()
                //    : new usp_GetPharmacyLicenseStatusResult();
                //LicenseIsActive = License?.IsValid == 1;

                LicenseIsActive = true;

                // 3️⃣ Load Non-Critical Data Immediately

                await LoadBadgesAsync();
                await LoadAcademicLevelsAsync();
                if (UserRole == "Student")
                {
                    await LoadUserActivitiesAsync();
                    await LoadAssignmentsAsync();
                }
            }
            else
            {
                LicenseIsActive = true;
            }
            IsFullyInitialized = true;
            NotifyStateChanged();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppState] Initialization failed: {ex.Message}");
            return false;
        }
    }

    #region Data Loading Helpers

    private async Task LoadBadgesAsync()
    {
        try
        {
            var badgeService = _genericService.GetService<Badge>();
            var result = await badgeService.GetAllAsync("api/Dynamic/GetAllSystemBadges", true);
            Badges = result.IsSuccess ? result.Data.ToList() : new();
        }
        catch
        {
            Badges = new();
        }
    }

    private async Task LoadAcademicLevelsAsync()
    {
        try
        {
            var academicService = _genericService.GetService<AcademicLevel>();
            var result = await academicService.GetAllAsync($"api/Dynamic/GetSchoolAcademicStructure/{SchoolID}", true);
            AcademicLevels = result.IsSuccess ? result.Data.ToList() : new();
        }
        catch
        {
            AcademicLevels = new();
        }
    }

    private async Task LoadUserActivitiesAsync()
    {
        try
        {
            var activityService = _genericService.GetService<UserActivity>();
            var result = await activityService.GetAllAsync($"api/Dynamic/GetAllUserActivities/{UserID}", true);
            UserActivities = result.IsSuccess ? result.Data.ToList() : new();
        }
        catch
        {
            UserActivities = new();
        }
    }

    private async Task LoadAssignmentsAsync()
    {
        try
        {
            var assignmentService = _genericService.GetService<GetStudentAssignmentsResult>();
            var result = await assignmentService.GetAllAsync($"api/Dynamic/GetAssignment/{UserID}", true);
            NewAssignmentsCount = result.IsSuccess ? result.Data.Count(c => c.Overdue != 1) : 0;
        }
        catch
        {
            NewAssignmentsCount = 0;
        }
    }

    #endregion

    private void NotifyStateChanged() => OnChange?.Invoke();

    #region Helper Getters for Pages

    public List<Badge> GetBadges() => Badges;
    public List<UserActivity> GetUserActivities() => UserActivities;
    public List<AcademicLevel> GetAcademicLevels() => AcademicLevels;
    public int GetNewAssignmentsCount() => NewAssignmentsCount;
    public void SwitchRole(string role)
    {
        if (UserRoles.Contains(role))
        {
            UserRole = role;
            NotifyStateChanged();
        }
    }

    public void SetNewAcademicStructure(List<AcademicLevel> levels)
    {
        AcademicLevels = levels.ToList();

    }
    #endregion

  
}
public class SchoolCurrency
{
    public string Currency { get; set; }
    public string CurrencyCode { get; set; }
    public string CurrencyCountry { get; set; }
    public string CurrencySymbol { get; set; }
}