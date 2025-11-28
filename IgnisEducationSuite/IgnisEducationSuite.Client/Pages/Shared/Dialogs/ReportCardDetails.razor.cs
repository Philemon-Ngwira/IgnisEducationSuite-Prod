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
        protected bool isLoading = false;
        protected async Task PrepareAndMailDocument()
        {
            isLoading = true;

            string htmlContent = $@"
<html>
<head>
    <style>
        <style>
        /* General dialog container styles */
        .dialog-container {{
            overflow-y: auto; /* Allow vertical scrolling if content overflows */
            max-height: 80vh; /* Ensure the dialog fits the viewport */
            padding: 20px;
        }}

        /* Styling for the report card */
        .report-card {{
            width: 100%;
            margin: 0 auto;
            background-color: #fff;
            padding: 20px;
            box-shadow: 0 0 15px rgba(0, 0, 0, 0.2);
            border-radius: 8px;
        }}

        .header {{
            text-align: center;
            margin-bottom: 20px;
        }}

        .header img {{
            width: 70px;
            margin-bottom: 10px;
        }}

        .header h1 {{
            font-size: 28px;
            margin: 10px 0;
        }}

        .header p {{
            font-size: 14px;
            margin: 5px 0;
            color: #555;
        }}

        .student-info, .grades, .attendance {{
            margin-bottom: 20px;
        }}

        table {{
            width: 100%;
            border-collapse: collapse;
        }}

        th, td {{
            border: 1px solid #ddd;
            padding: 10px;
            text-align: left;
        }}

        th {{
            background-color: #f2f2f2;
            font-weight: bold;
            text-align: center;
        }}

        td {{
            text-align: center;
        }}

        .grades th, .attendance th, .grades td, .attendance td {{
            text-align: center;
        }}

        /* Responsive adjustments */
        @media (max-width: 768px) {{
            .report-card {{
                padding: 10px;
            }}

            .header h1 {{
                font-size: 20px;
            }}

            th, td {{
                font-size: 12px;
                padding: 5px;
            }}
        }}
    </style>
    </style>
</head>
<body>
    <div class='dialog-container'>
        <div class='report-card'>
            <div class='header'>
                <img src=""/images/LogoW.png"" alt='School Logo' />
                <h2>Ignis Education Suite</h2>
                <p>Phone: +260760581058</p>
                <p>Email: philitiara.enterprises@gmail.com</p>
                <p>Website: <a href=""www.philtiaraenterprises.com"">www.philtiaraenterprises.com</a></p>
            </div>
            <div class='student-info'>
                <table>
                    <tr>
                        <th>Name of Student:</th>
                        <td>{student.FirstName} {student.LastName}</td>
                        <th>School Year:</th>
                        <td>{student.IssuedDate.Value.Year}</td>
                    </tr>
                    <tr>
                        <th>Grade Level:</th>
                        <td>{student.GradeLevel}</td>
                        <th>GPA:</th>
                        <td>{student.GPA:F2}</td>
                    </tr>
                </table>
            </div>
            <div class='grades'>
                <table>
                    <thead>
                        <tr>
                            <th>SUBJECT</th>
                            <th>SCORE</th>
                            <th>GRADE</th>
                            <th>FINAL</th>
                        </tr>
                    </thead>
                    <tbody>
                {string.Join("", Results.Select(r => $@"
                        <tr>
                            <td>{r.ClassName}
                </td>
        <td>{r.Score}</td>
        <td>{r.Grade}</td>
        <td>{r.Final}</td>
                                        </tr>
                "))}
            </ tbody >
        </ table >
    </ div >
    < div class= 'attendance' >
        < table >
            < thead >
                < tr >
                    < th > ATTENDED </ th >
                    < th > ABSENCES </ th >
                </ tr >
            </ thead >
            < tbody >
                {string.Join("", attendances.Select(a => $@"
                        <tr>
                            <td>{a.AttendanceCount}</td>
                            <td>{a.ExpectedAttendances - a.AttendanceCount}</td>
                        </tr>
                        "))}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </body>
        </html>
";
            // Your dynamic HTML content

            var httpClient = new HttpClient();

            var response = await httpClient.PostAsJsonAsync($"{_navigationManager.BaseUri}api/Upload/generatePDF", htmlContent);

            if (response.IsSuccessStatusCode)
            {
                var auth = await authenticationStateProvider.GetAuthenticationStateAsync();
                var user = auth.User;
                var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                ReportCardEmailDTO reportCardEmailDTO = new ReportCardEmailDTO();


                reportCardEmailDTO.EmailTo = user.Identity.Name;
                reportCardEmailDTO.PdfBytes = pdfBytes;

                var emailSent = await _emailService.SendReportCardAsync(reportCardEmailDTO, _navigationManager.BaseUri);
                if (emailSent == "Report Card Email Sent Successfully")
                {
                    isLoading = false;
                    MudDialog.Close(DialogResult.Ok(true));
                }



            }

        }

    }
}