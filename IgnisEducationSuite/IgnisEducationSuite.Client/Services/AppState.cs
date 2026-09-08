using EDUSphereSharedProject.AchievementModels;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.Models.StoreProModels;
using EDUSphereSharedProject.UniversalModels;
using EDUSphereSharedProject.LicensingModel;
using IgnisEducationSuite.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

public class AppState
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RolesAndLicensingService _rolesAndLicensing;
    private readonly GenericServiceFactory _genericService;

    // --- User & School Info ---
    public bool IsCoreInitialized { get; set; } = false;
    public bool IsDeferredLoaded { get; private set; } = false;
    public bool IsInitializing { get; private set; } = false;
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
    public bool LicenseIsActive { get; private set; } = true;

    /// <summary>
    /// Full licence state, including whether the check actually succeeded.
    ///
    /// Read <c>LicenseStatus.IsConfirmedUnlicensed</c> — not <c>!LicenseIsActive</c> — before
    /// restricting anything. The app fails open, so LicenseIsActive stays true when the licensing
    /// service is unreachable, and only a successful check that says otherwise should block a
    /// feature.
    /// </summary>
    public LicenseStatusDto LicenseStatus { get; private set; } = new();

    /// <summary>Maximum user accounts allowed, or null when unknown/unenforced.</summary>
    public int? LicenseUserLimit => LicenseStatus.UserLimit;
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
    public bool IsFullyInitialized { get; set; } = false;

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
    /// 

    private readonly object _initializationLock = new();
    private Task<bool>? _initializationTask;

    public Task<bool> EnsureInitializedAsync(string userName, bool isAuthenticated)
    {
        if (!isAuthenticated || string.IsNullOrWhiteSpace(userName))
            return Task.FromResult(false);
        lock (_initializationLock)
        {
            if (_initializationTask is { IsCompleted: false })
                return _initializationTask;
            if (IsFullyInitialized && UserID == userName)
                return Task.FromResult(true);
            return _initializationTask = InitializeSessionAsync(userName);
        }
    }

    public Task<bool> InitializeAsync(string userName, bool isAuthenticated)
        => EnsureInitializedAsync(userName, isAuthenticated);

    private async Task<bool> InitializeSessionAsync(string userName)
    {
        IsInitializing = true;
        IsFullyInitialized = false;
        IsCoreInitialized = false;
        IsDeferredLoaded = false;
        try
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (await InitializeCoreAsync(userName, true)) return true;
                if (attempt < 2) await Task.Delay(500 * (attempt + 1));
            }
            return false;
        }
        finally { IsInitializing = false; }
    }
    /// <summary>
    /// Resolves this school's licence once per sign-in.
    ///
    /// Fails open by construction: any failure leaves LicenseIsActive true and Verified false, so
    /// the app stays usable and admins can be told the check did not happen. SuperAdmins are not
    /// tied to a school, so there is nothing to check for them.
    /// </summary>
    private async Task LoadLicenseAsync()
    {
        IsLicenseLoading = true;

        try
        {
            if (UserRole == "SuperAdmin" || !Guid.TryParse(SchoolID, out var clientId))
            {
                LicenseStatus = new LicenseStatusDto { IsLicensed = true, Verified = true, Status = "Not applicable" };
                LicenseIsActive = true;
                return;
            }

            var http = _serviceProvider.GetRequiredService<HttpClient>();
            var status = await http.GetFromJsonAsync<LicenseStatusDto>($"api/Verification/Status/{clientId}");

            LicenseStatus = status ?? new LicenseStatusDto
            {
                IsLicensed = true,
                Verified = false,
                Status = "Could not be verified",
                Notice = "The licence check returned no result. Everything continues to work.",
            };

            LicenseIsActive = LicenseStatus.IsLicensed;
        }
        catch (Exception ex)
        {
            // Never rethrow: this runs inside initialization, and a licensing problem must not
            // prevent sign-in.
            Console.WriteLine($"[AppState] Licence check failed: {ex.Message}");

            LicenseStatus = new LicenseStatusDto
            {
                IsLicensed = true,
                Verified = false,
                Status = "Could not be verified",
                Notice = "The licensing service could not be reached, so this school's licence has not been verified.",
            };

            LicenseIsActive = true;
        }
        finally
        {
            IsLicenseChecked = true;
            IsLicenseLoading = false;
        }
    }

    /// <summary>Re-checks the licence, bypassing the server cache. For use after a licence is
    /// activated or renewed, which would otherwise not show up until the cache expires.</summary>
    public async Task RefreshLicenseAsync()
    {
        if (UserRole == "SuperAdmin" || !Guid.TryParse(SchoolID, out var clientId)) return;

        try
        {
            var http = _serviceProvider.GetRequiredService<HttpClient>();
            var status = await http.GetFromJsonAsync<LicenseStatusDto>(
                $"api/Verification/Status/{clientId}?refresh=true");

            if (status is not null)
            {
                LicenseStatus = status;
                LicenseIsActive = status.IsLicensed;
                NotifyStateChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppState] Licence refresh failed: {ex.Message}");
        }
    }

    private bool HasValidCoreData()
    {
        return !string.IsNullOrEmpty(UserID)
            && (Guid.TryParse(SchoolID, out _) || UserRole is "SuperAdmin" or "Parent" or "Finance")
            && !string.IsNullOrEmpty(UserRole);
    }
    private async Task<bool> InitializeCoreAsync(string userName, bool isAuthenticated)
    {
        if (!isAuthenticated || string.IsNullOrEmpty(userName))
            return false;

        if (IsFullyInitialized) return true;

        UserID = userName;

        var rolePriority = new List<string>
    {
        "Admin","Teacher","Dean","Principal","KitchenStaff",
        "Parent","Student","SuperAdmin","Clinic Staff","TransportStaff"
    };

        try
        {
            // --- 1️⃣ Load Initialization Data with retry ---
            var initService = _genericService.GetService<GetInitializationDataResult>();
            GetInitializationDataResult data = null;

            int attempts = 0;

            while (attempts < 2)
            {
                var res = await initService.GetAllAsync($"api/Dynamic/GetInitializationData/{UserID}", true);

                if (res != null && res.IsSuccess && res.Data.Any())
                {
                    data = res.Data.First();
                    break; // ✅ only break on success
                }

                attempts++;
                await Task.Delay(500); // retry delay
            }

            if (data == null)
                return false;

            // --- 2️⃣ Set Roles ---
            UserRoles = data.RoleName?
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Trim())
                        .ToList() ?? new();

            UserRole = (UserRoles.Count == 1 && (UserRoles[0] == "SuperAdmin" || UserRoles[0] == "Parent" || UserRoles[0] == "Finance"))
                        ? UserRoles[0]
                        : rolePriority.FirstOrDefault(role => UserRoles.Contains(role)) ?? "Guest";

            // --- 3️⃣ Set Basic Info ---
            SchoolID = data.SchoolID.ToString();
            SchoolName = data.SchoolName ?? "";
            UserEmail = data.Email ?? "";
            FirstName = data.FirstName ?? "";
            LastName = data.LastName ?? "";


            if (UserRole != "SuperAdmin")
            {
                SchoolLogo = data.SchoolLogo?.Length > 0
              ? Convert.ToBase64String(data.SchoolLogo)
              : "/images/logo.png";
                Currency.Currency = data.SchoolCurrencyName;
                Currency.CurrencyCode = data.CurrencyCode;
                Currency.CurrencyCountry = data.CurrencyCountry;
                Currency.CurrencySymbol = data.CurrencySymbol;
                HideStudentDashboard = data.HideStudentDashboard == 1;
            }

            // --- 4️⃣ Load License ---
            //
            // One call, once per sign-in. The server caches the result per school, so this does not
            // become a per-page cost, and it returns a status object rather than throwing — an
            // unreachable licensing service must never stop someone signing in.
            await LoadLicenseAsync();
            if (!HasValidCoreData())
            {
                Console.WriteLine("[AppState] Core initialization invalid.");
                return false;
            }

            IsCoreInitialized = true;
            NotifyStateChanged();

            // --- 🚀 Fire-and-forget deferred loading ---
            await LoadDeferredDataAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppState] Initialization failed: {ex.Message}");
            return false;
        }
    }
    #region Data Loading Helpers
    private async Task LoadDeferredDataAsync()
    {
        try
        {
            var tasks = new List<Task>();

            // ✅ Always safe
            tasks.Add(LoadBadgesAsync());

            // ❗ Only load school data if NOT SuperAdmin AND SchoolID is valid
            if (UserRole != "SuperAdmin" && (Guid.TryParse(SchoolID, out _) || UserRole is "SuperAdmin" or "Parent" or "Finance"))
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

            IsDeferredLoaded = true;
            IsFullyInitialized = true;

            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppState] Deferred load failed: {ex.Message}");
            throw;
        }
    }
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
            if (!result.IsSuccess || result.Data is null) throw new InvalidOperationException("Academic levels could not be loaded.");
            AcademicLevels = result.Data.ToList();
        }
        catch
        {
            throw;
        }
    }
    private async Task LoadAcademicSections()
    {
        try
        {
            if (string.IsNullOrEmpty(SchoolID))
                return; // 🚨 prevent crash

            var levelSectService = _genericService.GetService<LevelSection>();
            var result = await levelSectService.GetAllAsync(
                $"api/Dynamic/GetSchoolAcademicSections/{Guid.Parse(SchoolID)}", true);

            if (!result.IsSuccess || result.Data is null) throw new InvalidOperationException("Academic sections could not be loaded.");
            AcademicSections = result.Data.ToList();
        }
        catch
        {
            throw;
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