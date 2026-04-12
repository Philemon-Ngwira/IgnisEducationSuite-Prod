using EDUSphereSharedProject.AchievementModels;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.Models.StoreProModels;
using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.Client.Services;

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
    public bool IsLicenseChecked { get; private set; } = false;
    public bool IsLicenseLoading { get; private set; } = false;
    public usp_GetPharmacyLicenseStatusResult License { get; private set; } = new();

    // --- Global Data ---
    public List<Badge> Badges { get; private set; } = new();
    public List<UserActivity> UserActivities { get; private set; } = new();
    public List<AcademicLevel> AcademicLevels { get; private set; } = new();
    public List<LevelSection> AcademicSections { get; private set; } = new();
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

    public async Task<bool> InitializeAsync(string userName, bool isAuthenticated)
    {
        if (!isAuthenticated || string.IsNullOrEmpty(userName))
        {
            IsLicenseChecked = true;
            LicenseIsActive = true;
            IsFullyInitialized = true;
            return true;
        }

        if (IsFullyInitialized) return true;

        UserID = userName;

        var rolePriority = new List<string>
        {
            "Admin","Teacher","Dean","Principal","KitchenStaff",
            "Parent","Student","SuperAdmin","Clinic Staff","TransportStaff"
        };

        try
        {
            // --- 1️⃣ Init Data (SP) ---
            var initService = _genericService.GetService<GetInitializationDataResult>();
            GetInitializationDataResult data = null;

            int attempts = 0;

            while (attempts < 2)
            {
                var res = await initService.GetAllAsync(
                    $"api/Dynamic/GetInitializationData/{UserID}", true);

                if (res != null && res.IsSuccess && res.Data.Any())
                {
                    data = res.Data.First();
                    break;
                }

                attempts++;
                await Task.Delay(500);
            }

            if (data == null)
                return false;

            // --- 2️⃣ Roles ---
            UserRoles = data.RoleName?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .ToList() ?? new();

            UserRole =
                (UserRoles.Count == 1 && (UserRoles[0] == "SuperAdmin" || UserRoles[0] == "Parent"))
                ? UserRoles[0]
                : rolePriority.FirstOrDefault(role => UserRoles.Contains(role)) ?? "Guest";

            // --- 3️⃣ Core Info ---
            SchoolID = data.SchoolID.ToString();
            SchoolName = data.SchoolName ?? "";
            UserEmail = data.Email ?? "";
            FirstName = data.FirstName ?? "";
            LastName = data.LastName ?? "";
            SchoolLogo = data.SchoolLogo?.Length > 0
                ? Convert.ToBase64String(data.SchoolLogo)
                : string.Empty;

            Currency.Currency = data.SchoolCurrencyName;
            Currency.CurrencyCode = data.CurrencyCode;
            Currency.CurrencyCountry = data.CurrencyCountry;
            Currency.CurrencySymbol = data.CurrencySymbol;
            HideStudentDashboard = data.HideStudentDashboard == 1;

            // --- 4️⃣ LICENSE (FIXED RACE CONDITION) ---
            if (UserRole == "SuperAdmin")
            {
                LicenseIsActive = true;
                IsLicenseChecked = true;
            }
            else
            {
                var _ = LoadLicenseAsync();
            }

            // --- 5️⃣ Background data (safe) ---
            var tasks = new List<Task>();

            tasks.Add(LoadBadgesAsync());

            if (UserRole != "SuperAdmin" && !string.IsNullOrEmpty(SchoolID))
            {
                tasks.Add(LoadAcademicLevelsAsync());
                tasks.Add(LoadAcademicSections());

                if (UserRole == "Student")
                {
                    tasks.Add(LoadUserActivitiesAsync());
                    tasks.Add(LoadAssignmentsAsync());
                }
            }

            await Task.WhenAll(tasks);

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

    #region License
    private async Task LoadLicenseAsync()
    {
        if (IsLicenseLoading) return;

        IsLicenseLoading = true;

        try
        {
            var licenseService = _genericService.GetService<usp_GetPharmacyLicenseStatusResult>();

            int attempts = 0;

            while (attempts < 3)
            {
                var task = licenseService.GetAllAsync(
                    $"api/Dynamic/GetLicenseStatus/{SchoolID}", true);

                var completed = await Task.WhenAny(task, Task.Delay(8000));

                if (completed != task)
                {
                    attempts++;
                    continue;
                }

                var result = await task;

                if (result?.IsSuccess == true && result.Data.Any())
                {
                    License = result.Data.First();
                    LicenseIsActive = License?.IsValid == 1;
                    IsLicenseChecked = true;

                    NotifyStateChanged();
                    return;
                }

                attempts++;
                await Task.Delay(500);
            }

            LicenseIsActive = false;
            IsLicenseChecked = true;
            NotifyStateChanged();
        }
        catch
        {
            LicenseIsActive = false;
            IsLicenseChecked = true;
            NotifyStateChanged();
        }
        finally
        {
            IsLicenseLoading = false;
        }
    }
    #endregion

    #region Data Loaders
    private async Task LoadBadgesAsync()
    {
        try
        {
            var badgeService = _genericService.GetService<Badge>();
            var result = await badgeService.GetAllAsync("api/Dynamic/GetAllSystemBadges", true);
            Badges = result.IsSuccess ? result.Data.ToList() : new();
        }
        catch { Badges = new(); }
    }

    private async Task LoadAcademicLevelsAsync()
    {
        try
        {
            var academicService = _genericService.GetService<AcademicLevel>();
            var result = await academicService.GetAllAsync(
                $"api/Dynamic/GetSchoolAcademicStructure/{SchoolID}", true);

            AcademicLevels = result.IsSuccess ? result.Data.ToList() : new();
        }
        catch { AcademicLevels = new(); }
    }

    private async Task LoadAcademicSections()
    {
        try
        {
            if (string.IsNullOrEmpty(SchoolID)) return;

            var service = _genericService.GetService<LevelSection>();
            var result = await service.GetAllAsync(
                $"api/Dynamic/GetSchoolAcademicSections/{Guid.Parse(SchoolID)}", true);

            AcademicSections = result.IsSuccess ? result.Data.ToList() : new();
        }
        catch { AcademicSections = new(); }
    }

    private async Task LoadUserActivitiesAsync()
    {
        try
        {
            var service = _genericService.GetService<UserActivity>();
            var result = await service.GetAllAsync(
                $"api/Dynamic/GetAllUserActivities/{UserID}", true);

            UserActivities = result.IsSuccess ? result.Data.ToList() : new();
        }
        catch { UserActivities = new(); }
    }

    private async Task LoadAssignmentsAsync()
    {
        try
        {
            var service = _genericService.GetService<GetStudentAssignmentsResult>();
            var result = await service.GetAllAsync(
                $"api/Dynamic/GetAssignment/{UserID}", true);

            NewAssignmentsCount = result.IsSuccess
                ? result.Data.Count(c => c.Overdue != 1)
                : 0;
        }
        catch
        {
            NewAssignmentsCount = 0;
        }
    }
    #endregion

    private void NotifyStateChanged() => OnChange?.Invoke();

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
}

public class SchoolCurrency
{
    public string Currency { get; set; }
    public string CurrencyCode { get; set; }
    public string CurrencyCountry { get; set; }
    public string CurrencySymbol { get; set; }
}