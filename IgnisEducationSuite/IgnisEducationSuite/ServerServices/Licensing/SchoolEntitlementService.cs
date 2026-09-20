using EduSphereDomain.Data;
using EduSphereDomain.Repositories;
using EDUSphereSharedProject.LicensingModel;
using IgnisEducationSuite.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IgnisEducationSuite.ServerServices.Licensing
{
    /// <summary>
    /// What one school's own administrator is entitled to, and how much of it they have used.
    ///
    /// The school is resolved from the signed-in user, never from a parameter, so this cannot be
    /// pointed at another school. That is the difference between this and the SuperAdmin console's
    /// equivalent: same figures, but scoped by identity rather than chosen by the caller.
    /// </summary>
    public class SchoolEntitlementService
    {
        /// <summary>Matches the licence cache's default. The plan a school is on does not change
        /// between page loads, and this runs on the dashboard.</summary>
        private static readonly TimeSpan PlanCacheDuration = TimeSpan.FromMinutes(30);

        private readonly PhoenixEdusphereContext _domain;
        private readonly ApplicationDbContext _identity;
        private readonly EduSphereRepository _repository;
        private readonly LicenseService _licenses;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SchoolEntitlementService> _logger;

        public SchoolEntitlementService(
            PhoenixEdusphereContext domain,
            ApplicationDbContext identity,
            EduSphereRepository repository,
            LicenseService licenses,
            IMemoryCache cache,
            ILogger<SchoolEntitlementService> logger)
        {
            _domain = domain;
            _identity = identity;
            _repository = repository;
            _licenses = licenses;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Never throws and never returns null. A failure here would otherwise take down a dashboard
        /// tile, and a licence the app could not read must not look like a licence the app does not
        /// have — the unverified fallback keeps IsLicensed true and Verified false.
        /// </summary>
        public async Task<SchoolLicenseSummaryDto> GetForUserAsync(string requestingUserId, bool refresh = false, CancellationToken ct = default)
        {
            var summary = new SchoolLicenseSummaryDto
            {
                Status = "Could not be checked",
            };

            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null || schoolId == Guid.Empty)
            {
                summary.Status = "No school on the account";
                return summary;
            }

            await ApplyLicenceAsync(summary, schoolId.Value, refresh);
            await ApplyUsageAsync(summary, schoolId.Value, ct);

            return summary;
        }

        private async Task ApplyLicenceAsync(SchoolLicenseSummaryDto summary, Guid schoolId, bool refresh)
        {
            try
            {
                var status = await _licenses.GetLicenseStatusAsync(schoolId, refresh);

                summary.IsLicensed = status.IsLicensed;
                summary.Verified = status.Verified;
                summary.Status = status.Status;
                summary.EndDate = status.EndDate;
                summary.DaysUntilExpiry = status.DaysUntilExpiry;
                summary.StudentLimit = status.UserLimit;
                summary.CheckedAtUtc = status.CheckedAtUtc;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Licence check failed for school {SchoolId}", schoolId);

                summary.IsLicensed = true;
                summary.Verified = false;
                summary.Status = "Could not be verified";
            }

            await ApplyPlanDetailsAsync(summary, schoolId, refresh);
        }

        /// <summary>
        /// Plan name and start date, which the status call does not carry.
        ///
        /// Cached on the same window as the licence itself. This sits on the admin dashboard — every
        /// admin, every visit — and the underlying licence-history endpoint is deliberately
        /// uncached for the SuperAdmin console, where staleness after an activation would mislead.
        /// Calling it straight from here would have put an uncached outbound request on a hot path.
        ///
        /// Best-effort throughout: an administrator can work without knowing the plan's name, but
        /// not without knowing whether the licence is valid, and that part is already resolved.
        /// </summary>
        private async Task ApplyPlanDetailsAsync(SchoolLicenseSummaryDto summary, Guid schoolId, bool refresh)
        {
            var key = $"school-plan::{schoolId}";

            if (refresh) _cache.Remove(key);

            try
            {
                var current = await _cache.GetOrCreateAsync(key, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = PlanCacheDuration;

                    return (await _licenses.GetClientLicensesAsync(schoolId, pageNumber: 1, pageSize: 10))
                        .FirstOrDefault(l => l.IsCurrent);
                });

                if (current is not null)
                {
                    summary.PlanType = current.PlanType;
                    summary.StartDate = current.StartDate;
                    summary.EndDate ??= current.EndDate;
                    summary.StudentLimit ??= current.UserLimit > 0 ? current.UserLimit : null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read plan details for school {SchoolId}", schoolId);
            }
        }

        private async Task ApplyUsageAsync(SchoolLicenseSummaryDto summary, Guid schoolId, CancellationToken ct)
        {
            try
            {
                summary.StudentCount = await _domain.Students.CountAsync(s => s.SchoolID == schoolId, ct);
                summary.TeacherCount = await _domain.Teachers.CountAsync(t => t.SchoolID == schoolId, ct);
                summary.ParentCount = await _domain.Parents.CountAsync(p => p.SchoolID == schoolId, ct);
                summary.StaffCount = await _domain.Staff.CountAsync(s => s.SchoolID == schoolId, ct);
                summary.UserAccountCount = await _identity.Users.CountAsync(u => u.SchoolID == schoolId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read usage counts for school {SchoolId}", schoolId);
            }
        }

        private async Task<Guid?> ResolveSchoolIdAsync(string requestingUserId)
        {
            if (string.IsNullOrWhiteSpace(requestingUserId)) return null;

            var initData = await _repository.GetInitializationDataResults(requestingUserId);
            return initData?.FirstOrDefault()?.SchoolID;
        }
    }
}
