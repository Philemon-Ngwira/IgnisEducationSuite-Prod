using ClosedXML.Excel;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels;

namespace IgnisEducationSuite.ServerServices
{
    public class StudentPaymentUploadTemplate
    {
        public byte[] GeneratePaymentStatusTemplate(List<Student> students)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("PaymentStatus");

            // ===== Header =====
            worksheet.Cell(1, 1).Value = "Student Number";
            worksheet.Cell(1, 2).Value = "First Name";
            worksheet.Cell(1, 3).Value = "Last Name";
            worksheet.Cell(1, 4).Value = "Academic Level";
            worksheet.Cell(1, 5).Value = "Payment Status";

            // Bold header
            worksheet.Range(1, 1, 1, 5).Style.Font.Bold = true;
            worksheet.Range(1, 1, 1, 5).Style.Fill.BackgroundColor = XLColor.LightGray;

            // ===== Fill student info =====
            for (int i = 0; i < students.Count; i++)
            {
                var row = i + 2;
                var student = students[i];
                worksheet.Cell(row, 1).Value = student.StudentNumber;
                worksheet.Cell(row, 2).Value = student.FirstName;
                worksheet.Cell(row, 3).Value = student.LastName;
                worksheet.Cell(row, 4).Value = student.LevelName;
            }

            int lastRow = students.Count + 1;

            // ===== Payment Status drop-down =====
            var statusList = new[] { "Paid", "Unpaid" };
            var statusRange = worksheet.Range(2, 5, lastRow, 5);
            var validation = statusRange.SetDataValidation();
            validation.List(string.Join(",", statusList));

            // ===== Conditional formatting =====
            // Paid = green
            var paidRule = statusRange.AddConditionalFormat();
            paidRule.WhenEquals("Paid").Fill.SetBackgroundColor(XLColor.LightGreen);

            // Unpaid = red
            var unpaidRule = statusRange.AddConditionalFormat();
            unpaidRule.WhenEquals("Unpaid").Fill.SetBackgroundColor(XLColor.LightPink);

            // ===== Lock all except Payment Status =====
            worksheet.Columns(1, 4).Style.Protection.SetLocked(true);
            worksheet.Column(5).Style.Protection.SetLocked(false);

            // ===== Protect sheet =====
            worksheet.Protect("IGNIS2025");

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }



        public byte[] GenerateScheduleTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("ScheduleTemplate");

            // Headers
            ws.Cell(1, 1).Value = "Class";
            ws.Cell(1, 2).Value = "Grade";
            ws.Cell(1, 3).Value = "GradeSection";
            ws.Cell(1, 4).Value = "Day";
            ws.Cell(1, 5).Value = "StartTime";
            ws.Cell(1, 6).Value = "EndTime";

            // Example data
            ws.Cell(2, 1).Value = "History";
            ws.Cell(2, 2).Value = 8;
            ws.Cell(2, 3).Value = ""; // optional
            ws.Cell(2, 4).Value = "Monday";
            ws.Cell(2, 5).Value = "8:00";
            ws.Cell(2, 6).Value = "9:00";

            ws.Cell(3, 1).Value = "Information Technology";
            ws.Cell(3, 2).Value = 8;
            ws.Cell(3, 3).Value = ""; // optional
            ws.Cell(3, 4).Value = "Monday";
            ws.Cell(3, 5).Value = "9:00";
            ws.Cell(3, 6).Value = "10:00";

            // Formatting headers
            var headerRange = ws.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Auto-fit columns
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] GenerateStudentTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("StudentTemplate");

            // Headers
            string[] headers = {
        "FirstName", "LastName", "Gender", "Address", "Email","StudentNumber",
        "DateOnBoarded", "Country", "City", "GradeSection", "LevelName", "PaymentStatus", "DayScholar?","GroupName"
    };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            // Example row
            ws.Cell(2, 1).Value = "Raquel";
            ws.Cell(2, 2).Value = "Rodriguez";
            ws.Cell(2, 3).Value = "Male"; // strictly Male/Female
            ws.Cell(2, 4).Value = "123 Main Street";
            ws.Cell(2, 5).Value = "example@gmail.com";
            ws.Cell(2, 6).Value = "ST-001";
            ws.Cell(2, 7).Value = DateTime.Today.ToShortDateString();
            ws.Cell(2, 8).Value = "Zambia";
            ws.Cell(2, 9).Value = "Lusaka";
            ws.Cell(2, 10).Value = "A"; // GradeSection
            ws.Cell(2, 11).Value = "Grade 8"; // LevelName
            ws.Cell(2, 12).Value = 0; // PaymentStatus default false
            ws.Cell(2, 13).Value = 0;//daySchool Status
            ws.Cell(2, 14).Value = "Group 1"; // Any Special Groups

            // Formatting headers  
            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Auto-fit columns
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
        public byte[] GenerateFoodItemTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("FoodItemTemplate");

            // Headers
            string[] headers =
            {
        "Name",
        "Category",           // Perishable / Non-perishable
        "HasFixedExpiry",     // 0 = No, 1 = Yes
        "DefaultShelfLife",   // Days (nullable)
        "Unit",               // Selected from predefined list
        "Notes"
    };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            // Example rows
            ws.Cell(2, 1).Value = "Tomatoes";
            ws.Cell(2, 2).Value = "Perishable";
            ws.Cell(2, 3).Value = 0;
            ws.Cell(2, 4).Value = 5;
            ws.Cell(2, 5).Value = "kg";
            ws.Cell(2, 6).Value = "Store in cool dry place";

            ws.Cell(3, 1).Value = "Milk";
            ws.Cell(3, 2).Value = "Perishable";
            ws.Cell(3, 3).Value = 1;
            ws.Cell(3, 4).Value = ""; // must NOT be null
            ws.Cell(3, 5).Value = "L";
            ws.Cell(3, 6).Value = "Keep refrigerated";

            // Header formatting
            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // -----------------------------
            // DATA VALIDATION (CORRECT)
            // -----------------------------

            // Category dropdown
            var categoryDv = ws.Range("B2:B1000").SetDataValidation();
            categoryDv.Clear();
            categoryDv.List("Perishable,Non-perishable");
            categoryDv.ErrorTitle = "Invalid Category";
            categoryDv.ErrorMessage = "Choose Perishable or Non-perishable.";

            // HasFixedExpiry (0 / 1)
            var expiryDv = ws.Range("C2:C1000").SetDataValidation();
            expiryDv.Clear();
            expiryDv.WholeNumber.Between(0, 1);
            expiryDv.ErrorTitle = "Invalid Value";
            expiryDv.ErrorMessage = "Enter 0 (No) or 1 (Yes).";

            // DefaultShelfLife (days)
            var shelfLifeDv = ws.Range("D2:D1000").SetDataValidation();
            shelfLifeDv.Clear();
            shelfLifeDv.WholeNumber.Between(1, 3650);
            shelfLifeDv.ErrorTitle = "Invalid Shelf Life";
            shelfLifeDv.ErrorMessage =
                "Shelf life must be a number of days. Leave blank if not applicable.";

            // Unit dropdown (predefined)
            var unitDv = ws.Range("E2:E1000").SetDataValidation();
            unitDv.Clear();
            unitDv.List("L,g,loaf,kg,pkt,pcs,ml,bag");
            unitDv.ErrorTitle = "Invalid Unit";
            unitDv.ErrorMessage = "Select a unit from the list.";

            // Auto-fit columns
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] GenerateTeacherTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("TeacherTemplate");

            // Headers (ONLY what the admin must provide)
            string[] headers =
            {
        "FirstName",
        "LastName",
        "EmailAddress",
        "ContactNo",
        "Gender",
        "DateEngaged",
        "Nationality",
        "City",
        "EmployeeID"
    };

            // Write headers
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            // Example row (very important for UX)
            ws.Cell(2, 1).Value = "Mary";
            ws.Cell(2, 2).Value = "Banda";
            ws.Cell(2, 3).Value = "mary.banda@school.ac.zm";
            ws.Cell(2, 4).Value = "0978123456";
            ws.Cell(2, 5).Value = "Female"; // strictly Male/Female
            ws.Cell(2, 6).Value = DateTime.Today.ToShortDateString(); // DateEngaged
            ws.Cell(2, 7).Value = "Zambian";
            ws.Cell(2, 8).Value = "Lusaka";
            ws.Cell(2, 9).Value = "TCH-001";

            // Header styling
            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // Optional: lock header row
            ws.SheetView.FreezeRows(1);

            // Auto-fit
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] GenerateParentTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("ParentTemplate");

            // Headers (ONLY what admin must provide)
            string[] headers =
            {
        "FirstName",
        "LastName",
        "Email",
        "PhoneNumber",
        "Gender",
        "Address",
        "Country",
        "City",
        "StudentNumbers"
    };

            // Write headers
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            // Example row (VERY important for usability)
            ws.Cell(2, 1).Value = "Mary";
            ws.Cell(2, 2).Value = "Phiri";
            ws.Cell(2, 3).Value = "mary.phiri@gmail.com";
            ws.Cell(2, 4).Value = "0978123456";
            ws.Cell(2, 5).Value = "Female"; // strictly Male/Female
            ws.Cell(2, 6).Value = "Plot 123, Kabulonga";
            ws.Cell(2, 7).Value = "Zambia";
            ws.Cell(2, 8).Value = "Lusaka";
            ws.Cell(2, 9).Value = "ST-001, ST-014, ST-078"; // comma-separated student numbers

            // Header styling
            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // Freeze header row
            ws.SheetView.FreezeRows(1);

            // Auto-fit columns
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] GenerateAcademicStructureTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("AcademicStructureTemplate");

            // Headers (ONLY what admin must provide)
            string[] headers =
            {
        "Levelint",
        "LevelName",
        "GroupName",
        "SortNumber",
    };

            // Write headers
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            // Example row (VERY important for usability)
            ws.Cell(2, 1).Value = "1";
            ws.Cell(2, 2).Value = "Grade 1";
            ws.Cell(2, 3).Value = "Primary";
            ws.Cell(2, 4).Value = "1";


            // Header styling
            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // Freeze header row
            ws.SheetView.FreezeRows(1);

            // Auto-fit columns
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] GenerateMedicalInventoryTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("ClinicInventoryTemplate");

            // Headers (ONLY what admin must provide)
            string[] headers =
            {
        "Name",
        "Stock",
        "Unit",
        "ExpiryDate",
    };

            // Write headers
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            // Example row (VERY important for usability)
            ws.Cell(2, 1).Value = "Paracetamol Syrup";
            ws.Cell(2, 2).Value = 500;
            ws.Cell(2, 3).Value = "ml";
            ws.Cell(2, 4).Value = DateTime.Today.AddYears(1);


            // Header styling
            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // Freeze header row
            ws.SheetView.FreezeRows(1);

            // Auto-fit columns
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }




    }
}
