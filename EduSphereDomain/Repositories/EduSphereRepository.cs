using EduSphereDomain.AchievementData;
using EduSphereDomain.Data;
using EDUSphereSharedProject.AchievementModels;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.Models.StoreProModels;
using EDUSphereSharedProject.UniversalModels;
using Microsoft.EntityFrameworkCore;

namespace EduSphereDomain.Repositories
{
    public class EduSphereRepository
    {
        private readonly PhoenixEdusphereContext _context;
        private readonly PhoenixEdusphereContextProcedures _contextProcedures;
        private readonly AchievementContext _achivementContext;
        private readonly AchievementContextProcedures _achievementContextProcedures;
        public EduSphereRepository(PhoenixEdusphereContext context, PhoenixEdusphereContextProcedures phoenixEdusphereContextProcedures, AchievementContext achivementContext, AchievementContextProcedures achievementContextProcedures)
        {
            _context = context;
            _contextProcedures = phoenixEdusphereContextProcedures;
            _achivementContext = achivementContext;
            _achievementContextProcedures = achievementContextProcedures;
        }

        public async Task<IEnumerable<Class>> GetTeacherSubjectsByID(Guid id)
        {
            var result = await _context.Classes.Where(x => x.TeacherID == id).ToListAsync();
            return result;

        }
        public async Task<IEnumerable<Badge>> GetSystemBadges()
        {
            var result = await _achivementContext.Badges.ToListAsync();
            return result;
        }
        public async Task<IEnumerable<Activity>> GetSystemActivities()
        {
            var result = await _achivementContext.Activities.ToListAsync();
            return result;
        }
        public async Task <IEnumerable<Course>> GetTeacherCourses(Guid TeacherID)
        {
            var courses =  await _context.Courses.Where(x=>x.TeacherID==TeacherID).ToListAsync();
            return courses;
        }
        public async Task<bool> GetStudentDashboardState(string SchoolId)
        {
            try
            {
                var school = _context.ClientsWithoutStudentDashboards.Where(x => x.ClientID == Guid.Parse(SchoolId)).FirstOrDefault();
                if (school != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                var message = ex.Message;   
                throw;
            }
            

        }
        public async Task<IEnumerable<GetBestPerformingStudentsBySchoolResult>> GetBestPerformingStudents(string SchoolID)
        {
            List<GetBestPerformingStudentsBySchoolResult> getBestPerformingStudents = new();
            var result = await _contextProcedures.GetBestPerformingStudentsBySchoolAsync(SchoolID);
            if (result != null)
            {
                foreach (var item in result)
                {
                    GetBestPerformingStudentsBySchoolResult perfomer = new()
                    {
                        StudentID = item.StudentID,
                        FirstName = item.FirstName,
                        LastName = item.LastName,
                        SchoolID = item.SchoolID,
                        ProfilePic = item.ProfilePic,
                        AvgAssignmentGrade = item.AvgAssignmentGrade,
                        AvgExamGrade = item.AvgExamGrade,
                        AttendanceScore = item.AttendanceScore,
                        MarksScore = item.MarksScore,
                        LessonScore = item.LessonScore,
                        BadgeScore = item.BadgeScore,
                        FinalScore = item.FinalScore,
                    };
                    getBestPerformingStudents.Add(perfomer);
                }
                return getBestPerformingStudents;
            }
            else
            {
                return getBestPerformingStudents;
            }
        }
        public async Task<IEnumerable<UserActivity>> GetUserActivities(string UserID)
        {
            var result = await _achivementContext.UserActivities.Where(x => x.UserId == UserID).ToListAsync();
            return result;
        }
        public async Task<UserBadge> SaveUserBadge(UserBadge userBadge)
        {
            // Add the userBadge to the context
            var result = await _achivementContext.UserBadges.AddAsync(userBadge);

            // Save changes to the database
            await _achivementContext.SaveChangesAsync();

            // Return the added userBadge
            return result.Entity;
        }

        public async Task<IEnumerable<GetUserBadgesByUserIDResult>> GetUserBadges(string UserID)
        {
            List<GetUserBadgesByUserIDResult> userBadges = new();
            var result = await _achievementContextProcedures.GetUserBadgesByUserIDAsync(UserID);
            foreach (var item in result)
            {
                GetUserBadgesByUserIDResult badge = new()
                {
                    UserBadgeID = item.UserBadgeID,
                    UserID = item.UserID,
                    BadgeName = item.BadgeName,
                    BadgeDescription = item.BadgeDescription,
                    BadgeLevel = item.BadgeLevel,
                    image_url = item.image_url,
                    DateEarned = item.DateEarned,

                };
                userBadges.Add(badge);
            }
            return userBadges;
        }
        public async Task<UserActivity> SaveUserActivity(UserActivity Activity)
        {
            // Add the userBadge to the context
            var result = await _achivementContext.UserActivities.AddAsync(Activity);
            try
            {

                // Save changes to the database
                await _achivementContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                throw;
            }

            // Return the added userBadge
            return result.Entity;
        }
        public async Task<IEnumerable<GetLessonCountBySchoolResult>> GetLessonCountBySchool(string SchoolID)
        {
            var result = await _contextProcedures.GetLessonCountBySchoolAsync(SchoolID);
            var data = new List<GetLessonCountBySchoolResult>();
            foreach (var item in result)
            {
                GetLessonCountBySchoolResult newRes = new()
                {
                    TotalLessons = item.TotalLessons,
                    LessonsAddedThisMonth = item.LessonsAddedThisMonth,
                };
                data.Add(newRes);
            }
            return data;
        }
        public async Task<IEnumerable<Lesson>> GetstudentLessons(string guid)
        {
            List<Lesson> studentLessons = new();
            var result = await _contextProcedures.GetStudentLessonsAsync(guid);
            foreach (var lesson in result)
            {
                Lesson newLessson = new Lesson()
                {
                    LessonID = lesson.LessonID,
                    TeacherID = lesson.TeacherID,
                    Title = lesson.Title,
                    Content = lesson.Content,
                    ClassID = lesson.ClassID,
                    DatePosted = lesson.DatePosted,
                    Rating = lesson.Rating,
                    StudentID = lesson.StudentID,
                    LessonCompleted = lesson.LessonCompleted,
                    SchoolID = lesson.StudentID,

                };
                studentLessons.Add(newLessson);

            }
            return studentLessons;
        }
        public async Task<IEnumerable<Lesson>> GetstudentCompletedLessons(string guid)
        {
            List<Lesson> studentLessons = new();
            var result = await _contextProcedures.GetStudentCompletedLessonsAsync(guid);
            foreach (var lesson in result)
            {
                Lesson newLessson = new Lesson()
                {
                    LessonID = lesson.LessonID,
                    TeacherID = lesson.TeacherID,
                    Title = lesson.Title,
                    Content = lesson.Content,
                    ClassID = lesson.ClassID,
                    DatePosted = lesson.DatePosted,
                    Rating = lesson.Rating,
                    StudentID = lesson.StudentID,
                    LessonCompleted = lesson.LessonCompleted,
                    SchoolID = lesson.StudentID,

                };
                studentLessons.Add(newLessson);

            }
            return studentLessons;
        }
        public async Task<IEnumerable<GetStudentAssignmentsResult>> GetStudentAssignments(string id)
        {
            List<GetStudentAssignmentsResult> studentAssignments = new();
            var result = await _contextProcedures.GetStudentAssignmentsAsync(id);
            foreach (var item in result)
            {
                GetStudentAssignmentsResult assignment = new()
                {
                    AssignmentID = item.AssignmentID,
                    Title = item.Title,
                    Description = item.Description,
                    DueDate = item.DueDate,
                    ClassID = item.ClassID,
                    TeacherID = item.TeacherID,
                    TotalMarks = item.TotalMarks,
                    ClassName = item.ClassName,
                    StudentID = item.StudentID,
                    Overdue = item.Overdue,
                };
                studentAssignments.Add(assignment);
            }

            return studentAssignments;
        }

        public async Task<IEnumerable<GetTeacherAssignmentsResult>> GetTeacherAssignments(string id)
        {
            List<GetTeacherAssignmentsResult> studentAssignments = new();
            var result = await _contextProcedures.GetTeacherAssignmentsAsync(id);
            foreach (var item in result)
            {
                GetTeacherAssignmentsResult assignment = new()
                {
                    AssignmentID = item.AssignmentID,
                    StudentAssignmentID = item.StudentAssignmentID,
                    StudentID = item.StudentID,
                    SubmissionDate = item.SubmissionDate,
                    Grade = item.Grade,
                    Status = item.Status,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    TotalMarks = item.TotalMarks,
                    ClassName = item.ClassName,
                    Overdue = item.Overdue,
                    GradeLevel = item.GradeLevel,
                };
                studentAssignments.Add(assignment);
            }

            return studentAssignments;
        }
        public async Task<bool> CheckIfStudentNumberIsTaken(string id, string SchoolID)
        {
            var result = await _contextProcedures.CheckStudentNumberExistsAsync(id);
            var ret = result.FirstOrDefault();
            if (ret == null)
            {
                return false;
            }
            else if (ret.StudentNumberExists == false)
            {
                return false;
            }
            else if (ret.StudentNumberExists == true)
            {
                return true;
            }
            return false;
        }
        public async Task<IEnumerable<AssignmentQuestion>> GetAssignmentQuestions(Guid id)
        {
            var result = await _context.AssignmentQuestions.Where(x => x.AssignmentID == id).ToListAsync();
            return result;
        }
        public async Task<IEnumerable<GetStudentClassInfoResult>> GetStudentClassInfo(Guid id)
        {
            List<GetStudentClassInfoResult> stdCls = new();
            var result = await _contextProcedures.GetStudentClassInfoAsync(id);
            foreach (var item in result)
            {
                GetStudentClassInfoResult studentClass = new()
                {
                    StudentClassID = item.StudentClassID,
                    ClassID = item.ClassID,
                    StudentID = item.StudentID,
                    ClassName = item.ClassName,
                    Teacher = item.Teacher
                };
                stdCls.Add(studentClass);
            }
            return stdCls;
        }
        public async Task<IEnumerable<GetStudentsInGradeResult>> GetStudentsInGrade(int grade, string SchoolID)
        {
            List<GetStudentsInGradeResult> stds = new();
            var result = await _contextProcedures.GetStudentsInGradeAsync(grade, SchoolID);
            foreach (var item in result)
            {
                GetStudentsInGradeResult studentInGrade = new()
                {
                    StudentID = item.StudentID
                };
                stds.Add(studentInGrade);
            }
            return stds;
        }
        public async Task<IEnumerable<Course>> GetCoursesBySchool(Guid SchoolID)
        {
            var result = await _context.Courses.Where(x => x.SchoolID == SchoolID).ToListAsync();
            return result;
        }

        public async Task<IEnumerable<CourseDetail>> GetCourseDetailsByID(Guid CourseID)
        {
            var result = await _context.CourseDetails.Where(x => x.CourseID == CourseID)

                .ToListAsync();
            return result;
        }

        public async Task<IEnumerable<Student>> GetStudentsByGrade(int grade)
        {
            List<Student> stds = new();
            var result = await _contextProcedures.GetStudentsInGradeDetailsAsync(grade);
            foreach (var item in result)
            {
                Student student = new()
                {
                    StudentID = item.StudentID,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    GradeLevel = item.GradeLevel,
                    ParentID = item.ParentID,
                    Gender = item.Gender,
                    Address = item.Address,
                    StudentNumber = item.StudentNumber,
                    ProfilePic = item.ProfilePic,
                    UserID = item.UserID,
                    DateOnBoarded = item.DateOnBoarded,
                    Country = item.Country,
                    City = item.City
                };
                stds.Add(student);
            }
            return stds;
        }
        public async Task<IEnumerable<GetStudentsInGradeWithoutScheduleResult>> GetStudentsInGradeWithoutSchedule(int grade, string schoolID, string ClassSection)
        {
            List<GetStudentsInGradeWithoutScheduleResult> stds = new();
            var result = await _contextProcedures.GetStudentsInGradeWithoutScheduleAsync(grade, schoolID, ClassSection);
            foreach (var item in result)
            {
                GetStudentsInGradeWithoutScheduleResult studentInGrade = new()
                {
                    StudentID = item.StudentID
                };
                stds.Add(studentInGrade);
            }
            return stds;
        }
        public async Task<IEnumerable<StudentAssignmentAnswer>> GetAssignmentAnswers(Guid id)
        {
            var result = await _context.StudentAssignmentAnswers.Where(x => x.StudentAssignmentID == id).ToListAsync();
            return result;
        }
        public async Task<IEnumerable<GetStudentDemographicsCountryResult>> GetStudentDemographicsCountries(string SchoolID)
        {
            List<GetStudentDemographicsCountryResult> stds = new();
            var result = await _contextProcedures.GetStudentDemographicsCountryAsync(SchoolID);
            foreach (var item in result)
            {
                GetStudentDemographicsCountryResult studentDemographicsCountry = new()
                {
                    Country = item.Country,
                    StudentCount = item.StudentCount
                };
                stds.Add(studentDemographicsCountry);
            }
            return stds;
        }
        public async Task<IEnumerable<GetStudentDemographicsResult>> GetStudentDemographicsCities(string SchoolID)
        {
            List<GetStudentDemographicsResult> stds = new();
            var result = await _contextProcedures.GetStudentDemographicsAsync(SchoolID);
            foreach (var item in result)
            {
                GetStudentDemographicsResult studentDemographicsCountry = new()
                {
                    City = item.City,
                    StudentCount = item.StudentCount
                };
                stds.Add(studentDemographicsCountry);
            }
            return stds;
        }
        public async Task<IEnumerable<GetStudentAttendanceByUserIDAndEventDateResult>> StudentAttendances(string UserID)
        {
            List<GetStudentAttendanceByUserIDAndEventDateResult> stds = new();
            var result = await _contextProcedures.GetStudentAttendanceByUserIDAndEventDateAsync(UserID);
            foreach (var item in result)
            {
                GetStudentAttendanceByUserIDAndEventDateResult studentAttendance = new()
                {
                    StudentAttendanceID = item.StudentAttendanceID,
                    AttendanceDate = item.AttendanceDate,
                    StudentScheduleID = item.StudentScheduleID,
                    LateStatus = item.LateStatus,
                    AttendanceStatus = item.AttendanceStatus,
                    CreatedDate = item.CreatedDate,
                    Marked = item.Marked,
                    ClassName = item.ClassName,
                };
                stds.Add(studentAttendance);
            }
            return stds;
        }
        public async Task<IEnumerable<GetAttendanceSummaryResult>> GetAttendanceSummaryResults(Guid StudentID, DateTime Start, DateTime End)
        {
            List<GetAttendanceSummaryResult> getAttendanceSummaryResults = new();
            var result = await _contextProcedures.GetAttendanceSummaryAsync(StudentID, Start, End);
            foreach (var item in result)
            {
                GetAttendanceSummaryResult attendanceSummary = new()
                {


                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    Term = item.Term,
                    GPA = (double?)item.GPA,
                    IssueDate = item.IssuedDate,
                    TermStartDate = item.TermStartDate,
                    TermEndDate = item.TermEndDate,
                    GradeLevel = item.GradeLevel,
                    ExpectedAttendances = item.ExpectedAttendances,
                    AttendanceCount = item.AttendanceCount
                };
                getAttendanceSummaryResults.Add(attendanceSummary);
            }
            return getAttendanceSummaryResults;

        }
        public async Task<IEnumerable<ExamQuizTestQuestion>> ExamQuizTestQuestions(Guid guid)
        {
            var result = await _context.ExamQuizTestQuestions.Where(x => x.ExamQuizID == guid).ToListAsync();
            return result;

        }
        public async Task<IEnumerable<ClientAdmin>> GetClientAdmins(string UserID)
        {
            var result = await _context.ClientAdmins.Where(x => x.UserID == UserID).ToListAsync();
            return result;
        }
        public async Task<IEnumerable<Student>> GetStudentsBySchool(Guid SchoolID)
        {
            var result = await _context.Students.Where(x => x.SchoolID == SchoolID).ToListAsync();
            return result;
        }
        public async Task<IEnumerable<Teacher>> GetTeachersBySchool(Guid SchoolID)
        {
            var result = await _context.Teachers.Where(x => x.SchoolID == SchoolID).ToListAsync();
            return result;
        }
        public async Task<IEnumerable<Class>> GetClassesBySchool(Guid SchoolID)
        {
            var result = await _context.Classes.Where(x => x.SChoolID == SchoolID).ToListAsync();
            return result;
        }
        public async Task<IEnumerable<Teacher>> GetTeachersByUser(string UserID)
        {
            var result = await _context.Teachers.Where(x => x.UserID == UserID).ToListAsync();
            return result;
        }

        public async Task<IEnumerable<Lesson>> GetLessonsBySchool(string SchoolID)
        {
            var returnlist = new List<Lesson>();
            var result = await _contextProcedures.GetLessonsBySchoolAsync(SchoolID);
            foreach (var item in result)
            {
                Lesson lesson = new Lesson()
                {
                    ClassID = item.ClassID,
                    TeacherID = item.TeacherID,
                    LessonID = item.LessonID,
                    Title = item.Title,
                    Content = item.Content,
                    DatePosted = item.DatePosted,
                    Rating = item.Rating,
                    NumberOfRatings = item.NumberOfRatings,
                    SchoolID = item.SchoolID.Value

                };
                returnlist.Add(lesson);
            }
            return returnlist;
        }
        public async Task<IEnumerable<Lesson>> GetTeacherLessonsAsync(Guid TeacherID)
        {
            var result = await _context.Lessons.Where(x => x.TeacherID == TeacherID).ToListAsync();
            return result;
        }
        public async Task<IEnumerable<GetTop5TeachersByHighRatedLessonsResult>> GetTop5TeachersByHighRatings(string SchoolID)
        {
            var returnableList = new List<GetTop5TeachersByHighRatedLessonsResult>();
            var result = await _contextProcedures.GetTop5TeachersByHighRatedLessonsAsync(SchoolID);
            foreach (var item in result)
            {
                GetTop5TeachersByHighRatedLessonsResult newItem = new()
                {
                    TeacherID = item.TeacherID,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    ProfilePic = item.ProfilePic,
                    Gender = item.Gender,
                    AverageRating = item.AverageRating,
                    TotalHighRatedLessons = item.TotalHighRatedLessons,
                };
                returnableList.Add(newItem);
            }
            return returnableList;
        }
        public async Task<IEnumerable<vw_ClassLessonSummary>> GetHighestRatedClasses(string SchoolID)
        {
            var returnableList = new List<vw_ClassLessonSummary>();
            var result = await _contextProcedures.GetClassLessonSummaryBySchoolAsync(SchoolID);
            foreach (var item in result)
            {
                vw_ClassLessonSummary vw_ClassLesson = new()
                {
                    ClassID = item.ClassID,
                    ClassName = item.ClassName,
                    TeacherName = item.TeacherName,
                    ProfilePic = item.ProfilePic,
                    Gender = item.Gender,
                    SchoolID = item.SchoolID.Value,
                    TotalLessons = item.TotalLessons,
                    AverageRating = item.AverageRating,
                };
                returnableList.Add(vw_ClassLesson);
            }
            return returnableList;
        }
        public async Task<IEnumerable<vw_StudentGrowth>> GetStudentGrowth(string SchoolID)
        {
            List<vw_StudentGrowth> vw_StudentGrowths = new List<vw_StudentGrowth>();
            var result = await _contextProcedures.GetStudentGrowthBySchoolAsync(SchoolID);
            foreach (var item in result)
            {
                vw_StudentGrowth vw_StudentGrowth = new vw_StudentGrowth()
                {
                    SchoolID = item.SchoolID,
                    TotalStudents = item.TotalStudents,
                    NewStudentsCurrentMonth = item.NewStudentsCurrentMonth,
                    PercentageIncreaseInStudents = item.PercentageIncreaseInStudents,
                };
                vw_StudentGrowths.Add(vw_StudentGrowth);
            }
            return vw_StudentGrowths;
        }
        public async Task<IEnumerable<StudentExamQuizAndTestAnswer>> ExamQuizTestAnswers(Guid guid)
        {

            var result = await _context.StudentExamQuizAndTestAnswers.Where(x => x.HeaderID == guid)
                .ToListAsync();
            return result;

        }
        public async Task<IEnumerable<GetStudentExamDetailsByTeacherResult>> GetStudentExamDetails(string TeacherID)
        {
            List<GetStudentExamDetailsByTeacherResult> studentExams = new();
            var data = await _contextProcedures.GetStudentExamDetailsByTeacherAsync(TeacherID);
            foreach (var item in data)
            {
                GetStudentExamDetailsByTeacherResult studentExam = new()
                {
                    StudentExamID = item.StudentExamID,
                    Grade = item.Grade,
                    SubmissionDate = item.SubmissionDate,
                    StudentNumber = item.StudentNumber,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    ExamType = item.ExamType,
                    Title = item.Title,
                    TotalMarks = item.TotalMarks,
                    StartDate = item.StartDate,
                    GradeLevel = item.GradeLevel,
                    ClassName = item.ClassName,
                    ExamQuizID = item.ExamQuizID,
                };
                studentExams.Add(studentExam);
            }
            return studentExams;
        }
        public async Task<IEnumerable<GetUpcomingExamsOrQuizzesResult>> GetUpcomingExamsOrQuizzesResults(int GradeLevel)
        {
            List<GetUpcomingExamsOrQuizzesResult> result = new();
            var data = await _contextProcedures.GetUpcomingExamsOrQuizzesAsync(GradeLevel);
            foreach (var item in data)
            {
                GetUpcomingExamsOrQuizzesResult examsOrQuizzesResult = new()
                {
                    ExamQuizID = item.ExamQuizID,
                    Title = item.Title,
                    Description = item.Description,
                    TeacherID = item.TeacherID,
                    ExamType = item.ExamType,
                    TotalMarks = item.TotalMarks,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    CreatedDate = item.CreatedDate,
                    ClassID = item.ClassID,
                };
                result.Add(examsOrQuizzesResult);
            }
            return result;
        }
        public async Task<IEnumerable<GetReportCardDetailsResult>> GetReportCardDetailsResults(Guid ReportCardID)
        {
            List<GetReportCardDetailsResult> reportCardDetailsResults = new();
            var result = await _contextProcedures.GetReportCardDetailsAsync(ReportCardID);
            foreach (var item in result)
            {
                GetReportCardDetailsResult detailsResult = new()
                {
                    ReportCardDetailID = item.ReportCardDetailID,
                    ReportCardID = item.ReportCardID,
                    ClassName = item.ClassName,
                    Score = item.Score,
                    Grade = item.Grade,
                    Final = item.Final
                };
                reportCardDetailsResults.Add(detailsResult);
            }
            return reportCardDetailsResults;
        }
        public async Task<IEnumerable<GetReportCardsByStudentResult>> GetReportCardsByStudents(string userID)
        {
            List<GetReportCardsByStudentResult> stds = new();
            var result = await _contextProcedures.GetReportCardsByStudentAsync(userID);
            foreach (var item in result)
            {
                GetReportCardsByStudentResult reportCard = new()
                {
                    ReportCardID = item.ReportCardID,
                    StudentID = item.StudentID,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    GradeLevel = item.GradeLevel,
                    Term = item.Term,
                    GPA = item.GPA,
                    IssuedDate = item.IssuedDate,
                    TermStartDate = item.TermStartDate,
                    TermEndDate = item.TermEndDate
                };
                stds.Add(reportCard);
            }
            return stds;
        }

        public async Task<IEnumerable<GetReportCardsByStudentResult>> GetReportCardsByParent(string userID)
        {
            List<GetReportCardsByStudentResult> stds = new();
            var result = await _contextProcedures.GetReportCardsByParentAsync(userID);
            foreach (var item in result)
            {
                GetReportCardsByStudentResult reportCard = new()
                {
                    ReportCardID = item.ReportCardID,
                    StudentID = item.StudentID,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    GradeLevel = item.GradeLevel,
                    Term = item.Term,
                    GPA = item.GPA,
                    IssuedDate = item.IssuedDate,
                    TermStartDate = item.TermStartDate,
                    TermEndDate = item.TermEndDate
                };
                stds.Add(reportCard);
            }
            return stds;
        }

        public async Task<IEnumerable<GetAttendanceByTeacherAndDateResult>> GetAttendances(string Teacher, DateTime date)
        {
            List<GetAttendanceByTeacherAndDateResult> stdAttdances = new();
            var result = await _contextProcedures.GetAttendanceByTeacherAndDateAsync(Teacher, date);
            foreach (var item in result)
            {
                GetAttendanceByTeacherAndDateResult attendanceByTeacherAndDateResult = new()
                {
                    StudentAttendanceID = item.StudentAttendanceID,
                    AttendanceDate = item.AttendanceDate,
                    StudentScheduleID = item.StudentScheduleID,
                    LateStatus = item.LateStatus,
                    AttendanceStatus = item.AttendanceStatus,
                    CreatedDate = item.CreatedDate,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    ClassName = item.ClassName,
                };
                stdAttdances.Add(attendanceByTeacherAndDateResult);
            }
            return stdAttdances;
        }
        public async Task<IEnumerable<CityDTO>> GetCitiesAsync(string CountryCode)
        {
            List<CityDTO> Cities = new();
            var result = await _contextProcedures.GetCitiesByCountryNameAsync(CountryCode);
            foreach (var item in result)
            {
                CityDTO cityDTO = new CityDTO()
                {
                    CountryCode = item.CountryCode,
                    CityName = item.CityName
                };
                Cities.Add(cityDTO);
            }
            return Cities;
        }
        public async Task<IEnumerable<GetStudentClassScheduleResult>> GetStudentClasses(string Student)
        {
            List<GetStudentClassScheduleResult> stdClasses = new();
            var result = await _contextProcedures.GetStudentClassScheduleAsync(Student);
            foreach (var item in result)
            {
                GetStudentClassScheduleResult studentClassSchedule = new()
                {
                    StudentClassScheduleID = item.StudentClassScheduleID,
                    StudentID = item.StudentID,
                    ScheduleID = item.ScheduleID,
                    ClassName = item.ClassName,
                    DayName = item.DayName,
                    Grade = item.Grade,
                    StartTime = item.StartTime,
                    EndTime = item.EndTime,
                    Description = item.Description,
                    TimeslotID = item.TimeslotID,
                };
                stdClasses.Add(studentClassSchedule);
            }
            return stdClasses;

        }
        public async Task<IEnumerable<GetStudentUnCompletedLessonsResult>> GetStudentUnCompletedLessons(string studentID)
        {
            List<GetStudentUnCompletedLessonsResult> getStudentUnCompletedLessons = new();
            var result = await _contextProcedures.GetStudentUnCompletedLessonsAsync(studentID);
            foreach (var item in result)
            {
                GetStudentUnCompletedLessonsResult studentUnCompletedLesson = new()
                {
                    ClassID = item.ClassID,
                    StudentID = item.StudentID,
                    LessonID = item.LessonID,
                    Title = item.Title,
                    Content = item.Content,
                    ClassName = item.ClassName,
                    DatePosted = item.DatePosted,
                    Rating = item.Rating,
                    LessonCompleted = item.LessonCompleted,

                };
                getStudentUnCompletedLessons.Add(studentUnCompletedLesson);
            }
            return getStudentUnCompletedLessons;
        }
        public async Task<IEnumerable<GetMissedClassesForPastWeekResult>> GetMissedClassesForPastWeekResults(string ParentID)
        {
            List<GetMissedClassesForPastWeekResult> getMissedClasses = new();
            var result = await _contextProcedures.GetMissedClassesForPastWeekAsync(null, ParentID);
            foreach (var item in result)
            {
                GetMissedClassesForPastWeekResult missedClassesForPastWeek = new()
                {
                    StudentID = item.StudentID,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    ClassName = item.ClassName,
                    AttendanceDate = item.AttendanceDate,
                };
                getMissedClasses.Add(missedClassesForPastWeek);
            }
            return getMissedClasses;
        }
        public async Task<IEnumerable<GetAttendanceTrendForPastSevenDaysResult>> GetAttendanceTrendForPastSeven(string ParentID)
        {
            List<GetAttendanceTrendForPastSevenDaysResult> getAttendanceTrendForPastSevenDaysResults = new();
            var result = await _contextProcedures.GetAttendanceTrendForPastSevenDaysAsync(null, ParentID);
            foreach (var item in result)
            {
                GetAttendanceTrendForPastSevenDaysResult getAttendanceTrendForPastSevenDaysResult = new()
                {
                    StudentID = item.StudentID,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    AttendancePercentage = item.AttendancePercentage,
                    TotalClasses = item.TotalClasses,
                    TotalPresent = item.TotalPresent,
                    TotalAbsent = item.TotalAbsent
                };
                getAttendanceTrendForPastSevenDaysResults.Add(getAttendanceTrendForPastSevenDaysResult);
            }
            return getAttendanceTrendForPastSevenDaysResults;
        }
        public async Task<IEnumerable<GetStudentPerformanceForCurrentYearResult>> StudentYearlyPerfomance(string StudentId)
        {
            List<GetStudentPerformanceForCurrentYearResult> getStudentPerformanceForCurrentYearResults = new();
            var result = await _contextProcedures.GetStudentPerformanceForCurrentYearAsync(StudentId, null);
            foreach (var item in result)
            {
                GetStudentPerformanceForCurrentYearResult studentPerformanceForCurrentYear = new()
                {
                    StudentID = item.StudentID,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    UserID = item.UserID,
                    AverageMonthlyScore = item.AverageMonthlyScore,
                    PreviousAverage = item.PreviousAverage,
                    IsImproving = item.IsImproving,
                    Year = item.Year,
                    MonthName = item.MonthName,
                };
                getStudentPerformanceForCurrentYearResults.Add(studentPerformanceForCurrentYear);

            }
            return getStudentPerformanceForCurrentYearResults;
        }
        public async Task<IEnumerable<GetStudentPerformanceForCurrentYearResult>> StudentYearlyPerfomanceParent(string StudentId)
        {
            List<GetStudentPerformanceForCurrentYearResult> getStudentPerformanceForCurrentYearResults = new();
            var result = await _contextProcedures.GetStudentPerformanceForCurrentYearAsync(null, StudentId);
            foreach (var item in result)
            {
                GetStudentPerformanceForCurrentYearResult studentPerformanceForCurrentYear = new()
                {
                    StudentID = item.StudentID,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    UserID = item.UserID,
                    AverageMonthlyScore = item.AverageMonthlyScore,
                    PreviousAverage = item.PreviousAverage,
                    IsImproving = item.IsImproving,
                    Year = item.Year,
                    MonthName = item.MonthName,
                };
                getStudentPerformanceForCurrentYearResults.Add(studentPerformanceForCurrentYear);

            }
            return getStudentPerformanceForCurrentYearResults;
        }
        public async Task<IEnumerable<GetActiveClassScheduleResult>> GetActiveClassSchedules(int grade, string ClassSection, string SchoolID)
        {
            List<GetActiveClassScheduleResult> activeClassScheduleResults = new();
            var Sect = ClassSection;
            if (ClassSection == "null")
            {
                Sect = null;
            }
            var result = await _contextProcedures.GetActiveClassScheduleAsync(grade, SchoolID, Sect);
            foreach (var item in result)
            {
                GetActiveClassScheduleResult activeClassSchedule = new()
                {
                    ClassScheduleID = item.ClassScheduleID,
                    ClassID = item.ClassID,
                    TimeSlotID = item.TimeSlotID,
                    DayOfTheWeekID = item.DayOfTheWeekID,
                    IsActive = item.IsActive,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    ClassName = item.ClassName,
                    Grade = item.Grade
                };

                activeClassScheduleResults.Add(activeClassSchedule);
            }
            return activeClassScheduleResults;
        }

    }

}

