using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using EduSphereDomain.Repositories;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.ServerServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenAI.Assistants;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;


namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly PDFService _pDFService;
        private readonly EduSphereRepository _repository;
        public UploadController(PDFService pDFService, EduSphereRepository repository)
        {
            _pDFService = pDFService;
            _repository = repository;
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
            var parser = new WordParser();
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
        public async Task<IActionResult> Upload([FromForm] IFormFile file,
    [FromForm] string SchoolID)
        {
            var schedules = new List<ScheduleMappingClass>();
            var schoolStructure = await _repository.GetAcademicLevelsAsync(SchoolID.ToString());
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

                    var gradeLevel = schoolStructure.Where(x => x.LevelName.ToUpper() == gradeLevelStr.ToUpper()).Select(x => x.LevelInt).FirstOrDefault();

                    // Parse times safely
                    TimeSpan.TryParse(startTimeStr, out TimeSpan startTime);
                    TimeSpan.TryParse(endTimeStr, out TimeSpan endTime);

                    var schedule = new ScheduleMappingClass
                    {
                        Class = className,
                        GradeLevel = (int)gradeLevel,
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

        [HttpPost("uploadStudents")]
        public async Task<IActionResult> UploadStudents([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var students = new List<Student>();
            var errors = new List<RowError>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            foreach (var row in worksheet.RowsUsed().Skip(1).Select((r, i) => new { Row = r, RowIndex = i + 2 }))
            {
                DateTime dateValue;
                try
                {
                    string firstName = row.Row.Cell(1).GetValue<string>()?.Trim();
                    string lastName = row.Row.Cell(2).GetValue<string>()?.Trim();
                    string gender = row.Row.Cell(3).GetValue<string>()?.Trim();
                    string address = row.Row.Cell(4).GetValue<string>()?.Trim();
                    string email = row.Row.Cell(5).GetValue<string>()?.Trim();
                    string studentNumber = row.Row.Cell(6).GetValue<string>()?.Trim();
                    var dateCell = row.Row.Cell(7).GetValue<string>()?.Trim();
                    string country = row.Row.Cell(8).GetValue<string>()?.Trim();
                    string city = row.Row.Cell(9).GetValue<string>()?.Trim();
                    string gradeSection = row.Row.Cell(10).GetValue<string>()?.Trim();
                    string levelName = row.Row.Cell(11).GetValue<string>()?.Trim();
                    bool paymentStatus = row.Row.Cell(12).GetValue<int>() == 1;

                    // Validate required fields
                    if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
                        string.IsNullOrEmpty(studentNumber) || string.IsNullOrEmpty(levelName))
                        throw new Exception("Missing required field.");

                    // Validate gender
                    if (string.IsNullOrEmpty(gender))
                        gender = "Male";
                    if (gender != "Male" && gender != "Female")
                        throw new Exception("Gender must be 'Male' or 'Female'.");
                    var dateOnBoarded = (DateTime?)null;
                    if (!string.IsNullOrEmpty(dateCell) && DateTime.TryParse(dateCell, out dateValue))
                    {
                        dateOnBoarded = dateValue;
                    }
                    else
                    {
                        dateOnBoarded = DateTime.Today;
                    }
                    // Add to preview list (no UserID yet)
                    students.Add(new Student
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Gender = gender,
                        Address = address,
                        Email = email,
                        StudentNumber = studentNumber,
                        DateOnBoarded = dateOnBoarded ?? DateTime.Today,
                        Country = country,
                        City = city,
                        GradeSection = gradeSection,
                        LevelName = levelName,
                        PaymentStatus = paymentStatus,
                    });
                }
                catch (Exception exRow)
                {
                    errors.Add(new RowError { RowIndex = row.RowIndex, Message = exRow.Message });
                }
            }

            return Ok(new
            {
                Preview = students,
                Errors = errors
            });
        }

        [HttpPost("uploadTeachers")]
        public async Task<IActionResult> UploadTeachers([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var teachers = new List<Teacher>();
            var errors = new List<RowError>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            foreach (var row in worksheet.RowsUsed().Skip(1)
                         .Select((r, i) => new { Row = r, RowIndex = i + 2 }))
            {
                try
                {
                    string firstName = row.Row.Cell(1).GetValue<string>()?.Trim();
                    string lastName = row.Row.Cell(2).GetValue<string>()?.Trim();
                    string email = row.Row.Cell(3).GetValue<string>()?.Trim();
                    string contactNo = row.Row.Cell(4).GetValue<string>()?.Trim();
                    string gender = row.Row.Cell(5).GetValue<string>()?.Trim();
                    string dateCell = row.Row.Cell(6).GetValue<string>()?.Trim();
                    string nationality = row.Row.Cell(7).GetValue<string>()?.Trim();
                    string city = row.Row.Cell(8).GetValue<string>()?.Trim();
                    string employeeId = row.Row.Cell(9).GetValue<string>()?.Trim();

                    // Required fields
                    if (string.IsNullOrWhiteSpace(firstName) ||
                        string.IsNullOrWhiteSpace(lastName) ||
                        string.IsNullOrWhiteSpace(email) ||
                        string.IsNullOrWhiteSpace(employeeId))
                    {
                        throw new Exception("Missing required field.");
                    }

                    // Gender validation
                    if (string.IsNullOrWhiteSpace(gender))
                        gender = "Male";

                    if (gender != "Male" && gender != "Female")
                        throw new Exception("Gender must be 'Male' or 'Female'.");

                    // DateEngaged
                    DateTime dateEngaged;
                    if (!DateTime.TryParse(dateCell, out dateEngaged))
                        dateEngaged = DateTime.Today;

                    teachers.Add(new Teacher
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        EmailAddress = email,
                        ContactNo = contactNo,
                        Gender = gender,
                        DateEngaged = dateEngaged,
                        Nationality = nationality,
                        City = city,
                        EmployeeID = employeeId
                    });
                }
                catch (Exception exRow)
                {
                    errors.Add(new RowError
                    {
                        RowIndex = row.RowIndex,
                        Message = exRow.Message
                    });
                }
            }

            return Ok(new UploadPreviewResult
            {
                TeacherPreview = teachers,
                Errors = errors
            });
        }

        [HttpPost("uploadParents")]
        public async Task<IActionResult> UploadParents([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var parents = new List<Parent>();
            var errors = new List<RowError>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            foreach (var row in worksheet.RowsUsed().Skip(1)
                         .Select((r, i) => new { Row = r, RowIndex = i + 2 }))
            {
                try
                {
                    string firstName = row.Row.Cell(1).GetValue<string>()?.Trim();
                    string lastName = row.Row.Cell(2).GetValue<string>()?.Trim();
                    string email = row.Row.Cell(3).GetValue<string>()?.Trim();
                    string phone = row.Row.Cell(4).GetValue<string>()?.Trim();
                    string gender = row.Row.Cell(5).GetValue<string>()?.Trim();
                    string address = row.Row.Cell(6).GetValue<string>()?.Trim();
                    string country = row.Row.Cell(7).GetValue<string>()?.Trim();
                    string city = row.Row.Cell(8).GetValue<string>()?.Trim();
                    string studentNumbersRaw = row.Row.Cell(9).GetValue<string>()?.Trim();

                    // Required fields
                    if (string.IsNullOrWhiteSpace(firstName) ||
                        string.IsNullOrWhiteSpace(lastName) ||
                        string.IsNullOrWhiteSpace(phone))
                    {
                        throw new Exception("Missing required field.");
                    }

                    // Gender validation
                    if (string.IsNullOrWhiteSpace(gender))
                        gender = "Male";

                    if (gender != "Male" && gender != "Female")
                        throw new Exception("Gender must be 'Male' or 'Female'.");

                    parents.Add(new Parent
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        PhoneNumber = phone,
                        Gender = gender,
                        Address = address,
                        Country = country,
                        City = city,

                        // TEMP storage for preview / later resolution
                        StudentNumbersRaw = studentNumbersRaw
                    });
                }
                catch (Exception exRow)
                {
                    errors.Add(new RowError
                    {
                        RowIndex = row.RowIndex,
                        Message = exRow.Message
                    });
                }
            }

            return Ok(new UploadPreviewResult
            {
                ParentPreview = parents,
                Errors = errors
            });
        }


        [HttpPost("uploadPayments")]
        public async Task<IActionResult> UploadPayments([FromForm] IFormFile file, [FromForm] string schoolId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var unmatchedRows = new List<string>();
            var updatedPayments = new List<(string StudentNumber, bool IsPaid)>();

            // Load all students for the school in memory
            var students = await _repository.GetStudentsBySchool(schoolId);
            if (students == null || !students.Any())
                return BadRequest("No students found for the specified school.");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1); // first sheet

            // Iterate rows, skip header
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                string studentNumber = row.Cell(1).GetValue<string>()?.Trim();
                string paymentStatusRaw = row.Cell(5).GetValue<string>()?.Trim()?.ToLower();

                if (string.IsNullOrEmpty(studentNumber) || string.IsNullOrEmpty(paymentStatusRaw))
                {
                    unmatchedRows.Add($"Row {row.RowNumber()}: Missing student number or payment status.");
                    continue;
                }

                if (!new[] { "paid", "unpaid" }.Contains(paymentStatusRaw))
                {
                    unmatchedRows.Add($"Row {row.RowNumber()}: Invalid payment status '{paymentStatusRaw}'. Must be 'Paid' or 'Unpaid'.");
                    continue;
                }

                var student = students.FirstOrDefault(s => s.StudentNumber.Equals(studentNumber, StringComparison.OrdinalIgnoreCase));
                if (student == null)
                {
                    unmatchedRows.Add($"Row {row.RowNumber()}: Student '{studentNumber}' not found.");
                    continue;
                }

                updatedPayments.Add((student.StudentNumber, paymentStatusRaw == "paid"));
            }

            // If any unmatched rows exist, return them without updating DB
            if (unmatchedRows.Any())
            {
                return BadRequest(new
                {
                    message = "Some rows could not be matched. Please fix the issues and re-upload.",
                    errors = unmatchedRows
                });
            }

            // Update all matched students
            foreach (var p in updatedPayments)
            {
                await _repository.UpdateStudentPaymentStatus(p.StudentNumber, p.IsPaid);
            }

            // Optionally: trigger stored procedure to disable unpaid accounts
            //await _repository.DisableAccountsForUnpaidStudents(schoolId);

            return Ok(new
            {
                message = "Payments uploaded successfully.",
                updatedCount = updatedPayments.Count
            });
        }


        #endregion

        #region ReportCards
        [HttpPost("generatePDF")]
        public IActionResult GeneratePdf([FromBody] ReportCardPdfDTO reportCard)
        {
            if (reportCard == null)
                return BadRequest("Report card data is required.");

            var pdfBytes = _pDFService.GeneratePdf(reportCard);

            return File(pdfBytes, "application/pdf", "ReportCard.pdf");
        }
        #endregion
    }
}
