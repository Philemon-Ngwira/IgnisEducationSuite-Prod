using Microsoft.AspNetCore.Mvc;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.ServerServices;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using ClosedXML.Excel;


namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly PDFService _pDFService;

        public UploadController(PDFService pDFService)
        {
            _pDFService = pDFService;
        }
        [HttpPost("uploadWord")]
        public async Task<IActionResult> UploadWordDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var filePath = Path.GetTempFileName();

            // Save the uploaded file temporarily
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Process the file to extract questions
            var parser = new EduSphereDomain.Repositories.WordParser();
            var questions = parser.ExtractQuestionsFromWord(filePath);

            // Delete the temporary file after use
            System.IO.File.Delete(filePath);

            return Ok(questions);
        }

        #region Lesson Endpoints
        [HttpPost("uploadLesson")]
        public async Task<IActionResult> UploadLesson(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty or not provided.");
            }

            string extractedText = string.Empty;

            if (file.FileName.EndsWith(".pdf"))
            {
                extractedText = await ExtractTextFromPdfAsync(file.OpenReadStream());
            }
            else if (file.FileName.EndsWith(".docx"))
            {
                extractedText = await ExtractTextFromWordAsync(file.OpenReadStream());
            }
            else
            {
                return BadRequest("Unsupported file type.");
            }

            return Ok(extractedText);
        }
        private async Task<string> ExtractTextFromPdfAsync(Stream fileStream)
        {
            StringBuilder text = new StringBuilder();

            using (PdfDocument document = PdfDocument.Open(fileStream))
            {
                foreach (Page page in document.GetPages())
                {
                    text.Append(page.Text);
                }
            }

            return text.ToString();
        }
        private async Task<string> ExtractTextFromWordAsync(Stream fileStream)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(fileStream, false))
            {
                var body = doc.MainDocumentPart.Document.Body;
                return body.InnerText;
            }
        }
        #endregion

        #region Excel
        [HttpPost("uploadSchedules")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var schedules = new List<ScheduleMappingClass>();

            if (file != null && file.Length > 0)
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1); // First sheet

                foreach (var row in worksheet.RowsUsed().Skip(1)) // Skip header
                {
                    // Safely read each cell
                    string className = row.Cell(1).GetValue<string>()?.Trim() ?? "Unknown";
                    string gradeLevelStr = row.Cell(2).GetValue<string>()?.Trim() ?? "0";
                    string gradeSection = row.Cell(3).GetValue<string>()?.Trim() ?? "";
                    string day = row.Cell(4).GetValue<string>()?.Trim() ?? "";
                    string startTimeStr = row.Cell(5).GetValue<string>()?.Trim() ?? "";
                    string endTimeStr = row.Cell(6).GetValue<string>()?.Trim() ?? "";

                    // Parse GradeLevel safely
                    int.TryParse(gradeLevelStr, out int gradeLevel);

                    // Parse times safely
                    TimeSpan.TryParse(startTimeStr, out TimeSpan startTime);
                    TimeSpan.TryParse(endTimeStr, out TimeSpan endTime);

                    var schedule = new ScheduleMappingClass
                    {
                        Class = className,
                        GradeLevel = gradeLevel,
                        GradeSection = gradeSection,
                        Day = day,
                        StartTime = startTime.ToString(),
                        EndTime = endTime.ToString()
                    };

                    schedules.Add(schedule);
                }
            }

            return Ok(schedules);
        }

        #endregion

        #region ReportCards
        [HttpPost("generatePDF")]
        public IActionResult GeneratePdf([FromBody] string htmlContent)
        {


            var pdfBytes = _pDFService.GeneratePdf(htmlContent);

            return File(pdfBytes, "application/pdf", "ReportCard.pdf");
        }
        #endregion
    }
}
