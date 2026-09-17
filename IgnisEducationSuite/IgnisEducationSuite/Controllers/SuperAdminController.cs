using EDUSphereSharedProject.UniversalModels.SuperAdmin;
using IgnisEducationSuite.Client.Services;
using IgnisEducationSuite.Data;
using IgnisEducationSuite.ServerServices.SuperAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    /// <summary>
    /// The SuperAdmin tenant console.
    ///
    /// Every endpoint requires the SuperAdmin role — the role check lives here rather than in the
    /// page, because a page-level check only hides buttons.
    ///
    /// The console is read-only with respect to a school's own records. Nothing here returns
    /// students, marks, attendance or finances; a SuperAdmin sees how many and how healthy, never
    /// who. The write operations are limited to two things that are genuinely the operator's job:
    /// a school's licence, and whether its administrator accounts can sign in.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly TenantOversightService _tenants;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SuperAdminController> _logger;

        public SuperAdminController(
            TenantOversightService tenants,
            UserManager<ApplicationUser> userManager,
            ILogger<SuperAdminController> logger)
        {
            _tenants = tenants;
            _userManager = userManager;
            _logger = logger;
        }

        // -----------------------------------------------------------------------------
        // Oversight
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Every school with its size and licence state.
        /// </summary>
        /// <param name="withLicenses">
        /// Set false for a fast repaint where only the counts matter. The response says which it
        /// was, so the console never draws licence conclusions from a payload that did not ask.
        /// </param>
        /// <param name="refresh">Bypasses the licence cache — what the console's Refresh button
        /// means. Without it a refresh re-renders the same cached answer for up to the cache window.</param>
        [HttpGet("tenants")]
        public async Task<IActionResult> GetTenants(
            [FromQuery] bool withLicenses = true,
            [FromQuery] bool refresh = false,
            CancellationToken ct = default)
        {
            try
            {
                return Ok(await _tenants.GetPortfolioAsync(withLicenses, refresh, ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load the tenant portfolio");
                return Ok(new TenantPortfolioDto { LicensesIncluded = false });
            }
        }

        [HttpGet("tenants/{schoolId:guid}")]
        public async Task<IActionResult> GetTenant(Guid schoolId, CancellationToken ct = default)
        {
            var detail = await _tenants.GetTenantAsync(schoolId, ct);
            return detail is null ? NotFound() : Ok(detail);
        }

        // -----------------------------------------------------------------------------
        // Licensing
        // -----------------------------------------------------------------------------

        /// <summary>Licenses or renews a school. Returns 200 with a result object either way —
        /// a refusal and a transport failure must not look the same to the console.</summary>
        [HttpPost("license/activate")]
        public async Task<IActionResult> ActivateLicense([FromBody] ActivateTenantLicenseRequest request)
            => Ok(await _tenants.ActivateLicenseAsync(request));

        [HttpPost("license/terminate")]
        public async Task<IActionResult> TerminateLicense([FromBody] TerminateTenantLicenseRequest request)
            => Ok(await _tenants.TerminateLicenseAsync(request));

        // -----------------------------------------------------------------------------
        // Administrator accounts
        // -----------------------------------------------------------------------------

        [HttpGet("tenants/{schoolId:guid}/admins")]
        public async Task<IActionResult> GetAdmins(Guid schoolId, CancellationToken ct = default)
            => Ok(await _tenants.ListAdminsAsync(schoolId, ct));

        /// <summary>
        /// Suspends an administrator's access.
        ///
        /// The school id is part of the route and checked against the account, so a mistyped or
        /// stale user id cannot suspend somebody at a different school.
        /// </summary>
        [HttpPost("tenants/{schoolId:guid}/admins/{userId}/deactivate")]
        public async Task<IActionResult> DeactivateAdmin(Guid schoolId, string userId)
        {
            var user = await FindScopedUserAsync(schoolId, userId);
            if (user is null) return Ok(TenantActionResult.Fail("That account was not found at this school."));

            // A SuperAdmin locking themselves out of their own console is not a state worth allowing.
            if (string.Equals(user.Id, _userManager.GetUserId(User), StringComparison.Ordinal))
                return Ok(TenantActionResult.Fail("You cannot deactivate your own account."));

            user.AccountActive = false;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            var result = await _userManager.UpdateAsync(user);

            return Ok(result.Succeeded
                ? TenantActionResult.Ok($"{user.UserName} can no longer sign in.")
                : TenantActionResult.Fail(Describe(result)));
        }

        [HttpPost("tenants/{schoolId:guid}/admins/{userId}/activate")]
        public async Task<IActionResult> ActivateAdmin(Guid schoolId, string userId)
        {
            var user = await FindScopedUserAsync(schoolId, userId);
            if (user is null) return Ok(TenantActionResult.Fail("That account was not found at this school."));

            user.AccountActive = true;
            user.LockoutEnd = null;

            var result = await _userManager.UpdateAsync(user);

            return Ok(result.Succeeded
                ? TenantActionResult.Ok($"{user.UserName} can sign in again.")
                : TenantActionResult.Fail(Describe(result)));
        }

        /// <summary>
        /// Issues a new one-time password and returns it so the caller can mail it.
        ///
        /// Generated server-side rather than by the page. It also persists requiresPasswordReset —
        /// AdminController's equivalent sets that flag on the entity but never saves it, so users
        /// reset that way were never actually forced to choose a new password.
        /// </summary>
        [HttpPost("tenants/{schoolId:guid}/admins/{userId}/reset-password")]
        public async Task<IActionResult> ResetAdminPassword(Guid schoolId, string userId)
        {
            var user = await FindScopedUserAsync(schoolId, userId);
            if (user is null) return Ok(new AdminPasswordResetResult { Succeeded = false, Message = "That account was not found at this school." });

            var oneTimePassword = new PasswordGenerator().GenerateOneTimePassword();

            await _userManager.RemovePasswordAsync(user);
            var added = await _userManager.AddPasswordAsync(user, oneTimePassword);

            if (!added.Succeeded)
            {
                _logger.LogWarning("Password reset failed for {UserId}: {Errors}", userId, Describe(added));
                return Ok(new AdminPasswordResetResult { Succeeded = false, Message = Describe(added) });
            }

            user.requiresPasswordReset = true;
            await _userManager.UpdateAsync(user);

            return Ok(new AdminPasswordResetResult
            {
                Succeeded = true,
                Message = "A one-time password has been issued.",
                UserName = user.UserName,
                Email = user.Email,
                OneTimePassword = oneTimePassword,
            });
        }

        /// <summary>Looks the account up and confirms it belongs to the school in the route.</summary>
        private async Task<ApplicationUser?> FindScopedUserAsync(Guid schoolId, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user is not null && user.SchoolID == schoolId ? user : null;
        }

        private static string Describe(IdentityResult result) =>
            result.Errors.Any()
                ? string.Join(" ", result.Errors.Select(e => e.Description))
                : "The change could not be saved.";
    }
}
