using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace EDUSphereSharedProject.UniversalModels.SuperAdmin
{
    /// <summary>
    /// One school as the SuperAdmin sees it: who they are, how big they are, and where their
    /// licence stands.
    ///
    /// This is deliberately a flat projection rather than the <c>School</c> entity. The SuperAdmin
    /// console is read-only oversight across tenants, so it must never hand out navigation
    /// properties that would let a caller walk into a school's students, marks or finances.
    /// </summary>
    public class TenantSummaryDto
    {
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; } = "";
        public string? SchoolEmail { get; set; }
        public string? SchoolPhoneContact { get; set; }
        public string? SchoolWebsite { get; set; }
        public string? CurrencyCode { get; set; }
        public string? CurrencySymbol { get; set; }
        public bool HasLogo { get; set; }

        // ---- Size ----
        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
        public int ParentCount { get; set; }
        public int StaffCount { get; set; }
        public int ClassCount { get; set; }

        /// <summary>Identity accounts attached to this school. This is the figure a user limit is
        /// measured against, so it counts accounts, not people records.</summary>
        public int UserAccountCount { get; set; }

        public int AdminCount { get; set; }

        /// <summary>Accounts that exist but have been deactivated. Worth seeing next to the user
        /// count, because a licence is consumed by accounts an admin may have forgotten about.</summary>
        public int InactiveUserCount { get; set; }

        // ---- Licence ----
        public bool IsLicensed { get; set; } = true;

        /// <summary>False when the licensing service could not be reached. The app fails open, so
        /// a school in this state keeps working — but the console must not present an assumption as
        /// a fact.</summary>
        public bool LicenseVerified { get; set; }

        public string? LicenseStatus { get; set; }
        public DateTime? LicenseEndDate { get; set; }
        public int? DaysUntilExpiry { get; set; }
        public int? UserLimit { get; set; }

        // ---- Activity ----

        /// <summary>Most recent term end this school has configured. A school whose last term
        /// finished long ago is likely dormant, which is the cheapest dormancy signal available
        /// without touching per-school records.</summary>
        public DateTime? LatestTermEnd { get; set; }

        public string? ActiveTermName { get; set; }

        /// <summary>Newest student onboarding date — the other half of the dormancy picture.</summary>
        public DateTime? LastStudentOnboardedAt { get; set; }

        // ---- Derived state ----

        /// <summary>Verified, and verified as not valid. Only this state means anything is actually
        /// wrong; an unverified licence must never be shown as a failure.</summary>
        [JsonIgnore] public bool IsConfirmedUnlicensed => LicenseVerified && !IsLicensed;

        [JsonIgnore] public bool IsExpiringSoon => LicenseVerified && IsLicensed && DaysUntilExpiry is >= 0 and <= 30;

        /// <summary>Accounts in use against the limit, or null when no limit is known. Null means
        /// "not enforced" rather than "zero" — see LicenseStatusDto for why that distinction
        /// matters.</summary>
        [JsonIgnore] public double? SeatUsageRatio =>
            UserLimit is > 0 ? (double)UserAccountCount / UserLimit.Value : null;

        [JsonIgnore] public bool IsOverSeatLimit => UserLimit is > 0 && UserAccountCount > UserLimit.Value;

        [JsonIgnore] public bool IsNearSeatLimit => SeatUsageRatio is >= 0.85 and <= 1.0;

        /// <summary>A school with no admin account cannot be administered at all — nobody can sign
        /// in and run it. Worth surfacing as loudly as an expired licence.</summary>
        [JsonIgnore] public bool HasNoAdmin => AdminCount == 0;

        /// <summary>Registered but never populated: no students and no admin.</summary>
        [JsonIgnore] public bool IsUnusedShell => StudentCount == 0 && AdminCount == 0;

        /// <summary>Everything the console would raise a flag about, worst first. Computed here so
        /// the list, the detail dialog and the dashboard cannot disagree about what counts as a
        /// problem.</summary>
        [JsonIgnore]
        public List<TenantIssue> Issues
        {
            get
            {
                var issues = new List<TenantIssue>();

                if (IsConfirmedUnlicensed)
                    issues.Add(new TenantIssue(TenantIssueSeverity.Critical, "Not licensed",
                        LicenseStatus ?? "This school has no valid licence."));

                if (HasNoAdmin)
                    issues.Add(new TenantIssue(TenantIssueSeverity.Critical, "No administrator",
                        "Nobody can sign in to run this school."));

                if (IsOverSeatLimit)
                    issues.Add(new TenantIssue(TenantIssueSeverity.Critical, "Over user limit",
                        $"{UserAccountCount} accounts against a limit of {UserLimit}."));

                if (IsExpiringSoon)
                    issues.Add(new TenantIssue(TenantIssueSeverity.Warning, "Licence expiring",
                        $"Expires in {DaysUntilExpiry} day(s), on {LicenseEndDate:d}."));

                if (IsNearSeatLimit && !IsOverSeatLimit)
                    issues.Add(new TenantIssue(TenantIssueSeverity.Warning, "Approaching user limit",
                        $"{UserAccountCount} of {UserLimit} accounts used."));

                if (!LicenseVerified)
                    issues.Add(new TenantIssue(TenantIssueSeverity.Info, "Licence unverified",
                        "The licensing service could not be reached for this school."));

                if (IsUnusedShell)
                    issues.Add(new TenantIssue(TenantIssueSeverity.Info, "Not set up",
                        "Registered, but has no administrator and no students."));

                return issues;
            }
        }

        [JsonIgnore] public TenantIssueSeverity WorstSeverity =>
            Issues.Count == 0 ? TenantIssueSeverity.None : Issues.Max(i => i.Severity);
    }

    public enum TenantIssueSeverity
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Critical = 3,
    }

    public record TenantIssue(TenantIssueSeverity Severity, string Title, string Detail);

    /// <summary>Portfolio rollup for the SuperAdmin dashboard — the numbers that answer "is anything
    /// on fire" before drilling into any one school.</summary>
    public class TenantPortfolioDto
    {
        public List<TenantSummaryDto> Tenants { get; set; } = new();

        /// <summary>True when licence resolution was skipped for speed. The console must not draw
        /// licence conclusions from a payload that never asked.</summary>
        public bool LicensesIncluded { get; set; } = true;

        /// <summary>Schools whose licence could not be checked at all. Reported separately so an
        /// outage reads as an outage rather than as a portfolio full of unlicensed schools.</summary>
        [JsonIgnore] public int UnverifiedCount => Tenants.Count(t => LicensesIncluded && !t.LicenseVerified);

        [JsonIgnore] public int TotalSchools => Tenants.Count;
        [JsonIgnore] public int LicensedCount => Tenants.Count(t => t.LicenseVerified && t.IsLicensed);
        [JsonIgnore] public int UnlicensedCount => Tenants.Count(t => t.IsConfirmedUnlicensed);
        [JsonIgnore] public int ExpiringSoonCount => Tenants.Count(t => t.IsExpiringSoon);
        [JsonIgnore] public int OverSeatLimitCount => Tenants.Count(t => t.IsOverSeatLimit);
        [JsonIgnore] public int NoAdminCount => Tenants.Count(t => t.HasNoAdmin);

        [JsonIgnore] public int TotalStudents => Tenants.Sum(t => t.StudentCount);
        [JsonIgnore] public int TotalTeachers => Tenants.Sum(t => t.TeacherCount);
        [JsonIgnore] public int TotalUserAccounts => Tenants.Sum(t => t.UserAccountCount);

        /// <summary>Schools with at least one issue, worst first, for the "needs attention" list.</summary>
        [JsonIgnore] public List<TenantSummaryDto> NeedsAttention => Tenants
            .Where(t => t.WorstSeverity >= TenantIssueSeverity.Warning)
            .OrderByDescending(t => t.WorstSeverity)
            .ThenBy(t => t.DaysUntilExpiry ?? int.MaxValue)
            .ThenBy(t => t.SchoolName)
            .ToList();
    }

    /// <summary>One school in full: its summary, its administrator accounts, and its licence history.</summary>
    public class TenantDetailDto
    {
        public TenantSummaryDto Summary { get; set; } = new();
        public List<TenantAdminDto> Admins { get; set; } = new();
        public List<TenantLicenseDto> LicenseHistory { get; set; } = new();

        /// <summary>Set when licence history could not be read. Distinguished from an empty history,
        /// which legitimately means "never licensed".</summary>
        public string? LicenseHistoryError { get; set; }
    }

    public class TenantAdminDto
    {
        public string UserId { get; set; } = "";
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool AccountActive { get; set; }
        public bool RequiresPasswordReset { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsLockedOut { get; set; }

        [JsonIgnore] public string DisplayName =>
            string.IsNullOrWhiteSpace($"{FirstName}{LastName}")
                ? (UserName ?? Email ?? UserId)
                : $"{FirstName} {LastName}".Trim();
    }

    /// <summary>A licence record as the licensing service stores it. Property names match that
    /// service's payload so it deserialises directly.</summary>
    public class TenantLicenseDto
    {
        public Guid LicenseId { get; set; }
        public Guid ClientId { get; set; }
        public string? LicenseKey { get; set; }
        public string? PlanType { get; set; }
        public int UserLimit { get; set; }
        public string? Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [JsonIgnore] public bool IsCurrent =>
            string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase)
            && EndDate >= DateTime.UtcNow.Date;
    }

    /// <summary>Wrapper the licensing service returns from GetClientLicenses.</summary>
    public class TenantLicenseListDto
    {
        public int TotalRecords { get; set; }
        public List<TenantLicenseDto> Licenses { get; set; } = new();
    }

    /// <summary>
    /// Request to license a school.
    ///
    /// Only the school id, plan, seats and dates come from the client. Name, email and phone are
    /// filled in server-side from the school record, so the console cannot register a licence
    /// against a client identity of its own invention.
    /// </summary>
    public class ActivateTenantLicenseRequest
    {
        public Guid SchoolId { get; set; }
        public string PlanType { get; set; } = "annual";
        public int UserLimit { get; set; } = 50;
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);
    }

    public class TerminateTenantLicenseRequest
    {
        public Guid SchoolId { get; set; }
        public string LicenseKey { get; set; } = "";
    }

    /// <summary>Result of a SuperAdmin action. Always returned with HTTP 200 — the console needs to
    /// tell a refusal apart from a transport failure, and a bare status code cannot.</summary>
    public class TenantActionResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = "";

        public static TenantActionResult Ok(string message) => new() { Succeeded = true, Message = message };
        public static TenantActionResult Fail(string message) => new() { Succeeded = false, Message = message };
    }

    /// <summary>
    /// A password reset, carrying the generated password so the caller can mail it.
    ///
    /// The password is returned exactly once and is never stored in readable form — the account
    /// itself holds only the hash.
    /// </summary>
    public class AdminPasswordResetResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = "";
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? OneTimePassword { get; set; }
    }
}
