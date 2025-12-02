using EDUSphereSharedProject.AchievementModels;
using EDUSphereSharedProject.Models.StoreProModels;
using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public class AppState
{
    private readonly IServiceProvider _serviceProvider;
    private IHttpClientFactory _httpClientFactory;
    private RolesAndLicensingService _rolesAndLicensing;
    private GenericServiceFactory _genericService;

    public usp_GetPharmacyLicenseStatusResult License { get; private set; } = new();

    public string UserEmail { get; set; } = string.Empty;
    public string UserID { get; private set; } = string.Empty;
    public string SchoolID { get; private set; } = string.Empty;
    public bool LicenseIsActive { get; private set; }
    public string UserRole { get; private set; } = "Guest";
    public string SchoolName { get; private set; } = string.Empty;
    public string SchoolLogo { get; private set; } = string.Empty;
    public bool HideStudentDashboard { get; private set; }
    public List<Badge> Badges { get; set; } = new();
    public List<UserActivity> UserActivities { get; set; } = new();
    public bool IsInitialized { get; private set; }

    public int newAssignmentsCount { get; private set; }

    public event Action OnChange;

    public AppState(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> InitializeAsync(string userName, bool isAuthenticated, NavigationManager nav, HttpClient http)
    {
        if (IsInitialized)
            return true;

        if (!isAuthenticated)
        {
            Console.WriteLine("User not authenticated. Initialization cancelled.");
            return false;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();

            // Resolve once
            _httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            _rolesAndLicensing = scope.ServiceProvider.GetRequiredService<RolesAndLicensingService>();
            _genericService = scope.ServiceProvider.GetRequiredService<GenericServiceFactory>();

            UserID = userName;

            var initService = _genericService.GetService<GetInitializationDataResult>();
            var initResult = await initService.GetAllAsync($"api/Dynamic/GetInitializationData/{UserID}", true);

            if (!initResult.IsSuccess || !initResult.Data.Any())
            {
                Console.WriteLine("Initialization data unavailable.");
                return false;
            }

            var d = initResult.Data.ToList();
            var data = d[0];

            SchoolID = data.SchoolID.ToString();
            UserRole = data.RoleName;
            SchoolName = data.SchoolName;
            UserEmail = data.Email;
            // Convert once, avoid unnecessary null/empty operations
            SchoolLogo = data.SchoolLogo is { Length: > 0 }
                ? Convert.ToBase64String(data.SchoolLogo)
                : string.Empty;

            HideStudentDashboard = data.HideStudentDashboard == 1;

            await LoadLicenseAsync(SchoolID);
            await LoadUserBadgesAndActivitiesAsync();
            if (UserRole == "Student")
            {
                await GetAssignments();
            }
            LicenseIsActive = License?.IsValid == 1;

            IsInitialized = true;
            NotifyStateChanged();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppState] Initialization failed: {ex.Message}");
            return false;
        }
    }
    protected async Task GetAssignments()
    {
        var service = _genericService.GetService<GetStudentAssignmentsResult>();
        var result = await service.GetAllAsync($"api/Dynamic/GetAssignment/{UserID}", true);
        if (result.IsSuccess)
        {
            newAssignmentsCount = result.Data.Where(c => c.Overdue != 1).Count();
        }

    }
    private async Task LoadLicenseAsync(string tenantId)
    {
        try
        {
            var service = _genericService.GetService<usp_GetPharmacyLicenseStatusResult>();
            var result = await service.GetAllAsync($"api/Dynamic/GetLicenseStatus/{tenantId}", true);

            if (result.IsSuccess)
                License = result.Data.FirstOrDefault() ?? new usp_GetPharmacyLicenseStatusResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading license: {ex.Message}");
        }
    }

    private async Task LoadUserBadgesAndActivitiesAsync()
    {
        try
        {
            var badgeService = _genericService.GetService<Badge>();
            var badgeResult = await badgeService.GetAllAsync("api/Dynamic/GetAllSystemBadges", true);

            Badges = badgeResult.IsSuccess ? badgeResult.Data.ToList() : new List<Badge>();

            if (UserRole == "Student")
            {
                var activityService = _genericService.GetService<UserActivity>();
                var activityResult = await activityService.GetAllAsync($"api/Dynamic/GetAllUserActivities/{UserID}", true);

                UserActivities = activityResult.IsSuccess ? activityResult.Data.ToList() : new List<UserActivity>();
            }
            else
            {
                // Non-students don't need activities
                UserActivities.Clear();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading badges/activities: {ex.Message}");
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
