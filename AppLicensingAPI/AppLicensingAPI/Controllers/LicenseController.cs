using AppLicensingAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LicensingAPI.Controllers
{

    [ApiController]
    [Route("api/license")]
    public class LicenseController : ControllerBase
    {
        private readonly LicensingAPIContext _context;
        private readonly ILicensingAPIContextProcedures _procedures;
        public LicenseController(LicensingAPIContext licenseContext, ILicensingAPIContextProcedures licensingAPIContextProcedures)
        {
            _context = licenseContext;
            _procedures = licensingAPIContextProcedures;
        }
        [HttpPost("RewardReferrer")]
        public async Task<IActionResult> RewardReferrer([FromBody] Guid ReferrerLicenseID)
        {
            if (ReferrerLicenseID == Guid.Empty)
            {
                return BadRequest("ClientId and PlanType are required.");
            }
            try
            {
                var existingLicense = await _context.Licenses.FindAsync(ReferrerLicenseID);
                if (existingLicense != null)
                {
                    existingLicense.EndDate = existingLicense.EndDate.AddMonths(1);

                     _context.Update(existingLicense);
                    await _context.SaveChangesAsync();
                    return Ok(true);
                }
                else
                {
                    return Ok(false);
                }
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                return StatusCode(500, $"Internal server error: {ex.Message}");
                throw;
            }
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
                var clientId = Guid.Parse(request.ClientId);
                var existingClient = await _context.Clients.FindAsync(clientId);
                if (existingClient == null)
                {
                    var newClient = new Clients
                    {
                        ClientId = clientId,
                        Name = request.ClientName,
                        CreatedDate = DateTime.Now,
                        Email = request.Email,
                        PhoneNumber = request.Phone, // Add other properties as needed
                        ProjectLicense = request.Application
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
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
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
        [HttpGet("GetLicenseLimit")]
        public IActionResult LicenseLimit([FromQuery] Guid ClientID)
        {
            if (ClientID == Guid.Empty)
            {
                return BadRequest("ClientId is required.");

            }
            try
            {
                var returnable = _context.Licenses.Where(x => x.ClientId == ClientID && x.Status == "Active").FirstOrDefault();
                LicenseSlots newSlot = new();
                newSlot.UserLimit = returnable.UserLimit;
                return Ok(newSlot);
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                return StatusCode(500, $"Internal server error: {ex.Message}");

            }
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

        [HttpGet("GetClientActiveLicensePeriod/{ClientID}")]
        public async Task<IActionResult> GetActiveLicensePeriod(string ClientID)
        {
            if (string.IsNullOrEmpty(ClientID))
            {
                return BadRequest("ClientId is required.");
            }
            else
            {
                var returnable = await _procedures.usp_GetCompanyLicenseStatusAsync(Guid.Parse(ClientID));
                return Ok(returnable);
            }
        }
    }
    public class LicenseSlots
    {
        public int UserLimit { get; set; }
    }
    public class ActivateLicenseRequest
    {
        public required string ClientId { get; set; } // The unique identifier of the client
        public required string PlanType { get; set; } // e.g., Monthly, Quarterly, Biannual, Annual
        public int UserLimit { get; set; }   // Maximum number of users for this license
        public required string ClientName { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public required string Application { get; set; }
        public DateTime StartDate { get; set; } // License start date
        public DateTime EndDate { get; set; }   // License end date
    }

    public class TerminateLicenseRequest
    {
        public required string LicenseKey { get; set; }
    }
}