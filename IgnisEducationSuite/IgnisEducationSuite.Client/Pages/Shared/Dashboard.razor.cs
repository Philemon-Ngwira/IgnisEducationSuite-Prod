using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.PieChart;
using EDUSphereSharedProject.AchievementModels;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.Models.StoreProModels;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Net.NetworkInformation;
using System.Security.Claims;

namespace IgnisEducationSuite.Client.Pages.Shared
{
    public partial class Dashboard : AppBaseComponent
    {
        protected int activeDayIndex = 0;
        protected bool isLoading = false;
        protected bool isAuthenticated = false;
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
        protected List<GetStudentTimetableResult> Currentschedules = new();
        protected List<GetStudentAttendanceByUserIDAndEventDateResult> attendances = new();
        protected List<GetAttendanceTrendForPastSevenDaysResult> attendancesTrends = new();
        protected List<GetMissedClassesForPastWeekResult> missedClasses = new();
        protected List<GetStudentDemographicsCountryResult> StudentDemographicsCountries = new();
        protected List<GetStudentDemographicsResult> StudentDemographicsCity = new();
        protected List<GetTop5TeachersByHighRatedLessonsResult> _top5Teachers = new();
        protected List<GetBestPerformingStudentsBySchoolResult> bestPerformingStudents = new();

        //--------------------------------DINING---------------------------------------------\\
        protected List<DiningMenuDTO> schoolMenu = new();


        //-----------------------------DINING END---------------------------------------------\\

        [Inject] ISnackbar Snackbar { get; set; }
        private readonly List<string> dayOrder = new List<string>

{
    "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
};
        protected vw_StudentGrowth studentDetails = new();
        // bool isstudent = false;
        [Inject] GenericServiceFactory _genericService { get; set; } = default!;
        [Inject] RolesAndLicensingService licensingService { get; set; } = default!;
        [Inject] NavigationManager _navigationManager { get; set; } = default!;
        [Inject] HttpClient http { get; set; } = default!;

        [Inject] IDialogService DialogService { get; set; } = default!;
        private Position LegendPosition = Position.Bottom;
        public string[] XAxisLabels;
        public string[] CountryNames;
        public string[] CityNames;
        public List<ChartSeries> StudentPerfomanceSeries = new List<ChartSeries>();
        public List<ChartSeries> StudentCountryDemographic = new List<ChartSeries>();
        public double[] StudentCityDemographic;
        protected string StudentID = string.Empty;
        protected int GradeLevel = 0;
        protected List<DayofTheWeek> daysofTheWeek = new();
        [Inject] AuthenticationStateProvider _authenticationStateProvider { get; set; } = default!;
        protected string SchoolID = string.Empty;
        private bool _prerendered = true;

        public void Dispose()
        {
            AppState.OnChange -= StateHasChanged;
        }
        
        protected override async Task OnInitializedAsync()
        {
            LoaderService.Show("Initializing Dashboard please wait....");

            try
            {
                // ----------------------------------------------------------
                // 1. Authenticate User
                // ----------------------------------------------------------
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (!(user.Identity?.IsAuthenticated ?? false))
                {
                    _navigationManager.NavigateTo(
                        $"Account/Login?returnUrl={Uri.EscapeDataString(_navigationManager.Uri)}"
                    );
                    return;
                }

                // ----------------------------------------------------------
                // 2. Extract User ID
                // ----------------------------------------------------------
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _navigationManager.NavigateTo("Account/Login");
                    return;
                }

                // ----------------------------------------------------------
                // 4. Handle First Login (non-admins only, after initialize)
                // ----------------------------------------------------------
                if (AppState.UserRole is not ("SuperAdmin" or "Admin"))
                {
                    var baseUrl = _navigationManager.BaseUri;

                    if (await licensingService.GetLoginAttempt(baseUrl, AppState.UserID))
                    {
                        await licensingService.UpdateLoginAttemptAsync(baseUrl, AppState.UserID);
                        DialogService.Show<OpeningPage>("");
                    }
                }

                SchoolID = AppState.SchoolID;

                // ----------------------------------------------------------
                // 5. Load Core Dashboard Data (Lessons + Student first)
                // ----------------------------------------------------------
                Console.WriteLine("Loading core data...");

                await Task.WhenAll(
                    GetAllLessons(),
                    GetStudentDetails()
                );

                // Then load secondary dashboard data
                await Task.WhenAll(
                    GetTopLessons(),
                    GetStudentDemoGraphicCountry(),
                    GetStudentDemographicCity(),
                    GetTopTeachers(),
                    GetBestPerformingStudents()
                );

                // ----------------------------------------------------------
                // 6. Student / Parent Extra Data
                // ----------------------------------------------------------
                if (AppState.UserRole is "Student" or "Parent")
                {
                    await Task.WhenAll(
                        GetTimeSlots(),
                        GetDaysOfWeek(),
                        GetStudentPerfomanceData(),
                        GetUnCompletedClasses(),
                        GetAttendances(),
                        GetDiningMenus()
                       
                    );

                    // This MUST run AFTER timeSlots + daysOfWeek load
                    await GetClassSchedule();

                    // Now set today’s tab
                    var today = DateTime.Now.DayOfWeek.ToString();
                    activeDayIndex = dayOrder.IndexOf(today);
                    if (activeDayIndex < 0)
                        activeDayIndex = 0;
                }

                // ----------------------------------------------------------
                // 7. Listen for State Change Events
                // ----------------------------------------------------------
                AppState.OnChange -= StateHasChanged; // avoid double subscription
                AppState.OnChange += StateHasChanged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initialization error: {ex}");
            }
            finally
            {
                LoaderService.Hide();
            }
        }


        protected async Task GetBestPerformingStudents()
        {
            var service = _genericService.GetService<GetBestPerformingStudentsBySchoolResult>();
            var result = await service.GetAllAsync($"api/Dynamic/GetBestPerfomingStudentsBySchool/{AppState.SchoolID}", true);
            if (result.IsSuccess)
            {
                bestPerformingStudents = result.Data.ToList();
            }

        }
        protected async Task LoadInfomation()
        {

            await InvokeAsync(StateHasChanged); // Show loading state


            if (!AppState.IsFullyInitialized)
            {
                Console.WriteLine("AppState failed to initialize after retries.");
                isLoading = false;
                await InvokeAsync(StateHasChanged); // Notify UI of failure
                return;
            }

            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                SchoolID = AppState.SchoolID;

                try
                {
                    // Load common data sequentially for reliability
                    await GetAllLessons();
                    await GetStudentDetails();

                    // Load additional data in parallel
                    var otherTasks = new[]
                    {
                    GetTopLessons(),
                    GetStudentDemoGraphicCountry(),
                    GetStudentDemographicCity(),
                    GetTopTeachers()
                };
                    await Task.WhenAll(otherTasks);

                    if (AppState.UserRole == "Student" || AppState.UserRole == "Parent")
                    {
                        var timeSlotavailable = await GetTimeSlots();
                        var DOW = await GetDaysOfWeek();
                        await GetClassSchedule();
                        await GetStudentPerfomanceData();
                        await GetUnCompletedClasses();
                        await GetAttendances();

                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error during initialization: {ex.Message}");
                }
                finally
                {
                    isLoading = false;
                    await InvokeAsync(StateHasChanged); // Always ensure UI refresh
                }
            }
            else
            {
                isAuthenticated = false;
                isLoading = false;
                await InvokeAsync(StateHasChanged); // Notify UI of unauthenticated state
            }
        }
        protected async Task GetTopTeachers()
        {
            var service = _genericService.GetService<GetTop5TeachersByHighRatedLessonsResult>();
            var result = await service.GetAllAsync($"api/Dynamic/GetTopTeachers/{AppState.SchoolID}", true);
            if (result.IsSuccess)
            {
                _top5Teachers = result.Data.ToList();
            }
        }
        protected async Task GetDiningMenus()
        {
            var service = GenericService.GetService<DiningMenuDTO>();
            var result = await service.GetAllAsync($"api/Dynamic/GetSchoolDiningMenus/{Guid.Parse(AppState.SchoolID)}", true);

            if (result.IsSuccess)
            {
                // Group by MealName to avoid duplicates across dining halls
                var today = DateTime.Now.DayOfWeek.ToString();
                schoolMenu = result.Data
                      .GroupBy(x => new
                      {
                          x.DayOfWeek,
                          Meal = x.MealName.Trim().ToLower(),
                          Main = x.MainDish.Trim().ToLower()
                      })
                .Select(g => g.First()) // keeps one per Day+Meal+MainDish combo
                .OrderBy(x => dayOrder.IndexOf(x.DayOfWeek))
                .ThenBy(x => MealOrder.IndexOf(x.MealName))
                .ToList();
            }
        }
        protected async Task GetStudentDetails()
        {
            var service = _genericService.GetService<vw_StudentGrowth>();
            var result = await service.GetAllAsync($"api/Dynamic/GetStudentGrowthBySchool/{AppState.SchoolID}", true);
            if (result.IsSuccess)
            {
                studentDetails = result.Data.FirstOrDefault();
                if (studentDetails == null)
                {
                    studentDetails = new();
                }

                studentDetails.PercentageIncreaseInStudents = (int)studentDetails.PercentageIncreaseInStudents;
                StateHasChanged();
            }
        }
        protected async Task GetAllLessons()
        {
            try
            {
                var service = _genericService.GetService<GetLessonCountBySchoolResult>();
                var result = await service.GetAllAsync($"api/Dynamic/GetLessonCountBySchool/{AppState.SchoolID}", true);
                if (result.IsSuccess)
                {
                    if (result.Data.Any())
                    {
                        lesson = result.Data.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                throw;
            }

        }

        protected async Task GetTopLessons()
        {
            var service = _genericService.GetService<vw_ClassLessonSummary>();
            var result = await service.GetAllAsync($"api/Dynamic/GetLessonSummaryBySchool/{AppState.SchoolID}", true);
            if (result.IsSuccess)
            {
                HighRatedLessons = result.Data.ToList();

            }
        }
        protected async Task GetStudentDemoGraphicCountry()
        {
            try
            {
                var service = _genericService.GetService<GetStudentDemographicsCountryResult>();
                var result = await service.GetAllAsync($"api/Dynamic/GetStudentDemoCountry/{AppState.SchoolID}", true);
                if (result.IsSuccess)
                {
                    StudentDemographicsCountries = result.Data.ToList();
                    OrganizeCountryDemographicData();
                    StateHasChanged();
                }
                else
                {
                    Console.Error.WriteLine("Failed to fetch student demographics by country");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in GetStudentDemoGraphicCountry: {ex.Message}");
            }
        }
        protected async Task GetStudentDemographicCity()
        {
            try
            {
                var service = _genericService.GetService<GetStudentDemographicsResult>();
                var result = await service.GetAllAsync($"api/Dynamic/GetStudentDemoCity/{AppState.SchoolID}", true);
                if (result.IsSuccess)
                {
                    StudentDemographicsCity = result.Data.ToList();
                    OrganizeCityDemographicData(); StateHasChanged();
                }
                else
                {
                    Console.Error.WriteLine("Failed to fetch student demographics by city.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in GetStudentDemographicCity: {ex.Message}");
            }
        }
        #region Student



        protected async Task GetAttendanceTrends()
        {
            var service = _genericService.GetService<GetAttendanceTrendForPastSevenDaysResult>();
            var result = await service.GetAllAsync($"api/Dynamic/GetAttendanceTrends/{AppState.SchoolID}", true);
            if (result.IsSuccess)
            {
                attendancesTrends = result.Data.ToList();
            }
        }
        protected async Task GetMissedClassesPastSevenDaya()
        {
            var service = _genericService.GetService<GetMissedClassesForPastWeekResult>();
            var result = await service.GetAllAsync($"api/Dynamic/GetMissedClasses/{AppState.SchoolID}", true);
            if (result.IsSuccess)
            {
                missedClasses = result.Data.ToList();
            }
        }

      
        private async Task<List<UserActivity>> GetUserActivitiesDone(string UserRole, string UserID)
        {
            if (UserRole == "Student")
            {
                var service = _genericService.GetService<UserActivity>();
                var result = await service.GetAllAsync($"api/Dynamic/GetAllUserActivities/{UserID}", true);
                if (result.IsSuccess)
                {
                    return result.Data.ToList();
                }
                else
                {
                    return new List<UserActivity>();
                }
            }
            else
            {
                return new List<UserActivity>();
            }
        }
        protected async Task<List<TimeSlot>> GetTimeSlots()
        {
            try
            {
                var result = _genericService.GetService<TimeSlot>();
                var service = await result.GetAllAsync($"api/Dynamic/GetEntity/{"timeslot"}", true);
                if (service.IsSuccess)
                {
                    if (service.Data != null)
                    {
                        timeSlots = service.Data.Where(x => x.SchoolID == Guid.Parse(AppState.SchoolID)).ToList();
                        return timeSlots;
                    }
                    else
                    {
                        timeSlots = new();
                        return timeSlots;
                    }
                }
                return new List<TimeSlot>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in GettingTimeSlots: {ex.Message}");
                throw;
            }

        }
        protected async Task<List<DayofTheWeek>> GetDaysOfWeek()
        {
            try
            {
                var result = _genericService.GetService<DayofTheWeek>();
                var service = await result.GetAllAsync($"api/Dynamic/GetEntity/{"days"}", true);
                if (service.IsSuccess)
                {
                    if (service.Data != null)
                    {
                        daysofTheWeek = service.Data.ToList();
                        return daysofTheWeek;
                    }

                }
                return new List<DayofTheWeek>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in GettingDaysOfTheWeek: {ex.Message}");
                throw;
            }

        }

        private void OrganizeCountryDemographicData()
        {
            try
            {
                if (StudentDemographicsCountries.Any())
                {
                    CountryNames = StudentDemographicsCountries.Select(x => x.Country).ToArray();
                    //Prepare the datasets
                    var currentMonthDataset = new BarDataset<double>
                    {
                        Label = "Current Total",
                        BackgroundColor = "rgba(41, 16, 74, .7)", // Light Blue
                    };
                    var data = StudentDemographicsCountries.Select(x => x.StudentCount ?? 0.0m).Select(v => (double)v).ToArray();
                    // Add data to datasets
                    foreach (var record in data)
                    {
                        currentMonthDataset.Add(record);
                    }
                    //Configure chart
                    StudentCountryChartConfig = new BarConfig
                    {
                        Options = new BarOptions
                        {
                            Responsive = true,
                            Title = new OptionsTitle
                            {
                                Display = true,
                                Text = "Student Demographic By Country"
                            },
                            Scales = new BarScales
                            {
                                XAxes = new List<CartesianAxis>
                               {
                                   new CategoryAxis
                                   {
                                       ScaleLabel = new ScaleLabel
                                       {
                                           LabelString = "Countries"
                                       }
                                   }
                               },
                                YAxes = new List<CartesianAxis>
                               {
                                   new LinearCartesianAxis
                                   {
                                       ScaleLabel = new ScaleLabel
                                       {
                                           LabelString = "Total Number"
                                       }
                                   }
                               }
                            }
                        },
                    };
                    StudentCountryChartConfig.Data.Datasets.Add(currentMonthDataset);
                    foreach (var item in CountryNames)
                    {
                        StudentCountryChartConfig.Data.Labels.Add(item);
                    }
                }
            }
            catch (Exception ex) { Console.Error.WriteLine($"Exception in OrganizeCountryDemographicData: {ex.Message}"); }
        }

        private void OrganizeCityDemographicData()
        {
            try
            {
                if (StudentDemographicsCity.Any())
                {
                    CityNames = StudentDemographicsCity.Select(x => x.City).ToArray();
                    StudentCityDemographic = GetCityDemographicData();
                    var colors = GetColors(StudentDemographicsCity.Count);
                    var currentMonthDataset = new PieDataset<double>
                    {
                        BackgroundColor = colors.ToArray(),
                    };
                    // Add data to datasets
                    foreach (var record in StudentDemographicsCity)
                    {
                        currentMonthDataset.Add((double)record.StudentCount);
                    }
                    // Configure chart
                    StudentCityChartConfig = new PieConfig
                    {
                        Options = new PieOptions
                        {
                            Responsive = true,
                            Title = new OptionsTitle
                            {
                                Display = true,
                                Text = "Student Count by City"
                            },
                        },
                    };
                    StudentCityChartConfig.Data.Datasets.Add(currentMonthDataset);
                    foreach (var item in CityNames)
                    {
                        StudentCityChartConfig.Data.Labels.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in OrganizeCityDemographicData: {ex.Message}");
            }
        }
        protected async Task GetStudentPerfomanceData()
        {
            if (StudentID == string.Empty)
            {
                StudentID = AppState.UserID;
                if (!string.IsNullOrEmpty(StudentID))
                {
                    var service = _genericService.GetService<GetStudentPerformanceForCurrentYearResult>();
                    try
                    {
                        if (AppState.UserRole == "Parent")
                        {
                            var result = await service.GetAllAsync($"api/Dynamic/GetStudentPerfomanceDataParent/{AppState.UserID}", true);
                            if (result.IsSuccess)
                            {
                                YearlyStudentPerfomance = result.Data.ToList();
                                if (YearlyStudentPerfomance.Any())
                                {
                                    OrganizeStudentPerformanceDataParent();
                                }
                                else
                                {
                                    SetDefaultChart();
                                }
                            }
                            await GetMissedClassesPastSevenDaya();
                            await GetAttendanceTrends();
                        }
                        else
                        {
                            var result = await service.GetAllAsync($"api/Dynamic/GetStudentPerfomanceData/{AppState.UserID}", true);
                            if (result.IsSuccess)
                            {
                                YearlyStudentPerfomance = result.Data.ToList();
                                if (YearlyStudentPerfomance.Any())
                                {
                                    OrganizeStudentPerformanceData();
                                }
                                else
                                {
                                    SetDefaultChart();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Exception in GetStudentPerfomanceData: {ex.Message}");
                    }
                }
                else
                {
                    SetDefaultChart();
                }
            }
            else
            {
                SetDefaultChart();
            }
        }
        private void OrganizeStudentPerformanceDataParent()
        {
            try
            {
                // Prepare the datasets
                var currentMonthDataset = new BarDataset<double>
                {
                    Label = "Current Monthly Average '%'",
                    BackgroundColor = "rgba(54, 162, 235, 0.6)" // Light Blue
                };

                foreach (var record in YearlyStudentPerfomance)
                {
                    currentMonthDataset.Add((double)record.AverageMonthlyScore);
                }

                // Configure chart
                StudentPerformanceChartConfig = new BarConfig
                {
                    Options = new BarOptions
                    {
                        Responsive = true,
                        Title = new OptionsTitle
                        {
                            Display = true,
                            Text = "Student Performance by Month"
                        },
                        Scales = new BarScales
                        {
                            XAxes = new List<CartesianAxis>
                    {
                        new CategoryAxis
                        {
                            ScaleLabel = new ScaleLabel
                            {
                                LabelString = "Months"
                            }
                        }
                    },
                            YAxes = new List<CartesianAxis>
                    {
                        new LinearCartesianAxis
                        {
                            ScaleLabel = new ScaleLabel
                            {
                                LabelString = "Performance"
                            }
                        }
                    }
                        }
                    }
                };

                List<string> months = new();
                foreach (var item in YearlyStudentPerfomance)
                {
                    string monthName = $"{item.MonthName}:{item.FirstName}";
                    months.Add(monthName);
                }

                XAxisLabels = months.ToArray();
                StudentPerformanceChartConfig.Data.Datasets.Add(currentMonthDataset);

                foreach (var item in XAxisLabels)
                {
                    StudentPerformanceChartConfig.Data.Labels.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in OrganizeStudentPerformanceDataParent: {ex.Message}");
            }
        }

        private void OrganizeStudentPerformanceData()
        {
            try
            {
                // Prepare the datasets
                var currentMonthDataset = new BarDataset<double>
                {
                    Label = "Current Month Average",
                    BackgroundColor = "rgba(54, 162, 235, 0.6)" // Light Blue
                };

                foreach (var record in YearlyStudentPerfomance)
                {
                    currentMonthDataset.Add((double)record.AverageMonthlyScore);
                }

                // Configure chart
                StudentPerformanceChartConfig = new BarConfig
                {
                    Options = new BarOptions
                    {
                        Responsive = true,
                        Title = new OptionsTitle
                        {
                            Display = true,
                            Text = "Student Performance by Month"
                        },
                        Scales = new BarScales
                        {
                            XAxes = new List<CartesianAxis>
                    {
                        new CategoryAxis
                        {
                            ScaleLabel = new ScaleLabel
                            {
                                LabelString = "Months"
                            }
                        }
                    },
                            YAxes = new List<CartesianAxis>
                    {
                        new LinearCartesianAxis
                        {
                            ScaleLabel = new ScaleLabel
                            {
                                LabelString = "Performance"
                            }
                        }
                    }
                        }
                    }
                };

                XAxisLabels = YearlyStudentPerfomance.Select(x => x.MonthName).ToArray();
                StudentPerformanceChartConfig.Data.Datasets.Add(currentMonthDataset);

                foreach (var item in XAxisLabels)
                {
                    StudentPerformanceChartConfig.Data.Labels.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in OrganizeStudentPerformanceData: {ex.Message}");
            }
        }


        private void SetDefaultChart()
        {
            var defaultMonths = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

            var currentMonthDataset = new BarDataset<double>
            {
                Label = "Current Month Average",
                BackgroundColor = "rgba(54, 162, 235, 0.6)", // Light Blue
            };

            var previousMonthDataset = new BarDataset<double>
            {
                Label = "Previous Month Average",
                BackgroundColor = "rgba(255, 99, 132, 0.6)", // Light Red
            };

            // Populate datasets with zeros
            for (int i = 0; i < defaultMonths.Length; i++)
            {
                currentMonthDataset.Data.Append(0);
                previousMonthDataset.Data.Append(0);
            }

            StudentPerformanceChartConfig = new BarConfig
            {
                Options = new BarOptions
                {
                    Responsive = true,
                    Title = new OptionsTitle { Display = true, Text = "Student Performance (No Data Available)" }
                },

            };
        }
        protected async Task GetUnCompletedClasses()
        {
            var service = _genericService.GetService<GetStudentUnCompletedLessonsResult>();
            var result = await service.GetAllAsync($"api/Dynamic/GetStudentPendingClasses/{AppState.UserID}", true);
            if (result.IsSuccess)
            {
                uncompletedLessons = result.Data.ToList();
            }
        }
        protected async Task GetAttendances()
        {
            if (StudentID != string.Empty)
            {
                var service = _genericService.GetService<GetStudentAttendanceByUserIDAndEventDateResult>();
                var result = await service.GetAllAsync($"api/Dynamic/GetStudentAttendances/{AppState.UserID}", true);
                if (result.IsSuccess)
                {
                    attendances = result.Data.OrderBy(x => x.AttendanceDate).ToList();
                }
            }
        }
        protected async Task GetClassSchedule()
        {
            var service = _genericService.GetService<GetStudentTimetableResult>();
            var result = await service.GetAllAsync($"api/Dynamic/GetActiveStudentTimeTable/{AppState.UserID}", true);
            if (result.IsSuccess)
            {
                Currentschedules = result.Data.ToList();
            }
        }
        #endregion
        #region Chart Visualization
        public List<ChartSeries> GetStudentPerfomanceChartSeries()
        {
            // Extract data for the current and previous month
            // Convert CurrentMonthAverage (decimal?) to double
            var currentMonthData = YearlyStudentPerfomance.Select(item => item.AverageMonthlyScore ?? 0.0m) // Replace nulls with 0
                                       .Select(value => (double)value) // Convert decimal to double
                                       .ToArray();



            // Create ChartSeries list
            return new List<ChartSeries>
    {
        new ChartSeries { Name = "CurrentMonthPerfomance", Data = currentMonthData }
    };
        }

        public List<ChartSeries> GetCountryDemographicData()
        {
            var data = StudentDemographicsCountries.Select(x => x.StudentCount ?? 0.0m)
                                                   .Select(v => (double)v).ToArray();
            return new List<ChartSeries>
    {
        new ChartSeries { Name = "Student's By Country", Data = data }
    };
        }
        public double[] GetCityDemographicData()
        {
            var data = StudentDemographicsCity.Select(x => x.StudentCount ?? 0.0m)
                                                   .Select(v => (double)v).ToArray();
            return data;
        }
        #endregion


        #region Chart Theming
        private string[] colorPalette = new string[]
        {
    "rgba(41, 16, 74, 0.7)", // Dark Purple
    "rgba(75, 192, 192, 0.7)", // Light Blue
    "rgba(153, 102, 255, 0.7)", // Purple
    "rgba(255, 159, 64, 0.7)", // Orange
    "rgba(54, 162, 235, 0.7)", // Blue
    "rgba(255, 99, 132, 0.7)" // Red
        };

        private List<string> GetColors(int count)
        {
            List<string> colors = new List<string>();
            for (int i = 0; i < count; i++)
            {
                colors.Add(colorPalette[i % colorPalette.Length]);
            }
            return colors;
        }


        #endregion
    }
}