using EDUSphereSharedProject.AchievementModels;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

public class AppState
{
    private readonly IServiceProvider _serviceProvider;
    private IHttpClientFactory _httpClientFactory;
    private RolesAndLicensingService _rolesAndLicensing;
    private GenericServiceFactory _genericService;

    public string UserID { get; private set; } = string.Empty;
    public string SchoolID { get; private set; } = string.Empty;
    public bool LicenseIsActive { get; private set; } = false;
    public string UserRole { get; private set; } = "Guest";
    public bool HideStudentDashboard { get; private set; } = false;
    public List<Badge> Badges { get; set; } = new();
    public List<UserActivity> UserActivities { get; set; } = new();

    public bool IsInitialized { get; private set; } = false;

    public event Action OnChange;

    // Constructor accepts IServiceProvider
    public AppState(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> InitializeAsync(string UserName, bool isAuthenticated, NavigationManager _navigationManager, HttpClient http)
    {
        if (IsInitialized) return true;

        try
        {
            // Create a scope explicitly to resolve scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                _httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                _rolesAndLicensing = scope.ServiceProvider.GetRequiredService<RolesAndLicensingService>();
                _genericService = scope.ServiceProvider.GetRequiredService<GenericServiceFactory>();

                var _http = _httpClientFactory.CreateClient();

                if (isAuthenticated)
                {
                    UserID = UserName;
                    SchoolID = await _rolesAndLicensing.GetSchoolId(_navigationManager.BaseUri, UserID);
                    LicenseIsActive = await _rolesAndLicensing.GetLicenseStatus(_navigationManager.BaseUri, SchoolID);
                    UserRole = await _rolesAndLicensing.GetUserRole(_navigationManager.BaseUri, UserName);
                    HideStudentDashboard = await _rolesAndLicensing.getStudentDashState(SchoolID);
                    await LoadUserBadgesAndActivities();

                    IsInitialized = true;
                    NotifyStateChanged();
                }
                else
                {
                    Console.WriteLine("User is not authenticated. AppState initialization aborted.");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing AppState: {ex.Message}");
            return false;
        }

        return true;
    }

    private async Task LoadUserBadgesAndActivities()
    {
        try
        {
            // Resolve services dynamically

            var badgeService = _genericService.GetService<Badge>();
            var badgeResult = await badgeService.GetAllAsync("api/Dynamic/GetAllSystemBadges", true);
            Badges = badgeResult.IsSuccess ? badgeResult.Data.ToList() : new List<Badge>();

            if (UserRole == "Student")
            {
                var activityService = _genericService.GetService<UserActivity>();
                var activityResult = await activityService.GetAllAsync($"api/Dynamic/GetAllUserActivities/{UserID}", true);
                UserActivities = activityResult.IsSuccess ? activityResult.Data.ToList() : new List<UserActivity>();
            }

            NotifyStateChanged();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading badges and activities: {ex.Message}");
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
