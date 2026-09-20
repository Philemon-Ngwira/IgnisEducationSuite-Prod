using System.Security.Claims;
using EDUSphereSharedProject.LicensingModel;
using IgnisEducationSuite.ServerServices;
using IgnisEducationSuite.ServerServices.Licensing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    /// <summary>
    /// Licence checks and licence administration.
    ///
    /// This controller was previously anonymous, which meant any caller who knew a school's id could
    /// activate or terminate that school's licence, and could read its user limit. Reading a licence
    /// now requires a signed-in user; changing one requires a SuperAdmin.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VerificationController : ControllerBase
    {
        private readonly LicenseService _licenseService;
        private readonly SchoolEntitlementService _entitlements;
        private readonly ILogger<VerificationController> _logger;

        public VerificationController(
            LicenseService licenseService,
            SchoolEntitlementService entitlements,
            ILogger<VerificationController> logger)
        {
            _licenseService = licenseService;
            _entitlements = entitlements;
            _logger = logger;
        }

        /// <summary>Issues a licence. Prefer the SuperAdmin console's activate endpoint, which fills
        /// in the client identity from the school record instead of trusting the caller for it.</summary>
        [HttpPost("Generate")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GenerateLicense([FromBody] ActivateLicenseRequest activateLicenseRequest)
        {
            try
            {
                var result = await _licenseService.ActivateLicenseAsync(activateLicenseRequest);

                if (Guid.TryParse(activateLicenseRequest.ClientId, out var clientId))
                    _licenseService.InvalidateCache(clientId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Licence activation failed for client {ClientId}", activateLicenseRequest.ClientId);
                return StatusCode(502, "The licensing service could not complete the activation.");
            }
        }

        [HttpGet("GetLicenseLimit/{ClientID}")]
        public async Task<IActionResult> GetLicenseLimit(Guid ClientID)
        {
            var result = await _licenseService.GetUserLimitAsync(ClientID);
            return Ok(result);
        }

        [HttpGet("Validate/{ClientID}")]
        public async Task<IActionResult> ValidateLicense(string ClientID)
        {
            var result = await _licenseService.ValidateLicenseAsync(ClientID);
            return Ok(result);
        }

        /// <summary>
        /// The single licence check the app uses at start-up: validity, expiry and user limit in one
        /// call, cached per school server-side.
        ///
        /// Always returns 200 with a status object. It never returns an error, because a failure to
        /// verify must not be indistinguishable from a failure to load — the caller needs to know
        /// the difference between "not licensed" and "could not check".
        /// </summary>
        [HttpGet("Status/{ClientID:guid}")]
        public async Task<IActionResult> GetStatus(Guid ClientID, [FromQuery] bool refresh = false)
        {
            var result = await _licenseService.GetLicenseStatusAsync(ClientID, refresh);
            return Ok(result);
        }

        /// <summary>
        /// The signed-in user's own school: licence terms, how many student places are used, and
        /// which actions are currently blocked.
        ///
        /// Takes no school id — it is resolved from the identity — so an administrator at one school
        /// cannot read another school's entitlement. Always 200: a dashboard tile that cannot load
        /// must not take the dashboard with it.
        /// </summary>
        [HttpGet("MySchool")]
        public async Task<IActionResult> GetMySchoolEntitlement([FromQuery] bool refresh = false, CancellationToken ct = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            return Ok(await _entitlements.GetForUserAsync(userId, refresh, ct));
        }

        [HttpPost("Terminate")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> TerminateLicense(TerminateLicenseRequest terminateLicenseRequest)
        {
            try
            {
                var result = await _licenseService.TerminateLicenseAsync(terminateLicenseRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Licence termination failed");
                return StatusCode(502, "The licensing service could not complete the termination.");
            }
        }
    }
}
