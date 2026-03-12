using EduSphereDomain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FinanceController : ControllerBase
    {
        private readonly FinananceRepository _finananceRepository;

        public FinanceController(FinananceRepository repository)
        {
            _finananceRepository = repository;
        }

        [HttpGet("GetDashboardSummary/{SchoolID}")]
        public async Task<IActionResult> GetDashboardSummary(Guid SchoolID)
        {
            var result = await _finananceRepository.GetDashboardSummary(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetFinancialTrends/{SchoolID}")]
        public async Task<IActionResult> GetFinancialTrends(Guid SchoolID)
        {
            var result = await _finananceRepository.GetMonthlyPaymentTrends(SchoolID);
            return Ok(result);
        }

        [HttpGet("GetRecentPayments/{SchoolID}")]
        public async Task<IActionResult> GetRecentPayments(Guid SchoolID)
        {
            var result = await _finananceRepository.GetRecentPaymentsAsync(SchoolID);
            return Ok(result);
        }
    }
}
