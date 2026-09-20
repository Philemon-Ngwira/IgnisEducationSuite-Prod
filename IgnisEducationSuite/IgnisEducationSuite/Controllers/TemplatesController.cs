using EduSphereDomain.Repositories;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.ServerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemplatesController : ControllerBase
    {
        private readonly StudentPaymentUploadTemplate _template;
        private readonly EduSphereRepository _repository;
        public TemplatesController(StudentPaymentUploadTemplate template, EduSphereRepository repository)
        {
            _template = template;
            _repository = repository;
        }

        [HttpGet("DownloadPaymentTemplate/{SchoolID}")]
        public async Task<IActionResult> DownloadTemplate(Guid SchoolID)
        {
            // 1. Get all students
            var result = await _repository.GetStudentsBySchool(SchoolID);
            List<Student> students = result.ToList();
            // 2. Generate Excel template
            byte[] excelBytes = _template.GeneratePaymentStatusTemplate(students);

            // 3. Return file
            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PaymentStatusTemplate.xlsx"
            );
        }

        [HttpGet("DownloadScheduleTemplate")]
        public IActionResult DownloadTemplate()
        {

            byte[] excelBytes = _template.GenerateScheduleTemplate();

            // 3. Return file
            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ClassSchedule.xlsx"
            );
        }
        [HttpGet("DownloadStudentTemplate")]
        public IActionResult DownloadStudentTemplate()
        {
            byte[] excelBytes = _template.GenerateStudentTemplate();
            // 3. Return file
            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "StudentUploadTemplate.xlsx"
            );
        }
        [HttpGet("DownloadTeacherTemplate")]
        public IActionResult DownloadTeacherTemplate()
        {
            byte[] excelBytes = _template.GenerateTeacherTemplate();
            // 3. Return file
            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "TeacherUploadTemplate.xlsx"
            );
        }

        [HttpGet("DownloadFoodTemplate")]
        public IActionResult DownloadFoodTemplate()
        {
            byte[] excelBytes = _template.GenerateFoodItemTemplate();
            // 3. Return file
            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "FoodItemTemplate.xlsx"
            );
        }

        [HttpGet("DownloadParentTemplate")]
        public IActionResult DownloadParentTemplate()
        {
            byte[] excelBytes = _template.GenerateParentTemplate();
            return File(
               excelBytes,
               "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
               "ParentUploadTemplate.xlsx"
           );
        }

        [HttpGet("DownloadAcademicStructureTemplate")]
        public IActionResult DownloadAcademicStructureTemplate()
        {
            byte[] excelBytes = _template.GenerateAcademicStructureTemplate();
            return File(
              excelBytes,
              "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
              "AcademicStructureUploadTemplate.xlsx"
          );
        }

        [HttpGet("DownloadClinicInventoryTemplate")]
        public IActionResult DownloadClininInventoryTemplate()
        {
            byte[] excelBytes = _template.GenerateMedicalInventoryTemplate();
            return File(
                 excelBytes,
              "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
              "clinicInventoryTemplate.xlsx"
                );
        }

        [HttpGet("generateconsolidatedreport/{schoolId}/{term}")]
        public async Task<IActionResult> GenerateConsolidatedReport(Guid schoolId, string term)
        {
            var data = await _repository.GetConsolidatedReport(schoolId, term);

            if (data == null || !data.Any())
                return BadRequest("No data found");

            var fileBytes = _template.GenerateConsolidatedReport(data.ToList());

            var fileName = $"Consolidated_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}
