using EduSphereDomain.FinanceData;
using EduSphereDomain.Repositories;
using EDUSphereSharedProject.FinanceModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Authorize(Roles = "Admin, Finance, Parent, Student")]
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {

        private readonly FinananceRepository _finananceRepository;


        public PaymentController(FinananceRepository repository)
        {
            _finananceRepository = repository;
        }
        [HttpPost("SavePayment")]
        public async Task<IActionResult> SavePayment([FromBody] Payment paymentData)
        {
            var result = await _finananceRepository.RecordPayment(paymentData);
            return Ok(result);
        }

        [HttpGet("GetStudentPayments/{UserID}")]
        public async Task<IActionResult> GetStudentPayments(string UserID)
        {
            var result = await _finananceRepository.GetStudentPayments(UserID);
            return Ok(result);
        }
    }
}
