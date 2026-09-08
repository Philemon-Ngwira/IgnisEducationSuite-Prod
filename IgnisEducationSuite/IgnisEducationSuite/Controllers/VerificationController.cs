using EDUSphereSharedProject.LicensingModel;
using IgnisEducationSuite.ServerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VerificationController : ControllerBase
    {
        private readonly LicenseService _licenseService;
        public VerificationController(LicenseService licenseService)
        {
            _licenseService = licenseService;
        }
        [HttpPost("Generate")]
        public async Task<IActionResult> GenerateLicense([FromBody] ActivateLicenseRequest activateLicenseRequest)
        {
            var result = await _licenseService.ActivateLicenseAsync(activateLicenseRequest);
            return Ok(result);
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
        [HttpPost("Terminate")]
        public async Task<IActionResult> TerminateLicense(TerminateLicenseRequest terminateLicenseRequest)
        {
            var result = await _licenseService.TerminateLicenseAsync(terminateLicenseRequest);
            return Ok(result);
        }
    }
}
