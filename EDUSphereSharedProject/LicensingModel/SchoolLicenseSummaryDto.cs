using System;
using System.Text.Json.Serialization;

namespace EDUSphereSharedProject.LicensingModel
{
    /// <summary>
    /// What a school's own administrator is entitled to, and how much of it is in use.
    ///
    /// Distinct from <see cref="LicenseStatusDto"/>, which answers "may this app run". This answers
    /// "what can I do with it": how many student places are left, when the licence ends, and which
    /// actions are currently blocked.
    ///
    /// The school is resolved server-side from the signed-in user, so this type carries no school
    /// id and no endpoint accepts one — an administrator cannot ask about another school's licence.
    /// </summary>
    public class SchoolLicenseSummaryDto
    {
        // ---- Licence ----

        /// <summary>Whether the app should behave as licensed. True while unverified as well: the
        /// app fails open, so an outage never locks a school out of its own records.</summary>
        public bool IsLicensed { get; set; } = true;

        /// <summary>False when the licensing service could not be reached, making IsLicensed an
        /// assumption rather than a fact.</summary>
        public bool Verified { get; set; }

        public string? Status { get; set; }
        public string? PlanType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? DaysUntilExpiry { get; set; }

        /// <summary>
        /// Student places included in the licence, or null when no limit is known.
        ///
        /// Null means "not enforced" and is deliberately distinct from 0. A licensing service that
        /// cannot be reached must not read as a limit of zero, which would block a school still
        /// being set up from adding anybody.
        /// </summary>
        public int? StudentLimit { get; set; }

        // ---- Usage ----
        //
        // StudentCount is the figure the limit is actually measured against — see
        // UserManagement's create-user path. The rest is context for the administrator.

        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
        public int ParentCount { get; set; }
        public int StaffCount { get; set; }
        public int UserAccountCount { get; set; }

        /// <summary>When this was worked out. The licence half is cached server-side, so it can be
        /// a little older than the counts.</summary>
        public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;

        // ---- Derived ----

        [JsonIgnore] public bool IsConfirmedUnlicensed => Verified && !IsLicensed;

        [JsonIgnore] public bool IsExpiringSoon => Verified && IsLicensed && DaysUntilExpiry is >= 0 and <= 30;

        /// <summary>Places left, or null when there is no limit to count against.</summary>
        [JsonIgnore] public int? StudentPlacesRemaining =>
            StudentLimit is > 0 ? Math.Max(0, StudentLimit.Value - StudentCount) : null;

        [JsonIgnore] public double? UsageRatio =>
            StudentLimit is > 0 ? (double)StudentCount / StudentLimit.Value : null;

        [JsonIgnore] public bool IsAtOrOverLimit => StudentLimit is > 0 && StudentCount >= StudentLimit.Value;

        [JsonIgnore] public bool IsNearLimit => UsageRatio is >= 0.85 && !IsAtOrOverLimit;

        // ---- Capabilities ----
        //
        // Stated as answers rather than left for each screen to infer, so the dashboard cannot
        // promise something the create-user path then refuses.

        /// <summary>Only a licence verified as invalid blocks anything. Unverified must not.</summary>
        [JsonIgnore] public bool CanAddUsers => !IsConfirmedUnlicensed;

        [JsonIgnore] public bool CanAddStudents => CanAddUsers && !IsAtOrOverLimit;
    }
}
