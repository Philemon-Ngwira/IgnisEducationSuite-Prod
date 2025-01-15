using AppLicensingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace LicensingAPI.Controllers
{

    [ApiController]
    [Route("api/license")]
    public class LicenseController : ControllerBase
    {
        private readonly LicensingAPIContext _context;

        public LicenseController(LicensingAPIContext licenseContext)
        {
            _context = licenseContext;
        }

        [HttpPost("activate")]
        public async Task<IActionResult> ActivateLicense([FromBody] ActivateLicenseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.PlanType))
            {
                return BadRequest("ClientId and PlanType are required.");
            }

            try
            {
                var clientId = Guid.Parse(request.ClientId); var existingClient = await _context.Clients.FindAsync(clientId); if (existingClient == null)
                {
                    var newClient = new Clients
                    {
                        ClientId = clientId,
                        Name = request.ClientName, // Add other properties as needed
                    };
                    await _context.AddAsync<Clients>(newClient);
                    await _context.SaveChangesAsync();
                }
                var newLicense = new Licenses
                {
                    LicenseId = Guid.NewGuid(),
                    ClientId = Guid.Parse(request.ClientId),
                    LicenseKey = Guid.NewGuid().ToString(),
                    PlanType = request.PlanType.ToLower(),
                    UserLimit = request.UserLimit,
                    Status = "Active",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(GetPlanDuration(request.PlanType.ToLower()))
                };

                await _context.AddAsync<Licenses>(newLicense);
                await _context.SaveChangesAsync();

                return Ok(new { newLicense.LicenseKey, newLicense.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("validate")]
        public IActionResult ValidateLicense([FromQuery] string ClientID)
        {
            try
            {
                var clientGuid = Guid.Parse(ClientID);
                var license = _context.Licenses.Where(l => l.ClientId == clientGuid && l.Status == "Active" && l.EndDate >= DateTime.UtcNow).FirstOrDefault();
                if (license == null)
                {
                    return Ok(new { Status = "Expired or Terminated" });
                }



                return Ok(new { license.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("terminate")]
        public IActionResult TerminateLicense([FromBody] TerminateLicenseRequest request)
        {
            try
            {
                var license = _context.Licenses.FirstOrDefault(l => l.LicenseKey == request.LicenseKey);

                if (license == null)
                {
                    return NotFound("License not found.");
                }

                license.Status = "Terminated";
                _context.SaveChanges();

                return Ok("License terminated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private int GetPlanDuration(string planType)
        {
            return planType.ToLower() switch
            {
                "monthly" => 1,
                "quarterly" => 3,
                "biannual" => 6,
                "annual" => 12,
                _ => throw new ArgumentException("Invalid plan type")
            };
        }

        [HttpGet("GetClientLicenses/{ClientID}")]
        public IActionResult GetClientLicenses(Guid ClientID, int pageNumber = 1, int pageSize = 10)
        {
            if (ClientID == Guid.Empty)
            {
                return BadRequest("ClientId is required.");
            }

            try
            {
                var query = _context.Licenses.Where(x => x.ClientId == ClientID);
                var totalRecords = query.Count();
                var licenses = query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new { TotalRecords = totalRecords, Licenses = licenses });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class ActivateLicenseRequest
    {
        public string ClientId { get; set; }
        public string PlanType { get; set; }
        public int UserLimit { get; set; }
        public string ClientName { get; set; }
    }

    public class TerminateLicenseRequest
    {
        public string LicenseKey { get; set; }
    }
}
