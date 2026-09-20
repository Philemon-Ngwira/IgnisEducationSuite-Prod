using EduSphereDomain.Data;
using EDUSphereSharedProject.LicensingModel;
using EDUSphereSharedProject.UniversalModels.SuperAdmin;
using IgnisEducationSuite.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IgnisEducationSuite.ServerServices.SuperAdmin
{
    /// <summary>
    /// Cross-tenant oversight for the SuperAdmin console.
    ///
    /// Two rules shape everything here:
    ///
    /// 1. <b>Read-only across tenants.</b> This service reports on schools; it never reaches into
    ///    one. Counts and licence state only — no student, mark or finance records leave here, so a
    ///    SuperAdmin cannot read a school's data through the console.
    ///
    /// 2. <b>Aggregate, don't iterate.</b> Every count is one grouped query over all schools rather
    ///    than a query per school. With a few dozen tenants the naive shape would be several hundred
    ///    round trips per page load.
    ///
    /// The one unavoidable per-school call is the licence check, because the licensing service is
    /// addressed one client at a time. Those run with bounded concurrency and lean on
    /// <see cref="LicenseService"/>'s cache, so a warm console costs no outbound requests at all.
    /// </summary>
    public class TenantOversightService
    {
        /// <summary>How many licence checks to have in flight at once. High enough that a cold load
        /// of a normal portfolio is quick, low enough not to hammer the licensing service.</summary>
        private const int LicenseConcurrency = 6;

        /// <summary>Total budget for resolving every licence. Past this, remaining schools come back
        /// unverified rather than holding the whole console hostage to a slow service.</summary>
        private static readonly TimeSpan LicenseBudget = TimeSpan.FromSeconds(20);

        private readonly PhoenixEdusphereContext _domain;
        private readonly ApplicationDbContext _identity;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LicenseService _licenses;
        private readonly ILogger<TenantOversightService> _logger;

        public TenantOversightService(
            PhoenixEdusphereContext domain,
            ApplicationDbContext identity,
            UserManager<ApplicationUser> userManager,
            LicenseService licenses,
            ILogger<TenantOversightService> logger)
        {
            _domain = domain;
            _identity = identity;
            _userManager = userManager;
            _licenses = licenses;
            _logger = logger;
        }

        // -----------------------------------------------------------------------------
        // Portfolio
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Every school with its size and licence state.
        /// </summary>
        /// <param name="includeLicenses">
        /// When false, the licence fields are left at their defaults and
        /// <see cref="TenantPortfolioDto.LicensesIncluded"/> is false. Used for a fast repaint after
        /// an action, where the counts are what changed and the licence state is already on screen.
        /// </param>
        /// <param name="forceRefresh">
        /// Bypasses the licence cache. This is what "Refresh" in the console means: without it the
        /// button would re-render the same cached answer for up to CacheMinutes and look broken to
        /// anyone who had just changed a licence elsewhere.
        /// </param>
        public async Task<TenantPortfolioDto> GetPortfolioAsync(
            bool includeLicenses = true, bool forceRefresh = false, CancellationToken ct = default)
        {
            var schools = await _domain.Schools
                .AsNoTracking()
                .Select(s => new
                {
                    s.SchoolID,
                    s.SchoolName,
                    s.SchoolEmail,
                    s.SchoolPhoneContact,
                    s.SchoolWebsite,
                    s.CurrencyCode,
                    s.CurrencySymbol,
                    HasLogo = s.SchoolLogo != null,
                })
                .ToListAsync(ct);

            var counts = await LoadCountsAsync(ct);

            var portfolio = new TenantPortfolioDto { LicensesIncluded = includeLicenses };

            foreach (var school in schools)
            {
                var summary = new TenantSummaryDto
                {
                    SchoolId = school.SchoolID,
                    SchoolName = school.SchoolName ?? "(unnamed school)",
                    SchoolEmail = school.SchoolEmail,
                    SchoolPhoneContact = school.SchoolPhoneContact,
                    SchoolWebsite = school.SchoolWebsite,
                    CurrencyCode = school.CurrencyCode,
                    CurrencySymbol = school.CurrencySymbol,
                    HasLogo = school.HasLogo,
                };

                counts.ApplyTo(summary);
                portfolio.Tenants.Add(summary);
            }

            if (includeLicenses)
            {
                await ResolveLicensesAsync(portfolio.Tenants, forceRefresh, ct);
            }

            portfolio.Tenants = portfolio.Tenants
                .OrderByDescending(t => t.WorstSeverity)
                .ThenBy(t => t.SchoolName)
                .ToList();

            return portfolio;
        }

        /// <summary>One school in full, including its administrator accounts and licence history.</summary>
        public async Task<TenantDetailDto?> GetTenantAsync(Guid schoolId, CancellationToken ct = default)
        {
            var school = await _domain.Schools
                .AsNoTracking()
                .Where(s => s.SchoolID == schoolId)
                .Select(s => new
                {
                    s.SchoolID,
                    s.SchoolName,
                    s.SchoolEmail,
                    s.SchoolPhoneContact,
                    s.SchoolWebsite,
                    s.CurrencyCode,
                    s.CurrencySymbol,
                    HasLogo = s.SchoolLogo != null,
                    s.ManualPositionRecalculationEnabled,
                })
                .FirstOrDefaultAsync(ct);

            if (school is null) return null;

            var summary = new TenantSummaryDto
            {
                SchoolId = school.SchoolID,
                SchoolName = school.SchoolName ?? "(unnamed school)",
                SchoolEmail = school.SchoolEmail,
                SchoolPhoneContact = school.SchoolPhoneContact,
                SchoolWebsite = school.SchoolWebsite,
                CurrencyCode = school.CurrencyCode,
                CurrencySymbol = school.CurrencySymbol,
                HasLogo = school.HasLogo,
                ManualPositionRecalculationEnabled = school.ManualPositionRecalculationEnabled == true,
            };

            var counts = await LoadCountsAsync(ct, schoolId);
            counts.ApplyTo(summary);

            ApplyLicense(summary, await SafeLicenseAsync(schoolId, forceRefresh: false));

            var detail = new TenantDetailDto
            {
                Summary = summary,
                Admins = await ListAdminsAsync(schoolId, ct),
            };

            try
            {
                detail.LicenseHistory = await _licenses.GetClientLicensesAsync(schoolId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read licence history for school {SchoolId}", schoolId);
                detail.LicenseHistoryError = "Licence history could not be loaded.";
            }

            return detail;
        }

        /// <summary>Administrator accounts for one school, from Identity rather than the ClientAdmin
        /// table: an account that cannot sign in is the thing that matters, and ClientAdmin rows can
        /// exist without one.</summary>
        public async Task<List<TenantAdminDto>> ListAdminsAsync(Guid schoolId, CancellationToken ct = default)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            return admins
                .Where(u => u.SchoolID == schoolId)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new TenantAdminDto
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AccountActive = u.AccountActive,
                    RequiresPasswordReset = u.requiresPasswordReset,
                    EmailConfirmed = u.EmailConfirmed,
                    IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
                })
                .ToList();
        }

        // -----------------------------------------------------------------------------
        // Licence actions
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Licenses a school.
        ///
        /// The client identity sent to the licensing service is taken from the school record, not
        /// from the request: the console chooses which school and on what terms, never who the
        /// client is.
        /// </summary>
        public async Task<TenantActionResult> ActivateLicenseAsync(ActivateTenantLicenseRequest request)
        {
            if (request.SchoolId == Guid.Empty)
                return TenantActionResult.Fail("No school was specified.");

            if (request.UserLimit <= 0)
                return TenantActionResult.Fail("The user limit must be at least 1.");

            if (request.EndDate.Date <= request.StartDate.Date)
                return TenantActionResult.Fail("The end date must be after the start date.");

            var school = await _domain.Schools
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SchoolID == request.SchoolId);

            if (school is null)
                return TenantActionResult.Fail("That school no longer exists.");

            var payload = new ActivateLicenseRequest
            {
                ClientId = school.SchoolID.ToString(),
                ClientName = school.SchoolName ?? "Unnamed school",
                Email = school.SchoolEmail ?? "",
                Phone = school.SchoolPhoneContact ?? "",
                Application = "IgnisEducationSuite",
                PlanType = request.PlanType,
                UserLimit = request.UserLimit,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
            };

            try
            {
                await _licenses.ActivateLicenseAsync(payload);

                // Without this the new licence would not show up until the cache window expires,
                // and the console would appear to have done nothing.
                _licenses.InvalidateCache(school.SchoolID);

                return TenantActionResult.Ok(
                    $"{school.SchoolName} licensed until {request.EndDate:d} for {request.UserLimit} user(s).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Licence activation failed for school {SchoolId}", request.SchoolId);
                return TenantActionResult.Fail($"Activation failed: {ex.Message}");
            }
        }

        public async Task<TenantActionResult> TerminateLicenseAsync(TerminateTenantLicenseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.LicenseKey))
                return TenantActionResult.Fail("No licence key was supplied.");

            try
            {
                await _licenses.TerminateLicenseAsync(new TerminateLicenseRequest { LicenseKey = request.LicenseKey });

                if (request.SchoolId != Guid.Empty)
                    _licenses.InvalidateCache(request.SchoolId);

                return TenantActionResult.Ok("Licence terminated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Licence termination failed for key ending {Tail}",
                    request.LicenseKey.Length > 4 ? request.LicenseKey[^4..] : "????");

                return TenantActionResult.Fail($"Termination failed: {ex.Message}");
            }
        }

        // -----------------------------------------------------------------------------
        // Feature settings
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Turns manual position recalculation on or off for one school.
        ///
        /// This is the one school-record field the console writes — deliberately: it changes what a
        /// screen offers, not any student's data, so it stays within the console's remit of running
        /// schools rather than reaching into them.
        /// </summary>
        public async Task<TenantActionResult> SetManualPositionRecalculationAsync(Guid schoolId, bool enabled)
        {
            if (schoolId == Guid.Empty)
                return TenantActionResult.Fail("No school was specified.");

            var school = await _domain.Schools.FirstOrDefaultAsync(s => s.SchoolID == schoolId);

            if (school is null)
                return TenantActionResult.Fail("That school no longer exists.");

            school.ManualPositionRecalculationEnabled = enabled;
            await _domain.SaveChangesAsync();

            return TenantActionResult.Ok(enabled
                ? $"{school.SchoolName} can now recalculate positions manually."
                : $"Manual position recalculation is off for {school.SchoolName}.");
        }

        // -----------------------------------------------------------------------------
        // Counts
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Every per-school count in one pass.
        ///
        /// Each figure is a single grouped query across all schools; the dictionaries are then read
        /// per school in memory. Pass a school id to restrict the same queries to one tenant.
        /// </summary>
        private async Task<TenantCounts> LoadCountsAsync(CancellationToken ct, Guid? onlySchool = null)
        {
            var counts = new TenantCounts();

            var students = _domain.Students.AsNoTracking().Where(s => s.SchoolID != null);
            var teachers = _domain.Teachers.AsNoTracking().Where(t => t.SchoolID != null);
            var parents = _domain.Parents.AsNoTracking().Where(p => p.SchoolID != null);
            var staff = _domain.Staff.AsNoTracking().Where(s => s.SchoolID != null);
            // Note the spelling: the Class entity's column is SChoolID, not SchoolID.
            var classes = _domain.Classes.AsNoTracking().Where(c => c.SChoolID != null);
            var terms = _domain.TermSettings.AsNoTracking().Where(t => t.SchoolID != null);
            var users = _identity.Users.AsNoTracking().Where(u => u.SchoolID != null);

            if (onlySchool is { } id)
            {
                students = students.Where(s => s.SchoolID == id);
                teachers = teachers.Where(t => t.SchoolID == id);
                parents = parents.Where(p => p.SchoolID == id);
                staff = staff.Where(s => s.SchoolID == id);
                classes = classes.Where(c => c.SChoolID == id);
                terms = terms.Where(t => t.SchoolID == id);
                users = users.Where(u => u.SchoolID == id);
            }

            counts.Students = await students
                .GroupBy(s => s.SchoolID!.Value)
                .Select(g => new { g.Key, Count = g.Count(), LastOnboarded = g.Max(x => x.DateOnBoarded) })
                .ToDictionaryAsync(x => x.Key, x => (x.Count, x.LastOnboarded), ct);

            counts.Teachers = await teachers
                .GroupBy(t => t.SchoolID!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            counts.Parents = await parents
                .GroupBy(p => p.SchoolID!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            counts.Staff = await staff
                .GroupBy(s => s.SchoolID!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            counts.Classes = await classes
                .GroupBy(c => c.SChoolID!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            counts.Terms = await terms
                .GroupBy(t => t.SchoolID!.Value)
                .Select(g => new { g.Key, LatestEnd = g.Max(x => x.ActualEndTermDate) })
                .ToDictionaryAsync(x => x.Key, x => x.LatestEnd, ct);

            counts.Users = await users
                .GroupBy(u => u.SchoolID!.Value)
                .Select(g => new
                {
                    g.Key,
                    Total = g.Count(),
                    Inactive = g.Count(u => !u.AccountActive),
                })
                .ToDictionaryAsync(x => x.Key, x => (x.Total, x.Inactive), ct);

            // Admin accounts. Joined through Identity's role tables rather than calling
            // GetUsersInRoleAsync per school, which would be one round trip per tenant.
            var adminQuery =
                from user in _identity.Users.AsNoTracking()
                join userRole in _identity.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in _identity.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where role.Name == "Admin" && user.SchoolID != null
                select user;

            if (onlySchool is { } adminSchool)
                adminQuery = adminQuery.Where(u => u.SchoolID == adminSchool);

            counts.Admins = await adminQuery
                .GroupBy(u => u.SchoolID!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            // The current term, if any school is inside one right now.
            //
            // Grouped in memory rather than in the query: picking one row out of each group
            // (First() after an OrderBy inside a GroupBy projection) is not reliably translatable,
            // and a school has a handful of terms, so the rows are cheap to bring back.
            var today = DateTime.Today;

            var activeTerms = await terms
                .Where(t => t.ActualStartTermDate <= today && t.ActualEndTermDate >= today)
                .Select(t => new { SchoolId = t.SchoolID!.Value, t.TermName, t.ActualStartTermDate })
                .ToListAsync(ct);

            counts.ActiveTermNames = activeTerms
                .GroupBy(t => t.SchoolId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.ActualStartTermDate).First().TermName ?? "");

            return counts;
        }

        private sealed class TenantCounts
        {
            public Dictionary<Guid, (int Count, DateTime? LastOnboarded)> Students { get; set; } = new();
            public Dictionary<Guid, int> Teachers { get; set; } = new();
            public Dictionary<Guid, int> Parents { get; set; } = new();
            public Dictionary<Guid, int> Staff { get; set; } = new();
            public Dictionary<Guid, int> Classes { get; set; } = new();
            public Dictionary<Guid, int> Admins { get; set; } = new();
            public Dictionary<Guid, DateTime?> Terms { get; set; } = new();
            public Dictionary<Guid, string> ActiveTermNames { get; set; } = new();
            public Dictionary<Guid, (int Total, int Inactive)> Users { get; set; } = new();

            public void ApplyTo(TenantSummaryDto summary)
            {
                if (Students.TryGetValue(summary.SchoolId, out var students))
                {
                    summary.StudentCount = students.Count;
                    summary.LastStudentOnboardedAt = students.LastOnboarded;
                }

                summary.TeacherCount = Teachers.GetValueOrDefault(summary.SchoolId);
                summary.ParentCount = Parents.GetValueOrDefault(summary.SchoolId);
                summary.StaffCount = Staff.GetValueOrDefault(summary.SchoolId);
                summary.ClassCount = Classes.GetValueOrDefault(summary.SchoolId);
                summary.AdminCount = Admins.GetValueOrDefault(summary.SchoolId);
                summary.LatestTermEnd = Terms.GetValueOrDefault(summary.SchoolId);
                summary.ActiveTermName = ActiveTermNames.GetValueOrDefault(summary.SchoolId);

                if (Users.TryGetValue(summary.SchoolId, out var users))
                {
                    summary.UserAccountCount = users.Total;
                    summary.InactiveUserCount = users.Inactive;
                }
            }
        }

        // -----------------------------------------------------------------------------
        // Licence resolution
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Fills in licence state for every school, a few at a time.
        ///
        /// Bounded rather than sequential because a cold cache would otherwise mean one round trip
        /// after another; bounded rather than unbounded because a portfolio of fifty schools should
        /// not open fifty sockets at the licensing service. The whole pass is capped by
        /// <see cref="LicenseBudget"/> — schools not resolved by then stay unverified, which the
        /// console displays honestly rather than as a failure.
        /// </summary>
        private async Task ResolveLicensesAsync(List<TenantSummaryDto> tenants, bool forceRefresh, CancellationToken ct)
        {
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
            budget.CancelAfter(LicenseBudget);

            using var gate = new SemaphoreSlim(LicenseConcurrency);

            var work = tenants.Select(async tenant =>
            {
                try
                {
                    await gate.WaitAsync(budget.Token);
                }
                catch (OperationCanceledException)
                {
                    ApplyLicense(tenant, null);
                    return;
                }

                try
                {
                    ApplyLicense(tenant, budget.IsCancellationRequested
                        ? null
                        : await SafeLicenseAsync(tenant.SchoolId, forceRefresh));
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(work);
        }

        /// <summary>Never throws. A licence check that fails is unverified, not unlicensed.</summary>
        private async Task<LicenseStatusDto?> SafeLicenseAsync(Guid schoolId, bool forceRefresh)
        {
            try
            {
                return await _licenses.GetLicenseStatusAsync(schoolId, forceRefresh);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Licence check failed for school {SchoolId}", schoolId);
                return null;
            }
        }

        /// <summary>
        /// Copies a licence result onto a tenant row. A null status means the check did not happen —
        /// which leaves IsLicensed true and Verified false, matching the app's fail-open behaviour
        /// everywhere else.
        /// </summary>
        private static void ApplyLicense(TenantSummaryDto tenant, LicenseStatusDto? status)
        {
            if (status is null)
            {
                tenant.IsLicensed = true;
                tenant.LicenseVerified = false;
                tenant.LicenseStatus = "Could not be verified";
                return;
            }

            tenant.IsLicensed = status.IsLicensed;
            tenant.LicenseVerified = status.Verified;
            tenant.LicenseStatus = status.Status;
            tenant.LicenseEndDate = status.EndDate;
            tenant.DaysUntilExpiry = status.DaysUntilExpiry;
            tenant.UserLimit = status.UserLimit;
        }
    }
}
