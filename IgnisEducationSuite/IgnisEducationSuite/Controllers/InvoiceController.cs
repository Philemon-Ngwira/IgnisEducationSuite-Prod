using EduSphereDomain.Repositories;
using EDUSphereSharedProject.FinanceModels;
using EDUSphereSharedProject.UniversalModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Authorize(Roles = "Admin, Finance")]
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly FinananceRepository _finananceRepository;

        public InvoiceController(FinananceRepository finananceRepository)
        {
            _finananceRepository = finananceRepository;
        }

        [HttpGet("GetInvoiceTypes")]
        public async Task<IActionResult> GetInvoiceTypes()
        {
            var result = await _finananceRepository.GetInvoiceTypes();
            return Ok(result);
        }
        [HttpGet("GetStudentFinances/{SchoolID}")]
        public async Task<IActionResult> GetStudentFinances(Guid SchoolID)
        {
            var result = await _finananceRepository.GetStudentFinances(SchoolID);
            return Ok(result);
        }


        [HttpPost("generate-bulk")]
        public async Task<IActionResult> GenerateBulkInvoices(
            [FromBody] BulkInvoiceRequest request)
        {
            var schoolId = request.SchoolID;

            var result = await _finananceRepository
                .GenerateBulkInvoicesAsync(request, schoolId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new
            {
                Message = result.Message,
                Count = result.Count
            });
        }

    }
}
