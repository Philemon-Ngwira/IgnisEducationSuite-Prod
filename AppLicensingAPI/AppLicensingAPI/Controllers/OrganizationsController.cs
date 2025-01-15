//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace AppLicensingAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class OrganizationsController : ControllerBase
//    {
//        private readonly LicensingDbContext _context;

//        public OrganizationController(LicensingDbContext context)
//        {
//            _context = context;
//        }

//        [HttpPost("register")]
//        public async Task<IActionResult> RegisterOrganization([FromBody] RegisterOrganizationRequest request)
//        {
//            var organization = new Organization
//            {
//                Name = request.Name,
//                ContactEmail = request.ContactEmail,
//                ContactPhone = request.ContactPhone
//            };

//            _context.Organizations.Add(organization);
//            await _context.SaveChangesAsync();

//            return Ok(new { OrganizationId = organization.Id });
//        }

//        [HttpGet("{id}")]
//        public async Task<IActionResult> GetOrganization(int id)
//        {
//            var organization = await _context.Organizations
//                .Include(o => o.Licenses)
//                .FirstOrDefaultAsync(o => o.Id == id);

//            if (organization == null) return NotFound("Organization not found.");
//            return Ok(organization);
//        }
//    }
//}
