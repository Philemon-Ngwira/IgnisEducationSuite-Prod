using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.PieChart;
using EDUSphereSharedProject.Models;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using EDUSphereSharedProject.Models.StoreProModels;
using Serilog;
using static System.Net.WebRequestMethods;
using System.Net.Http.Json;

namespace IgnisEducationSuite.Client.Pages.Shared
{
    public partial class LandingPage
    {

        [Inject] AppState AppState { get; set; } = default!;
        [Inject] HttpClient http { get; set; }
        private bool isLoading = false;
        private bool isAuthenticated = false;
        private BarConfig StudentPerformanceChartConfig { get; set; }
        private BarConfig StudentCountryChartConfig { get; set; }
        private PieConfig StudentCityChartConfig { get; set; }
        private int Index = -1; //default value cannot be 0 -> first selectedindex is 0.
        protected List<vw_TopPerformingTeacher> teacherPerfomances = new();
        protected List<vw_ClassLessonSummary> HighRatedLessons = new();
        protected GetLessonCountBySchoolResult lesson = new();
        protected List<TimeSlot> timeSlots = new();
        protected List<GetStudentPerformanceForCurrentYearResult> YearlyStudentPerfomance = new();
        protected List<GetStudentUnCompletedLessonsResult> uncompletedLessons = new();
        protected List<GetStudentClassScheduleResult> Currentschedules = new();
        protected List<GetStudentAttendanceByUserIDAndEventDateResult> attendances = new();
        protected List<GetAttendanceTrendForPastSevenDaysResult> attendancesTrends = new();
        protected List<GetMissedClassesForPastWeekResult> missedClasses = new();
        protected List<GetStudentDemographicsCountryResult> StudentDemographicsCountries = new();
        protected List<GetStudentDemographicsResult> StudentDemographicsCity = new();
        protected List<GetTop5TeachersByHighRatedLessonsResult> _top5Teachers = new();
        [Inject] ISnackbar Snackbar { get; set; }
        private readonly List<string> dayOrder = new List<string>

{
    "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
};
        protected vw_StudentGrowth studentDetails = new();
        // bool isstudent = false;
        [Inject] GenericServiceFactory _genericService { get; set; } = default!;
        [Inject] RolesAndLicensingService rolesAndLicensingService { get; set; } = default!;
        [Inject] NavigationManager _navigationManager { get; set; } = default!;
        private Position LegendPosition = Position.Bottom;
        public string[] XAxisLabels;
        public string[] CountryNames;
        public string[] CityNames;
        public List<ChartSeries> StudentPerfomanceSeries = new List<ChartSeries>();
        public List<ChartSeries> StudentCountryDemographic = new List<ChartSeries>();
        public double[] StudentCityDemographic;
        protected string StudentID = string.Empty;
        protected int GradeLevel = 0;
        private IEnumerable<IGrouping<TimeSlot, GetStudentClassScheduleResult>> groupedSchedules;
        protected List<DayofTheWeek> daysofTheWeek = new();
        [Inject] AuthenticationStateProvider _authenticationStateProvider { get; set; } = default!;
        protected string SchoolID = string.Empty;
        protected string UserEmailOrUserName = string.Empty;
        private readonly object _lockObject = new object();
        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            Console.WriteLine("Starting OnInitializedAsync");

            bool authState = await GetAuthenticationState();
            Console.WriteLine($"AuthState: {authState}");

            if (authState)
            {
                await InititalizeAppState(UserEmailOrUserName,authState);
                Console.WriteLine("AppState Initialized");

                while (!await InitializeParent())
                {
                    Console.WriteLine("Waiting for AppState to Initialize");
                    await Task.Delay(500);
                }
                Console.WriteLine("AppState Initialization Complete");


                //------------------Non Role Specific Methods-------------------------\\
                GetStudentDetails().Wait(); // Use .Wait() to ensure it completes within the lock
                Console.WriteLine("GetStudentDetails Executed");

                Snackbar.Add($"{studentDetails?.TotalStudents ?? 0}");
                Console.WriteLine("Snackbar displayed with student details");


                // Ensure that StateHasChanged is called immediately after updating the state
                StateHasChanged();
                await Task.Delay(100); // Adding a slight delay to ensure the UI updates
            }

            isLoading = false;
            Console.WriteLine("isLoading set to false");
        }


        #region Intializers
        protected async Task<bool> GetAuthenticationState()
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            if (authState != null)
            {
                var user = authState.User;
                if (user.Identity.IsAuthenticated)
                {
                    UserEmailOrUserName = user.Identity.Name;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        private async Task<bool> InitializeParent()
        {
            if (!AppState.IsInitialized)
            {
                await Task.Delay(1000); // Delay to simulate initialization work
                return false;
            }
            return true;
        }

        protected async Task InititalizeAppState(string username, bool authstate)
        {
            await AppState.InitializeAsync(username,authstate,_navigationManager, http);
            StateHasChanged();
        }
        #endregion

        #region Get Data
        //-----------------------Get Student Totals-------------------\\
        protected async Task GetStudentDetails()
        {
            var service = _genericService.GetService<vw_StudentGrowth>();
            if (service == null)
            {
                Log.Error("Service for vw_StudentGrowth is null");
                return;
            }
            if (AppState.SchoolID == null)
            {
                await Task.Delay(500);
            }
            var result = await service.GetAllAsync($"api/Dynamic/GetStudentGrowthBySchool/{AppState.SchoolID}", true);
            if (result.IsSuccess)
            {
                if (result.Data != null)
                {
                    studentDetails = result.Data.FirstOrDefault();
                    studentDetails.PercentageIncreaseInStudents = (int)studentDetails.PercentageIncreaseInStudents;
                }
                else
                {
                    Log.Warning("GetStudentGrowthBySchool returned null data");
                }
            }
            else
            {
                Log.Error("Failed to fetch student growth data: " + result.ErrorMessage);
            }
        }

        #endregion
    }
}
