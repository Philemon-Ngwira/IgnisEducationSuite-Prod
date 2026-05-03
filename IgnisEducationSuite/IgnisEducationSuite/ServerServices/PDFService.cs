using EDUSphereSharedProject.UniversalModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;

namespace IgnisEducationSuite.ServerServices
{
    public class PDFService
    {
        static PDFService()
        {
            QuestPDF.Settings.UseEnvironmentFonts = false;
            QuestPDF.Settings.FontDiscoveryPaths.Clear();
            QuestPDF.Settings.FontDiscoveryPaths.Add(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts"));

            var fontsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts");
            FontManager.RegisterFontWithCustomName("HeaderFont", File.OpenRead(Path.Combine(fontsPath, "Montserrat-Bold.ttf")));
            FontManager.RegisterFontWithCustomName("BodyFont", File.OpenRead(Path.Combine(fontsPath, "Roboto-Regular.ttf")));
            FontManager.RegisterFontWithCustomName("BodyItalic", File.OpenRead(Path.Combine(fontsPath, "Roboto-Italic.ttf")));
        }

        public byte[] GeneratePdf(ReportCardPdfDTO reportCard)
        {
            return Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("BodyFont").FontSize(12));

                    // WATERMARK
                    page.Background().Element(c => ComposeWatermark(c, reportCard));

                    // HEADER
                    page.Header().Element(c => ComposeHeader(c, reportCard));

                    // CONTENT
                    page.Content().Element(c => ComposeContent(c, reportCard));

                    // FOOTER
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        // ============================
        // WATERMARK
        // ============================
        private void ComposeWatermark(IContainer container, ReportCardPdfDTO reportCard)
        {
            container.AlignCenter()
                     .AlignMiddle()
                     .Rotate(-45)
                     .Text(reportCard.SchoolName ?? "")
                     .FontFamily("HeaderFont")
                     .FontSize(120)
                     .Bold()
                     .FontColor(Colors.Blue.Darken2.WithAlpha(0.05f))
                     .AlignCenter();
        }

        // ============================
        // HEADER
        // ============================
        private void ComposeHeader(IContainer c, ReportCardPdfDTO reportCard)
        {
            c.Column(col =>
            {
                col.Spacing(5);

                col.Item().Row(row =>
                {
                    row.ConstantItem(90)
                       .Height(60)
                       .Image(GetImage(reportCard.SchoolLogo), ImageScaling.FitHeight);

                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(reportCard.SchoolName)
                            .FontFamily("HeaderFont").Bold().FontSize(22).FontColor(Colors.Blue.Darken2).AlignCenter();

                        if (!string.IsNullOrWhiteSpace(reportCard.SchoolPhone))
                            inner.Item().Text($"Phone: {reportCard.SchoolPhone}").FontSize(11).AlignCenter();

                        if (!string.IsNullOrWhiteSpace(reportCard.SchoolEmail))
                            inner.Item().Text($"Email: {reportCard.SchoolEmail}").FontSize(11).AlignCenter();

                        if (!string.IsNullOrWhiteSpace(reportCard.SchoolWebsite))
                            inner.Item().Text($"Website: {reportCard.SchoolWebsite}").FontSize(11).AlignCenter();
                    });
                });

                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }


        // ============================
        // CONTENT
        // ============================
        private void ComposeContent(IContainer container, ReportCardPdfDTO reportCard)
        {
            container.Column(col =>
            {
                // STUDENT INFO HEADER
                col.Item().Element(c => StudentInfoHeader(c, reportCard));

                // GRADES TABLE
                col.Item().PaddingTop(15).Text("Grades").FontSize(14).Bold().Underline();
                col.Item().Element(c => BuildGradesTable(c, reportCard));

                // ATTENDANCE TABLE
                col.Item().PaddingTop(15).Text("Attendance").FontSize(14).Bold().Underline();
                col.Item().Element(c => BuildAttendanceTable(c, reportCard));

                // BEST-SIX ROW
                col.Item().PaddingTop(15).Element(c => BuildBestSixRow(c, reportCard));

                // COMMENTS
                if (!string.IsNullOrWhiteSpace(reportCard.DeanName) ||
                    !string.IsNullOrWhiteSpace(reportCard.PrincipleName))
                {
                    col.Item().PaddingTop(20).Text("Comments").FontSize(14).Bold().Underline();

                    if (!string.IsNullOrWhiteSpace(reportCard.DeanName))
                        col.Item().Text($"Dean ({reportCard.DeanName}): {reportCard.DeansComment}")
                            .FontFamily("BodyItalic");

                    if (!string.IsNullOrWhiteSpace(reportCard.PrincipleName))
                        col.Item().Text($"Principal ({reportCard.PrincipleName}): {reportCard.PrinciplesComment}")
                            .FontFamily("BodyItalic");
                }
            });
        }

        // ============================
        // STUDENT INFO HEADER
        // ============================
        private void StudentInfoHeader(IContainer c, ReportCardPdfDTO reportCard)
        {
            c.Background(Colors.Blue.Lighten5)
             .Padding(15)
             .CornerRadius(8)
             .Border(1)
             .BorderColor(Colors.Blue.Lighten3)
             .Column(col =>
             {
                 col.Spacing(5);
                 col.Item().Text($"{reportCard.FirstName} {reportCard.LastName}")
                    .FontFamily("HeaderFont")
                    .FontSize(20)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                 col.Item().Row(r =>
                 {
                     r.RelativeItem().Text($"Grade Level: {reportCard.LevelName}").FontColor(Colors.Grey.Darken2);
                     r.RelativeItem().Text($"Term: {reportCard.Term}").FontColor(Colors.Grey.Darken2);
                     r.RelativeItem().Text($"Report Type: {reportCard.ReportCardType}").FontColor(Colors.Grey.Darken2);
                 });

                 col.Item().Row(r =>
                 {
                     r.RelativeItem().Text($"School Year: {reportCard.IssuedDate?.Year}").FontColor(Colors.Grey.Darken2);
                     // r.RelativeItem().Text($"Position In Calss: {reportCard.PositionInClass?.ToString("F2") ?? "-"}").FontColor(Colors.Grey.Darken2);
                 });
             });
        }

        // ============================
        // BEST SIX ROW
        // ============================
        private void BuildBestSixRow(IContainer c, ReportCardPdfDTO reportCard)
        {
            c.Background(Colors.Blue.Lighten4)
             .Border(1)
             .BorderColor(Colors.Blue.Lighten3)
             .CornerRadius(8)
             .Padding(12)
             .Row(row =>
             {
                 if (reportCard.isGCE)
                 {
                     row.RelativeItem().Text($"Points in Best Six: GCE")
                         .Bold().FontColor(Colors.Blue.Darken2);
                     row.RelativeItem().Text($"Marks in Best Six: {reportCard.MarksInBestSix ?? 0}")
                         .Bold().FontColor(Colors.Blue.Darken2);
                     row.RelativeItem().Text($"Position in Class: GCE")
                    .Bold().FontColor(Colors.Blue.Darken2);
                 }
                 else
                 {
                     row.RelativeItem().Text($"Points in Best Six: {reportCard.PointsInBestSix ?? 0}")
                     .Bold().FontColor(Colors.Blue.Darken2);

                     row.RelativeItem().Text($"Marks in Best Six: {reportCard.MarksInBestSix ?? 0}")
                         .Bold().FontColor(Colors.Blue.Darken2);

                     row.RelativeItem().Text($"Position in Class: {reportCard.PositionInClass ?? 0}")
                         .Bold().FontColor(Colors.Blue.Darken2);
                 }
             });
        }

        // ============================
        // GRADES TABLE
        // ============================
        private void BuildGradesTable(IContainer container, ReportCardPdfDTO reportCard)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Subject
                    columns.RelativeColumn();   // Score (%)
                    columns.RelativeColumn();   // Grade
                });

                table.Header(header =>
                {
                    string[] headers = { "Subject", "Score (%)", "Grade" };
                    foreach (var h in headers)
                        header.Cell().Element(cell =>
                        {
                            cell.Background(Colors.Blue.Darken3)
                                .Padding(8)
                                .Border(1, Colors.Blue.Lighten2)
                                .Text(h).FontColor(Colors.White).Bold();
                        });
                });

                bool gray = false;
                foreach (var item in reportCard.Results)
                {
                    table.Cell().Element(c => c.Background(gray ? Colors.Grey.Lighten4 : Colors.White)
                                                      .Padding(6)
                                                      .Text(item.ClassName));

                    // Append % to the existing Score
                    var scoreText = item.Score.HasValue ? $"{item.Score:F2}%" : "-";
                    table.Cell().Element(c => c.Background(gray ? Colors.Grey.Lighten4 : Colors.White)
                                                      .Padding(6)
                                                      .Text(scoreText));

                    table.Cell().Element(c => c.Background(gray ? Colors.Grey.Lighten4 : Colors.White)
                                                      .Padding(6)
                                                      .Text(item.Grade ?? "-"));

                    gray = !gray;
                }
            });
        }

        // ============================
        // ATTENDANCE TABLE
        // ============================
        private void BuildAttendanceTable(IContainer container, ReportCardPdfDTO reportCard)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(col =>
                {
                    col.RelativeColumn();
                    col.RelativeColumn();
                });

                table.Header(h =>
                {
                    h.Cell().Element(c => c.Background(Colors.Blue.Darken2).Padding(8).Text("Attended").FontColor(Colors.White).Bold());
                    h.Cell().Element(c => c.Background(Colors.Blue.Darken2).Padding(8).Text("Absences").FontColor(Colors.White).Bold());
                });

                bool gray = false;
                foreach (var a in reportCard.Attendances)
                {
                    table.Cell().Element(c => c.Background(gray ? Colors.Grey.Lighten4 : Colors.White).Padding(6).Text(a.AttendanceCount.ToString()));
                    table.Cell().Element(c => c.Background(gray ? Colors.Grey.Lighten4 : Colors.White).Padding(6).Text((a.ExpectedAttendances - a.AttendanceCount).ToString()));
                    gray = !gray;
                }
            });
        }

        // ============================
        // IMAGE UTIL
        // ============================
        private byte[] GetImage(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return Array.Empty<byte>();
            var cleaned = base64.Contains(",") ? base64.Split(',')[1] : base64;
            return Convert.FromBase64String(cleaned);
        }
    }
}
