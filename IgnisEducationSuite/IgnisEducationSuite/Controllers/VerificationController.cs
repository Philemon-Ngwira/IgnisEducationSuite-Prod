using EDUSphereSharedProject.LicensingModel;
using IgnisEducationSuite.ServerServices;
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
        public async Task<IActionResult> GenerateLicense(ActivateLicenseRequest activateLicenseRequest)
        {
            var result = await _licenseService.ActivateLicenseAsync(activateLicenseRequest);
            return Ok(result);
        }

        [HttpGet("Validate/{ClientID}")]
        public async Task<IActionResult> ValidateLicense(string ClientID)
        {
            var result = await _licenseService.ValidateLicenseAsync(ClientID);
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
