using System;

namespace EDUSphereSharedProject.LicensingModel
{
    /// <summary>
    /// One school's licence state, resolved in a single call.
    ///
    /// Combines what used to be two round trips (validity period and user limit) so start-up costs
    /// one request instead of several, and so the app never holds a half-known licence state.
    ///
    /// Note the distinction between <see cref="IsLicensed"/> and <see cref="Verified"/>: the app
    /// fails OPEN. If the licensing service cannot be reached, IsLicensed stays true so a school is
    /// never locked out of its own records by an outage, while Verified is false so admins can be
    /// told the check did not actually happen.
    /// </summary>
    public class LicenseStatusDto
    {
        /// <summary>Whether the app should behave as licensed. True when verified valid, and also
        /// true when verification failed — see the class remarks.</summary>
        public bool IsLicensed { get; set; } = true;

        /// <summary>False when the licensing service could not be reached or returned an error, so
        /// IsLicensed is an assumption rather than a fact.</summary>
        public bool Verified { get; set; }

        /// <summary>"Active", "Expired or Terminated", or a message explaining a failed check.</summary>
        public string? Status { get; set; }

        public DateTime? EndDate { get; set; }

        public int? DaysUntilExpiry { get; set; }

        /// <summary>
        /// Maximum user accounts allowed, or null when unknown.
        ///
        /// Null means "do not enforce" — deliberately distinct from 0. The licensing API returns a
        /// 500 for a client with no active licence, and treating that as a limit of zero silently
        /// blocked all user creation, including for a school still being onboarded.
        /// </summary>
        public int? UserLimit { get; set; }

        /// <summary>Shown to admins when Verified is false or the licence is close to expiry.</summary>
        public string? Notice { get; set; }

        /// <summary>When this result was produced, so a cached value can be reasoned about.</summary>
        public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>True when the licence is valid but expiring soon enough to warn about.</summary>
        public bool IsExpiringSoon => Verified && IsLicensed && DaysUntilExpiry is >= 0 and <= 14;

        /// <summary>True when verification succeeded and said the licence is not valid. Only this
        /// state should restrict anything — an unverified licence must not.</summary>
        public bool IsConfirmedUnlicensed => Verified && !IsLicensed;
    }
}
