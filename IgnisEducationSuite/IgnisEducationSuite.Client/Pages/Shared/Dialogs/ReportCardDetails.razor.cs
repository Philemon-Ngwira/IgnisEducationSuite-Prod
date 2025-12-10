using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using EDUSphereSharedProject.Models.StoreProModels;
using System.Net.Http.Json;

namespace IgnisEducationSuite.Client.Pages.Shared.Dialogs
{
    public partial class ReportCardDetails
    {

        [CascadingParameter] IMudDialogInstance MudDialog { get; set; }
        [Parameter] public List<GetReportCardDetailsResult> Results { get; set; } = new();
        [Parameter] public List<GetAttendanceSummaryResult> attendances { get; set; } = new();
        [Parameter] public GetReportCardsByStudentResult student { get; set; } = new();
        [Inject] AuthenticationStateProvider authenticationStateProvider { get; set; }
        [Inject] NavigationManager _navigationManager { get; set; } = default!;
        [Inject] ClientEmailService _emailService { get; set; } = default!;
        [Inject] AppState AppState { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;
        protected bool isLoading = false;
        protected async Task PrepareAndMailDocument()
        {
            try
            {
                isLoading = true;

                // Only allow Students/Parents to generate PDF
                if (AppState.UserRole != "Student" && AppState.UserRole != "Parent")
                {
                    Snackbar.Add("Only students or parents can download their report card.", Severity.Warning);
                    return;
                }

                // Build dynamic HTML or pass structured data to server
                var dto = new ReportCardPdfDTO
                {
                    FirstName = student.FirstName ?? string.Empty,
                    LastName = student.LastName ?? string.Empty,
                    LevelName = student.LevelName ?? string.Empty,
                    GPA = student.GPA,
                    SchoolLogo = AppState.SchoolLogo ?? string.Empty,
                    SchoolName = AppState.SchoolName ?? string.Empty,
                    SchoolEmail = student.SchoolEmail ?? string.Empty,
                    SchoolWebsite = student.SchoolWebsite ?? string.Empty,
                    Results = Results.Select(r => new ReportCardResultDTO
                    {
                        ClassName = r.ClassName ?? string.Empty,
                        Score = r.Score,
                        Grade = r.Grade ?? string.Empty,
                        Final = r.Final ?? string.Empty
                    }).ToList(),
                    Attendances = attendances.Select(a => new AttendanceDTO
                    {
                        AttendanceCount = a.AttendanceCount,
                        ExpectedAttendances = a.ExpectedAttendances
                    }).ToList(),
                    PointsInBestSix = student.PointsInBestSix ?? 0,
                    MarksInBestSix = student.MarksInBestSix ?? 0,
                    PositionInClass = student.PositionInClass ?? 0,
                    DeanName = student.DeanName ?? string.Empty,
                    DeansComment = student.DeansComment ?? string.Empty,
                    PrincipleName = student.PrincipleName ?? string.Empty,
                    PrinciplesComment = student.PrinciplesComment ?? string.Empty,
                    Term = student.Term ?? string.Empty,
                    ReportCardType = student.ReportCardType ?? string.Empty
                };



                // Call API to generate PDF (server-side)
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(
                    $"{_navigationManager.BaseUri}api/Upload/generatePDF", dto);

                if (!response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Failed to generate the report card PDF. Please try again.", Severity.Error);
                    return;
                }

                // Get PDF bytes
                var pdfBytes = await response.Content.ReadAsByteArrayAsync();

                // Send email
                var auth = await authenticationStateProvider.GetAuthenticationStateAsync();
                var user = auth.User;
                var EmailToMail = "";
                var emailDto = new ReportCardEmailDTO
                {
                    EmailTo = "ngwira.philemon@gmail.com",
                    PdfBytes = pdfBytes
                };

                var emailSent = await _emailService.SendReportCardAsync(emailDto, _navigationManager.BaseUri);

                if (emailSent == "Report Card Email Sent Successfully")
                {
                    Snackbar.Add("Report card sent to your email successfully.", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add("Failed to send the report card email. Please contact support.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Snackbar.Add("An unexpected error occurred while sending the report card.", Severity.Error);
            }
            finally
            {
                isLoading = false;
            }
        }


    }
}