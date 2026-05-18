using EduSphereDomain.Repositories;
using EDUSphereSharedProject.FinanceModels;
using EDUSphereSharedProject.FinanceModels.DTOs;
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
        [HttpPost("GetStudentLedgers")]
        public async Task<IActionResult> GetStudentLedgers([FromBody] FinanceLedgerRequestDTO requestDTO)
        {
            var result = await _finananceRepository.GetStudentLedgersAsync(requestDTO);
            return Ok(result);
        }
        [HttpPost("UpdateFeeStructure")]
        public async Task<IActionResult> UpdateFeeStructure(UpdateFeeStructureDto dto)
        {
            var result = await _finananceRepository.UpdateFeeStructureAsync(dto);
            return Ok(result);
        }
        [HttpPost("SaveFeeStructure")]
        public async Task<IActionResult> SaveFeeStructure(FeeStructure feeStructure)
        {
            var result = await _finananceRepository.SaveFeeStructureAsync(feeStructure);
            return Ok(result);
        }
        [HttpPost("SaveNewBucket")]
        public async Task<IActionResult> SaveNewFeeBucket(FeeBucket bucket)
        {
            var result = await _finananceRepository.SaveFeeBucket(bucket);
            return Ok(result);
        }
        [HttpGet("GetRecentPayments/{SchoolID}")]
        public async Task<IActionResult> GetRecentPayments(Guid SchoolID)
        {
            var result = await _finananceRepository.GetRecentPaymentsAsync(SchoolID);
            return Ok(result);
        }

        [HttpGet("GetFeeStructure/{SchoolID}")]
        public async Task<IActionResult> GetFeeStructure(Guid SchoolID)
        {
            var result = await _finananceRepository.GetFeeStructures(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetFeeStructureItems/{StructureID}")]
        public async Task<IActionResult> GetFeeStructureItems(Guid StructureID)
        {
            var result = await _finananceRepository.GetFeeStructureItems(StructureID);
            return Ok(result);
        }

        [HttpGet("GetStudentsByParent/{ParentID}")]
        public async Task<IActionResult> GetStudentsByParent(Guid ParentID)
        {
            var result = await _finananceRepository.GetStudentsByParent(ParentID);
            return Ok(result);
        }
    }
}