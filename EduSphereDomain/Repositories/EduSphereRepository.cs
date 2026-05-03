using EduSphereDomain.AchievementData;
using EduSphereDomain.Data;
using EduSphereDomain.FinanceData;
using EDUSphereSharedProject.AchievementModels;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.Models.StoreProModels;
using EDUSphereSharedProject.UniversalModels;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using Microsoft.EntityFrameworkCore;
using static EDUSphereSharedProject.UniversalModels.StudentWithClassesDTO;

namespace EduSphereDomain.Repositories
{
    public class EduSphereRepository
    {
        private readonly PhoenixEdusphereContext _context;
        private readonly PhoenixEdusphereContextProcedures _contextProcedures;
        private readonly AchievementContext _achivementContext;
        private readonly AchievementContextProcedures _achievementContextProcedures;
        private readonly PhoenixEdusphereFinanceContextProcedures _financeContextProcedures;
        public EduSphereRepository(PhoenixEdusphereContext context, PhoenixEdusphereContextProcedures phoenixEdusphereContextProcedures, AchievementContext achivementContext, AchievementContextProcedures achievementContextProcedures, PhoenixEdusphereFinanceContextProcedures contextProcedures)
        {
            _context = context;
            _contextProcedures = phoenixEdusphereContextProcedures;
            _achivementContext = achivementContext;
            _achievementContextProcedures = achievementContextProcedures;
            _financeContextProcedures = contextProcedures;
        }


        #region Main Modules
        public async Task<IEnumerable<Class>> GetTeacherSubjectsByID(Guid id)
        {
            var result = await _context.Classes.Where(x => x.TeacherID == id).ToListAsync();
            return result;

        }
        public async Task<IEnumerable<LiveMeeting>> GetMeetingsAsync(string MeetingID)
        {
            var result = await _contextProcedures.GetLiveMeetingAsync(MeetingID);
            return result.Select(x => new LiveMeeting
            {
                MeetingID = x.MeetingID,
                MeetingNumber = x.MeetingNumber,
                MeetingPassword = x.MeetingPassword,
                CreatedBy = x.CreatedBy,
                CreatorEmail = x.CreatorEmail,
                CreatedDate = x.CreatedDate,
                ScheduledDate = x.ScheduledDate,
                isDone = x.isDone

            }).ToList();
        }

        public async Task<IEnumerable<AcademicLevel>> GetAcademicLevelsAsync(string SchoolID)
        {
            var result = await _context.AcademicLevels.Where(x => x.SchoolID == Guid.Parse(SchoolID) && x.isActive == true).ToListAsync();
            return result;
        }

        public async Task<IEnumerable<AcademicLevel>> GetFullAcademicStructureForSchool(Guid SchoolID)
        {
            var result = await _context.AcademicLevels
                .Where(x => x.SchoolID == SchoolID && x.isActive == true).ToListAsync();
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
        public async Task<IEnumerable<MultipleChoiceAssignmentAnswer>> GetAssingmentMultipleChoices(Guid ID)
        {
            var result = await _context.MultipleChoiceAssignmentAnswers.Where(x => x.AssignmentQuestionID == ID).ToListAsync();
            return result;
        }
        public async Task<IEnumerable<ExamTestQuizMultipleChoiceAnswer>> GetExamMultipleChoices(Guid ID)
        {
            var result = await _context.ExamTestQuizMultipleChoiceAnswers.Where(x => x.ExamTestQuizQuestionID == ID).ToListAsync();
            return result;
        }
        public async Task<IEnumerable<GetInitializationDataResult>> GetInitializationDataResults(string ID)
        {
            try
            {
                var result = await _contextProcedures.GetInitializationDataAsync(ID);
                return result.Select(x => new GetInitializationDataResult
                {
                    StudentID = x.StudentID,
                    RoleName = x.RoleName,
                    HideStudentDashboard = x.HideStudentDashboard,
                    UserName = x.UserName,
                    Email = x.Email,
                    SchoolID = x.SchoolID,
                    SchoolName = x.SchoolName,
                    SchoolLogo = x.SchoolLogo,
                    HasplagerismEnaabled = x.HasplagerismEnaabled,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    SchoolCurrencyName = x.SchoolCurrencyName,
                    CurrencySymbol = x.CurrencySymbol,
                    CurrencyCode = x.CurrencyCode,
                    CurrencyCountry = x.CurrencyCountry,
                });
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                Console.Write(_);
                throw;
            }

        }
        public async Task<IEnumerable<LessonMedium>> GetLessonMedia(Guid Id)
        {
            var result = await _context.LessonMedia.Where(x => x.LessonID == Id).ToListAsync();
            return result;
        }
        public async Task<IEnumerable<Course>> GetTeacherCourses(Guid TeacherID)
        {
            var courses = await _context.Courses.Where(x => x.TeacherID == TeacherID).ToListAsync();
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
        public async Task<IEnumerable<TimetableOverride>> GetTimeTableOverrides(Guid SchoolId)
        {
            var result = await _financeContextProcedures.GetTimetableOverridesBySchoolAsync(SchoolId);
            return result.Select(x => new TimetableOverride
            {
                AcademicLevel = x.AcademicLevel,
                StudentGroup = x.StudentGroup,
                GradeSection = x.GradeSection,
                TimeSlotID = x.TimeSlotID,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                DayOfTheWeekID = x.DayOfTheWeekID,
                ReplacementClassID = x.ReplacementClassID,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo,
                IsActive = x.IsActive,
                Reason = x.Reason,
                TimetableOverrideID = x.TimetableOverrideID
            }).ToList();
        }
        public async Task<List<ClassSchedule>> UpsertClassSchedulesAsync(
    List<ClassSchedule> incomingSchedules)
        {
            if (incomingSchedules == null || incomingSchedules.Count == 0)
                return new List<ClassSchedule>();

            // Defensive validation
            foreach (var s in incomingSchedules)
            {
                if (s.SchoolID == Guid.Empty)
                    throw new ArgumentException("SchoolID is required");

                if (!s.DayOfTheWeekID.HasValue || !s.TimeSlotID.HasValue)
                    throw new ArgumentException("DayOfTheWeekID and TimeSlotID are required");
            }

            var schoolId = incomingSchedules.First().SchoolID;
            var academicLevel = incomingSchedules.First().AcademicLevel;
            var section = incomingSchedules.First().AcademicLevelSection;

            // Load existing schedules ONCE
            var existingSchedules = await _context.ClassSchedules
                .Where(x =>
                    x.SchoolID == schoolId &&
                    x.AcademicLevel == academicLevel &&
                    x.AcademicLevelSection == section &&
                    x.IsActive == true)
                .ToListAsync();

            // Logical identity map
            var existingMap = existingSchedules.ToDictionary(
                x => (x.DayOfTheWeekID!.Value, x.TimeSlotID!.Value),
                x => x
            );

            var persisted = new List<ClassSchedule>();

            foreach (var incoming in incomingSchedules)
            {
                var key = (incoming.DayOfTheWeekID!.Value, incoming.TimeSlotID!.Value);

                if (existingMap.TryGetValue(key, out var existing))
                {
                    // 🔁 UPDATE
                    existing.ClassID = incoming.ClassID;
                    existing.ScheduledActivity = incoming.ScheduledActivity;
                    existing.IsDoublePeriod = incoming.IsDoublePeriod;
                    existing.SlotOrder = incoming.SlotOrder;
                    existing.IsFiller = incoming.IsFiller;
                    existing.StartDate = incoming.StartDate;
                    existing.EndDate = incoming.EndDate;
                    existing.LevelSectionName = incoming.LevelSectionName;

                    persisted.Add(existing);
                }
                else
                {
                    // ➕ INSERT
                    incoming.ClassScheduleID = Guid.NewGuid();
                    incoming.IsActive = true;

                    _context.ClassSchedules.Add(incoming);
                    persisted.Add(incoming);
                }
            }

            await _context.SaveChangesAsync();

            return persisted;
        }



        public async Task<IEnumerable<GetStudentsInGradeResult>> GetStudentsInGrade(int grade, string SchoolID)
        {
            List<GetStudentsInGradeResult> stds = new();
            var result = await _contextProcedures.GetStudentsInGradeAsync(grade, SchoolID);
            foreach (var item in result)
            {
                GetStudentsInGradeResult studentInGrade = new()
                {
                    StudentID = item.StudentID,
                    GradeSection = item.GradeSection,
                    GroupName = item.GroupName,
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

        public async Task<IEnumerable<Student>> GetStudentsByGrade(int grade, string SchoolID)
        {
            List<Student> stds = new();
            var result = await _contextProcedures.GetStudentsInGradeDetailsAsync(grade, Guid.Parse(SchoolID));
            foreach (var item in result)
            {
                Student student = new()
                {
                    StudentID = item.StudentID,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    AcademicLevel = item.AcademicLevel,
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
                    StudentID = item.StudentID,
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
                    Final = item.Final,
                    ClassID = item.ClassID

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
                    AcademicLevel = item.AcademicLevel,
                    Term = item.Term,
                    GPA = item.GPA,
                    IssuedDate = item.IssuedDate,
                    TermStartDate = item.TermStartDate,
                    TermEndDate = item.TermEndDate,
                    SchoolEmail = item.SchoolEmail,
                    SchoolName = item.SchoolName,
                    SchoolWebsite = item.SchoolWebsite,
                    MarksInBestSix = item.MarksInBestSix,
                    PointsInBestSix = item.PointsInBestSix,
                    DeanName = item.DeanName,
                    DeansComment = item.DeansComment,
                    PositionInClass = item.PositionInClass,
                    PrincipleName = item.PrincipleName,
                    PrinciplesComment = item.PrinciplesComment,
                    ReportCardType = item.ReportCardType,
                    ClassName = $"{item.LevelName}({item.GradeSection})",
                    isGCE = item.isGCE,

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
                    AcademicLevel = item.AcademicLevel,
                    Term = item.Term,
                    GPA = item.GPA,
                    IssuedDate = item.IssuedDate,
                    TermStartDate = item.TermStartDate,
                    TermEndDate = item.TermEndDate,
                    SchoolEmail = item.SchoolEmail,
                    SchoolName = item.SchoolName,
                    SchoolWebsite = item.SchoolWebsite,
                    MarksInBestSix = item.MarksInBestSix,
                    PointsInBestSix = item.PointsInBestSix,
                    DeanName = item.DeanName,
                    DeansComment = item.DeansComment,
                    PositionInClass = item.PositionInClass,
                    PrincipleName = item.PrincipleName,
                    PrinciplesComment = item.PrinciplesComment,
                    ReportCardType = item.ReportCardType,
                    ClassName = $"{item.LevelName}({item.GradeSection})",
                    isGCE = item.isGCE,

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
                    AcademicLevel = item.AcademicLevel,
                    LevelName = item.LevelName,
                    StudentID = item.StudentID,
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
        public async Task<IEnumerable<GetStudentTimetableResult>> GetStudentClasses(string Student)
        {
            List<GetStudentTimetableResult> stdClasses = new();
            var result = await _contextProcedures.sp_GetStudentTimetableAsync(Student, DateTime.Today);
            foreach (var x in result)
            {
                GetStudentTimetableResult studentClassSchedule = new()
                {
                    TimeslotID = x.TimeslotID,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    TimeSlotDescription = x.TimeSlotDescription,
                    SlotType = x.SlotType,
                    DayID = x.DayID,
                    DayName = x.DayName,
                    ClassScheduleID = x.ClassScheduleID,
                    ClassID = x.ClassID,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    SchoolID = x.SchoolID,
                    EntryType = x.EntryType,
                    ScheduledClassID = x.ScheduledClassID,
                    ActivityID = x.ActivityID,
                    ActivityName = x.ActivityName,
                    ClassName = x.ClassName,
                    LevelName = x.LevelName,
                    TeacherFirstName = x.TeacherFirstName,
                    TeacherLastName = x.TeacherLastName,

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
                    Grade = item.AcademicLevel,
                };

                activeClassScheduleResults.Add(activeClassSchedule);
            }
            return activeClassScheduleResults;
        }
        #endregion


        #region New Modules

        #region  Improvements
        public async Task<IEnumerable<LevelSection>> GetLevelSectionsAsync(Guid AcademicLevelID)
        {
            var result = await _context.LevelSections
                .Where(x => x.AcademicLevelID == AcademicLevelID && x.IsActive == true)
                .ToListAsync();
            return result;
        }
        public async Task<IEnumerable<ClassTeacher>> GetClassTeachersBySchoolIDAsync(Guid SchoolID)
        {
            var result = await _context.ClassTeachers
                .Where(x => x.SchoolID == SchoolID)
                .ToListAsync();

            return result;
        }
        public async Task<IEnumerable<LevelSection>> GetLevelSectionsBySchoolAsync(Guid SchoolID)
        {
            var result = await _context.LevelSections
                .Where(x => x.SchoolID == SchoolID && x.IsActive == true)
                .ToListAsync();
            return result;
        }

        public async Task<IEnumerable<StudentGradedExamsTestsAndQuizzes>> GetGradedExamsByStudent(string StudentID)
        {
            var result = await _contextProcedures.GetGradedStudentExamsAsync(Guid.Parse(StudentID));
            return result.Select(x => new StudentGradedExamsTestsAndQuizzes
            {
                StudentID = x.StudentID,
                ExamID = x.ExamID,
                Status = x.Status,
                StudentExamID = x.StudentExamID,
                SubmissionDate = x.SubmissionDate,
                Grade = x.Grade,
                TeacherFirstName = x.TeacherFirstName,
                TeacherLastName = x.TeacherLastName,
                Title = x.Title,
                TotalMarks = x.TotalMarks,

            }).ToList();
        }

        public async Task<IEnumerable<StudentGradedAssignments>> GetGradedAssignmentsByStudent(string StudentID)
        {
            var result = await _contextProcedures.GetGradedStudentAssignmentsAsync(Guid.Parse(StudentID));
            return result.Select(a => new StudentGradedAssignments
            {
                StudentID = a.StudentID,
                AssignmentID = a.AssignmentID,
                StudentAssignmentID = a.StudentAssignmentID,
                Grade = a.Grade,
                Status = a.Status,
                SubmissionDate = a.SubmissionDate,
                QuestionID = a.QuestionID,
                AssignmentTotalMarks = a.AssignmentTotalMarks,
                TeacherFirstName = a.TeacherFirstName,
                TeacherLastName = a.TeacherLastName,
                Title = a.Title,


            }).ToList();
        }

        public async Task<IEnumerable<Student>> GetStudentsBySchool(string SchoolID)
        {
            var result = await _contextProcedures.GetStudentsBySchoolAsync(Guid.Parse(SchoolID));
            return result.Select(s => new Student
            {
                StudentID = s.StudentID,
                FirstName = s.FirstName,
                LastName = s.LastName,
                AcademicLevel = s.AcademicLevel,
                ParentID = s.ParentID,
                Gender = s.Gender,
                Address = s.Address,
                StudentNumber = s.StudentNumber,
                ProfilePic = s.ProfilePic,
                UserID = s.UserID,
                DateOnBoarded = s.DateOnBoarded,
                Country = s.Country,
                City = s.City,
                SchoolID = s.SchoolID,
                GradeSection = s.GradeSection,
                LevelName = s.LevelName,
            }).ToList();
        }
        public async Task<IEnumerable<Student>> GetStudentByUserID(string UserID)
        {
            var result = await _contextProcedures.GetStudentByUserIDAsync(Guid.Parse(UserID));
            return result.Select(s => new Student
            {
                StudentID = s.StudentID,
                FirstName = s.FirstName,
                LastName = s.LastName,
                AcademicLevel = s.AcademicLevel,
                ParentID = s.ParentID,
                Gender = s.Gender,
                Address = s.Address,
                StudentNumber = s.StudentNumber,
                ProfilePic = s.ProfilePic,
                UserID = s.UserID,
                DateOnBoarded = s.DateOnBoarded,
                Country = s.Country,
                City = s.City,
                SchoolID = s.SchoolID,
                GradeSection = s.GradeSection,
                LevelName = s.LevelName,
            }).ToList();
        }

        private static readonly Func<PhoenixEdusphereContext, Guid, DateTime, CancellationToken, Task<TermSetting?>> _getCurrentActiveTermCompiled
     = EF.CompileAsyncQuery((PhoenixEdusphereContext ctx, Guid schoolId, DateTime today, CancellationToken ct) =>
         ctx.TermSettings
            .AsNoTracking()
            .Where(t => t.SchoolID == schoolId
                     && t.ActualStartTermDate <= today
                     && t.ActualEndTermDate >= today)
            .OrderByDescending(t => t.ActualStartTermDate)
            .FirstOrDefault());

        public async Task<List<TermSetting>> GetCurrentActiveTermsAsync(Guid schoolId, CancellationToken cancellationToken = default)
        {
            var today = DateTime.Now.Date;

            var termSetting = await _getCurrentActiveTermCompiled(_context, schoolId, today, cancellationToken);

            // Return as a list, empty if no term is active
            return termSetting != null
                ? new List<TermSetting> { termSetting }
                : new List<TermSetting>();
        }
        private static readonly Func<PhoenixEdusphereContext, Guid, CancellationToken, Task<List<Teacher>>> _getTeachersBySchoolCompiled
    = EF.CompileAsyncQuery((PhoenixEdusphereContext ctx, Guid schoolId, CancellationToken ct) =>
        ctx.Teachers
           .AsNoTracking()
           .Where(t => t.SchoolID == schoolId)
           .OrderBy(t => t.LastName)
           .ToList());


        public async Task<List<Teacher>> GetTeachersBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default)
        {
            return await _getTeachersBySchoolCompiled(_context, schoolId, cancellationToken);
        }
        private static readonly Func<PhoenixEdusphereContext, Guid, CancellationToken, Task<List<Class>>> _getClassesBySchoolCompiled
    = EF.CompileAsyncQuery((PhoenixEdusphereContext ctx, Guid schoolId, CancellationToken ct) =>
        ctx.Classes
           .AsNoTracking()
           .Where(c => c.SChoolID == schoolId)
           .Include(c => c.Teacher)  // pre-load teacher
           .OrderBy(c => c.ClassName)
           .ToList());


        public async Task<List<Class>> GetClassesBySchoolAsync(Guid schoolId, CancellationToken cancellationToken = default)
        {
            return await _getClassesBySchoolCompiled(_context, schoolId, cancellationToken);
        }
        public Task<List<GradingScale>> GetGradingScalesBySchoolAsync(Guid schoolId)
        {
            return _context.GradingScales
                .AsNoTracking()
                .Where(g => g.SchoolID == schoolId)
                .OrderBy(g => g.LoweScore)
                .ToListAsync();
        }

        public async Task<List<StudentWithClassesDTO>> GetStudentsWithClassesBySchoolAsync(Guid schoolId)
        {
            try
            {
                return await _context.Students
                     .AsNoTracking()
                     .Where(s => s.SchoolID == schoolId)
                     .Select(s => new StudentWithClassesDTO
                     {
                         StudentID = s.StudentID,
                         FirstName = s.FirstName,
                         LastName = s.LastName,
                         AcademicLevel = s.AcademicLevel ?? 0,
                         LevelName = s.LevelName ?? "",
                         Gender = s.Gender,
                         StudentNumber = s.StudentNumber,
                         AcademicLevelID = s.AcademicLevelID,
                         LevelSectionID = s.LevelSectionID,
                         GradeSection = s.GradeSection ?? "",

                         Classes = s.StudentClasses
                             .Where(sc => sc.Class != null) // safety
                             .Select(sc => new StudentClassDTO
                             {
                                 ClassID = sc.Class.ClassID,
                                 ClassName = sc.Class.ClassName,
                                 TeacherID = sc.Class.TeacherID
                             })
                             .ToList()
                     })
                     .ToListAsync();
            }
            catch (Exception ex)
            {
                // optional logging here
                throw;
            }
        }

        public async Task<List<ReportCard>> GetReportCardHeaderByStudent(Guid StudentID, string ReportCardType)
        {
            var today = DateTime.Now.Date;
            return await _context.ReportCards
                .Where(s => s.StudentID == StudentID
                 && s.TermStartDate <= today
                     && s.TermEndDate >= today
                     && s.ReportCardType == ReportCardType
                ).ToListAsync();
        }
        #endregion
        //-------------------------START-----------------------------------------------------------------\\
        #region Hostel Management

        public async Task<IEnumerable<RoomAsset>> GetRoomAssetsByRoom(Guid RoomID)
        {
            var rooms = await _context.RoomAssets.Where(x => x.RoomId == RoomID).ToListAsync();
            return rooms;
        }
        public async Task<IEnumerable<GetRoomsBySchoolResult>> GetRoomsBySchools(Guid SchoolID)
        {
            try
            {
                var rooms = await _contextProcedures.GetRoomsBySchoolAsync(SchoolID);
                return rooms.Select(r => new GetRoomsBySchoolResult
                {
                    RoomId = r.RoomId,
                    HostelId = r.HostelId,
                    RoomNumber = r.RoomNumber,
                    MaxOccupancy = r.MaxOccupancy,
                    RoomType = r.RoomType,
                    HostelName = r.HostelName,
                    GenderId = r.GenderId,
                    GenderDescription = r.GenderDescription,
                    OccupiedCount = r.OccupiedCount,
                    IsFull = r.IsFull
                }).ToList();
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                throw;
            }
        }
        public async Task<StudentRoomLog> GetRoomLogByID(Guid RoomID)
        {
            try
            {
                var log = await _context.StudentRoomLogs.Where(x => x.RoomId == RoomID && x.Status == "Active")
                            .FirstAsync();
                return log;
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                Console.WriteLine(_);
                throw;
            }
        }
        public async Task<IEnumerable<GetHostelsBySchoolResult>> GetHostelsBySchool(Guid SchoolID)
        {
            try
            {
                var hostels = await _contextProcedures.GetHostelsBySchoolAsync(SchoolID);
                return hostels.Select(x => new GetHostelsBySchoolResult
                {
                    HostelId = x.HostelId,
                    HostelName = x.HostelName,
                    Gender = x.Gender,
                    Capacity = x.Capacity,
                    Supervisor = x.Supervisor,
                    SchoolId = x.SchoolId,
                    Occupied = x.Occupied,
                    CreatedAt = x.CreatedAt,
                    GenderID = x.GenderID,
                    SupervisorID = x.SupervisorID,

                }).ToList();
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                throw;
            }

        }
        public async Task<IEnumerable<StudentsWithoutRoomDTO>> StudentsWithoutRooms(Guid GenderID, Guid SchoolID)
        {
            var result = await _contextProcedures.GetStudentsWithoutRoomsByGenderAsync(SchoolID, GenderID);
            return result.Select(s => new StudentsWithoutRoomDTO
            {
                StudentID = s.StudentID,
                FirstName = s.FirstName,
                LastName = s.LastName,
                AcademicLevel = s.AcademicLevel,
                ParentID = s.ParentID,
                GenderId = s.GenderId,
                GenderDescription = s.GenderDescription,
                Address = s.Address,
                StudentNumber = s.StudentNumber,
                ProfilePic = s.ProfilePic,
                UserID = s.UserID,
                DateOnBoarded = s.DateOnBoarded,
                Country = s.Country,
                City = s.City,
                SchoolID = s.SchoolID,
                GradeSection = s.GradeSection,
                LevelName = s.LevelName
            }).ToList();
        }

        public async Task<IEnumerable<StudentsInRoomDTO>> StudentsInRoom(Guid RoomID)
        {
            var result = await _contextProcedures.GetStudentsInRoomAsync(RoomID);
            return result.Select(s => new StudentsInRoomDTO
            {
                StudentID = s.StudentID,
                FirstName = s.FirstName,
                LastName = s.LastName,
                AcademicLevel = s.AcademicLevel,
                ParentID = s.ParentID,
                GenderId = s.GenderId,
                GenderDescription = s.GenderDescription,
                Address = s.Address,
                StudentNumber = s.StudentNumber,
                ProfilePic = s.ProfilePic,
                UserID = s.UserID,
                DateOnBoarded = s.DateOnBoarded,
                Country = s.Country,
                City = s.City,
                SchoolID = s.SchoolID,
                GradeSection = s.GradeSection,
                LevelName = s.LevelName,
                AllocationId = s.AllocationId,
                CheckInDate = s.CheckInDate,
                CheckOutDate = s.CheckOutDate,
                Status = s.Status
            }).ToList();
        }
        public async Task<IEnumerable<MaintainanceRequestsDTO>> GetMaintainanceRequests(Guid SchoolID)
        {
            var result = await _contextProcedures.GetHostelMaintainanceRequestsBySchoolAsync(SchoolID);
            return result.Select(x => new MaintainanceRequestsDTO
            {
                RequestID = x.RequestID,
                HostelID = x.HostelID,
                RoomID = x.RoomID,
                ProblemDescription = x.ProblemDescription,
                ReportedBy = x.ReportedBy,
                DateReported = x.DateReported,
                DateResolved = x.DateResolved,
                Status = x.Status,
                HostelName = x.HostelName,
                RoomNumber = x.RoomNumber

            }).ToList();
        }
        #endregion
        //-------------------------END-----------------------------------------------------------------\\

        //-------------------------START-----------------------------------------------------------------\\
        #region Dining Management

        public async Task<IEnumerable<InventoryBatch>> GetKitchenInventoryBatchesAsync(Guid SchoolID)
        {
            var result = await _contextProcedures.GetInventoryBatchesBySchoolAsync(SchoolID);
            return result.Select(x => new InventoryBatch
            {
                BatchID = x.BatchID,
                FoodName = x.ItemName,
                FoodItemID = x.FoodItemID,
                Quantity = x.Quantity,
                ReceivedDate = x.ReceivedDate,
                ExpiryDate = x.ExpiryDate,
                StorageLocation = x.StorageLocation,
                CurrentTemp = x.CurrentTemp,
                Status = x.Status,
                Notes = x.Notes,
                AIAdviceGenerated = x.AIAdviceGenerated,
                Unit = x.Unit,
                RecievedDateAltered = x.ReceivedDate,
                hasExpiry = x.HasFixedExpiry,
                ShelfLifeDays = x.DefaultShelfLife ?? 0,
                Category = x.Category
            }).ToList();


        }
        public async Task<IEnumerable<DiningHall>> GetDiningHallsBySchoolAsync(Guid SchoolID)
        {
            var result = await _context.DiningHalls.Where(x => x.SchoolId == SchoolID).ToListAsync();
            return result;
        }
        public async Task<IEnumerable<SchoolMealSessionsDTO>> GetSchoolMealSessions(Guid SchoolID)
        {
            var result = await _contextProcedures.GetSchoolMealSessionsAsync(SchoolID);
            return result.Select(x => new SchoolMealSessionsDTO
            {
                MealId = x.MealId,
                DiningHallId = x.DiningHallId,
                Name = x.Name,
                TimeStart = x.TimeStart,
                TimeEnd = x.TimeEnd
            }).ToList();
        }
        public async Task<IEnumerable<DiningMenuDTO>> GetDiningMenus(Guid SchoolID)
        {
            var result = await _contextProcedures.GetSchoolDiningMenusAsync(SchoolID);
            return result.Select(x => new DiningMenuDTO
            {
                SideDish = x.SideDish,
                DayOfWeek = x.DayOfWeek,
                Drink = x.Drink,
                MainDish = x.MainDish,
                MealId = x.MealId,
                MealName = x.MealName,
                MenuId = x.MenuId,

            }).ToList();
        }

        public async Task<IEnumerable<StudentsWithSpecialDietsDTO>> GetSpecialDiets(Guid SchoolID)
        {
            var result = await _contextProcedures.GetStudentsWithSpecialDietsAsync(SchoolID);
            return result.Select(x => new StudentsWithSpecialDietsDTO
            {
                AcademicLevel = x.AcademicLevel,
                Description = x.Description,
                DietId = x.DietId,
                StudentId = x.StudentId,
                StudentNumber = x.StudentNumber,
                DietType = x.DietType,
                FirstName = x.FirstName,
                Gender = x.Gender,
                LastName = x.LastName,
                ProfilePic = x.ProfilePic

            }).ToList();
        }
        #endregion
        //-------------------------END-----------------------------------------------------------------\\
        public async Task UpdateStudentPaymentStatus(string studentNumber, bool isPaid)
        {
            // Get the student by student number
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);

            if (student == null)
                throw new ArgumentException($"Student with number {studentNumber} not found.");

            // Update payment status
            student.PaymentStatus = isPaid;

            // Save changes
            await _context.SaveChangesAsync();
        }
        /// <summary>
        /// Generates a unique username for a single user.
        /// </summary>
        public async Task<string> GenerateNextUsernameAsync(string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Invalid names.");

            string baseUsername = (firstName[0] + lastName).ToLower();

            var existingUsernames = await _context.AspNetUsers
                .Where(u => u.UserName.StartsWith(baseUsername))
                .Select(u => u.UserName)
                .ToListAsync();

            if (!existingUsernames.Contains(baseUsername))
                return baseUsername;

            int suffix = 1;
            string candidate;
            do
            {
                candidate = baseUsername + suffix;
                suffix++;
            } while (existingUsernames.Contains(candidate));

            return candidate;
        }

        /// <summary>
        /// Generates unique usernames for a list of students in bulk.
        /// </summary>
        public async Task<List<string>> GenerateNextUsernamesAsync(
     List<(string FirstName, string LastName)> names)
        {
            // Fetch ALL usernames once
            var existingUsernames = await _context.AspNetUsers
                .Select(u => u.UserName.ToLower())
                .ToListAsync();

            var result = new List<string>();

            foreach (var (firstName, lastName) in names)
            {
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    result.Add(string.Empty);
                    continue;
                }

                string baseUsername = (firstName[0] + lastName).ToLower();
                string candidate = baseUsername;
                int suffix = 1;

                while (existingUsernames.Contains(candidate))
                {
                    candidate = baseUsername + suffix;
                    suffix++;
                }

                result.Add(candidate);
                existingUsernames.Add(candidate); // reserve it
            }

            return result;
        }


        public async Task<List<Student>> GetStudentsByStudentNumbers(
     List<string> studentNumbers,
     Guid schoolId)
        {
            return await _context.Students
                .Where(s =>
                    studentNumbers.Contains(s.StudentNumber) &&
                    s.SchoolID == schoolId)
                .ToListAsync();
        }

        public async Task<IEnumerable<TimeTableActivity>> GetSchoolActivities(Guid SchoolID)
        {
            var result = await _context.TimeTableActivities
                .Where(x => x.SchoolID == SchoolID).ToListAsync();

            return result;
        }

        public async Task<List<TeacherScheduleConstraints>> GetTeacherConstraintsAsync(
      List<Guid>? teacherIDs)
        {
            var classSchedules = await _context.ClassSchedules
                .Include(cs => cs.Class)
                .Include(cs => cs.TimeSlot)
                .Include(cs => cs.DayOfTheWeek) // REQUIRED
                .Where(cs =>
                    cs.Class.TeacherID.HasValue &&
                    teacherIDs.Contains(cs.Class.TeacherID.Value))
                .ToListAsync();

            var groupedByTeacher = classSchedules
                .GroupBy(cs => cs.Class.TeacherID!.Value);

            var teacherConstraints = new List<TeacherScheduleConstraints>();

            foreach (var group in groupedByTeacher)
            {
                var unavailableSlots = group
                    .Select(cs =>
                    {
                        // 🔥 CRITICAL: hydrate NotMapped field
                        cs.TimeSlot.Day = Enum.Parse<DayOfWeek>(
                            cs.DayOfTheWeek.DayName,
                            ignoreCase: true
                        );

                        return cs.TimeSlot;
                    })
                    .DistinctBy(ts => new { ts.Day, ts.StartTime })
                    .ToList();

                teacherConstraints.Add(new TeacherScheduleConstraints
                {
                    TeacherId = group.Key,
                    MaxDailyPeriods = 9, // hard set as agreed
                    UnavailableSlots = unavailableSlots
                });
            }

            return teacherConstraints;
        }


        #endregion
        #region Clinic Module
        public async Task<IEnumerable<vw_ClinicDashboard_Metric>> GetDashboardMetric(Guid ClinicID)
        {
            var result = await _contextProcedures.sp_GetClinicDashboardMetricsAsync(ClinicID);
            return result.Select(x => new vw_ClinicDashboard_Metric
            {
                CurrentlySickStudents = x.CurrentlySickStudents,
                ExpiringSoonMedications = x.ExpiringSoonMedications,
                LowStockMedications = x.LowStockMedications,
                VisitsLast30Days = x.VisitsLast30Days,
            }).ToList();
        }
        public async Task<IEnumerable<Clinic>> GetSchoolClinics(Guid SchoolID)
        {
            var resuilt = await _context.Clinics.Where(x => x.SchoolId == SchoolID).ToListAsync();
            return resuilt;
        }
        public async Task<IEnumerable<ClinicStaff>> GetClinicStaffBySchool(Guid SchoolId)
        {
            var result = await _contextProcedures.GetClinicStaffBySchoolAsync(SchoolId);
            return result.Select(x => new ClinicStaff
            {
                ClinicId = x.ClinicId,
                Contact = x.Contact,
                FullName = x.FullName,
                Role = x.Role,
                StaffId = x.StaffId,
                UserID = x.UserID,
                EmployeeID = x.EmployeeID
            }).ToList();
        }
        public async Task<IEnumerable<ClinicMedication>> GetMedicationsBySchoolAsync(Guid SchoolID)
        {
            var result = await _contextProcedures.GetClinicMedicationsBySchoolAsync(SchoolID);
            return result.Select(x => new ClinicMedication
            {
                MedicationId = x.MedicationId,
                Name = x.Name,
                Stock = x.Stock,
                Unit = x.Unit,
                ExpiryDate = x.ExpiryDate,
                ClinicID = x.ClinicID,
                BatchNumber = x.BatchNumber
            }).ToList();
        }

        public async Task<IEnumerable<ClinicVisit>> GetClinicVisits(Guid clinicId)
        {
            var rows = await _contextProcedures
                .GetClinicVisitsWithMedicationLogsAsync(clinicId);

            var visits = rows
                .GroupBy(x => x.VisitId)
                .Select(g =>
                {
                    var first = g.First();

                    return new ClinicVisit
                    {
                        VisitId = first.VisitId,
                        ClinicId = first.ClinicId,
                        StudentId = first.StudentId,
                        VisitDate = first.VisitDate,
                        Symptoms = first.Symptoms,
                        Diagnosis = first.Diagnosis,
                        Treatment = first.Treatment,
                        Notes = first.Notes,
                        AttendedByStaffId = first.AttendedByStaffId,
                        DaysOff = first.DaysOff,
                        ReturnToClassDate = first.ReturnToClassDate,
                        isActive = first.IsActive,

                        ClinicMedicationLogs = g
                            .Where(x => x.LogId.HasValue)
                            .Select(x => new ClinicMedicationLog
                            {
                                LogId = x.LogId!.Value,
                                VisitId = x.VisitId,
                                MedicationId = x.MedicationId!.Value,
                                QuantityUsed = x.QuantityUsed!.Value
                            })
                            .ToList()
                    };
                })
                .ToList();

            return visits;
        }

        public async Task<IEnumerable<ClinicDashboardVisitsDTO>> GetActiveClinicVisits(Guid ClinicID)
        {
            var result = await _contextProcedures.sp_GetActiveClinicVisitsAsync(ClinicID);
            return result.Select(x => new ClinicDashboardVisitsDTO
            {
                FirstName = x.FirstName,
                LastName = x.LastName,
                ProfilePic = x.ProfilePic,
                VisitDate = x.VisitDate,
                Symptoms = x.Symptoms,
                Diagnosis = x.Diagnosis,
                Treatment = x.Treatment,
            }).ToList();
        }
        #endregion
        #region Transport
        public async Task<IEnumerable<BusMaintenanceRequest>> GetMaintenanceRequestsAsync(Guid SchoolID)
        {
            var result = await _contextProcedures.sp_GetBusMaintenanceRequestsAsync(SchoolID);
            return result.Select(x => new BusMaintenanceRequest
            {
                ActualCost = x.ActualCost,
                ApprovedDate = x.ApprovedDate,
                BusId = x.BusId,
                HandledByStaffId = x.HandledByStaffId,
                ReportedByStaffId = x.ReportedByStaffId,
                RegistrationNumber = x.RegistrationNumber,
                SchoolId = x.SchoolId,
                Status = x.Status,
                CompletedDate = x.CompletedDate,
                Description = x.Description,
                EmployeeID = x.EmployeeID,
                ReportingStaffFirstName = x.ReportingStaffFirstName,
                ReportingStaffLastName = x.ReportingStaffLastName,
                EstimatedCost = x.EstimatedCost,
                MaintenanceRequestId = x.MaintenanceRequestId,
                Priority = x.Priority,
                ReportedDate = x.ReportedDate,
                Title = x.Title,


            }).ToList();
        }
        public async Task<IEnumerable<AvailableTripsDTO>> AvailableTripsForParent(string ParentID)
        {
            var result = await _contextProcedures.usp_GetAvailableTripsForParentBookingAsync(ParentID);
            return result.Select(x => new AvailableTripsDTO
            {
                TripId = x.TripId,
                TripName = x.TripName,
                Direction = x.Direction,
                DepartureTime = x.DepartureTime,
                EstimatedArrivalTime = x.EstimatedArrivalTime,
                RouteId = x.RouteId,
                BusId = x.BusId,
                MaxCapacity = x.MaxCapacity,
                Notes = x.Notes,
                StudentID = x.StudentID,
                FirstName = x.FirstName,
                LastName = x.LastName,
                LevelName = x.LevelName,
                isDaySchool = x.isDaySchool,
                RecurringDays = x.RecurringDays,
                isRecurring = x.isRecurring,
                TripDate = x.TripDate ?? DateTime.Today,
            }).ToList();

        }
        public async Task<IEnumerable<TripAttendance>> GetStudentTripAttendanceByParent(string ParentID)
        {
            var result = await _contextProcedures.usp_GetParentTripAttendancesAsync(ParentID);
            return result.Select(x => new TripAttendance
            {
                AcademicLevel = x.LevelName,
                AttendanceDate = x.AttendanceDate,
                TripAttendanceId = x.TripAttendanceId,
                CreatedDate = x.CreatedDate,
                ProfilePic = x.ProfilePic,
                Status = x.Status,
                StudentId = x.StudentId,
                StudentName = x.FirstName + " " + x.LastName,
                TripId = x.TripId,
                OffboardStopId = x.OffboardStopId,
                BusId = x.BusId,
                DepartureTime = x.DepartureTime,
                EstimatedArrivalTime = x.EstimatedArrivalTime,
                OffboardedByUserId = x.OffboardedByUserId,
                OffboardLatitude = x.OffboardLatitude,
                OffboardLongitude = x.OffboardLongitude,
                OffboardTime = x.OffboardTime,
                OnboardTime = x.OnboardTime,
                RouteId = x.RouteId,
                TripDate = x.TripDate ?? DateTime.Today,
                TripName = x.TripName

            }).ToList();
        }
        public async Task<IEnumerable<TripAttendance>> TripAttendancesAsync(Guid TripID)
        {
            var result = await _contextProcedures.usp_GetTripAttendancesByTripAsync(TripID);
            return result.Select(x => new TripAttendance
            {
                AcademicLevel = x.AcademicLevel,
                AttendanceDate = x.AttendanceDate,
                TripAttendanceId = x.TripAttendanceId,
                CreatedDate = x.CreatedDate,
                ProfilePic = x.ProfilePic,
                Status = x.Status,
                StudentId = x.StudentId,
                StudentName = x.FirstName + " " + x.LastName,
                TripId = x.TripId
            }).ToList();

        }
        public async Task<IEnumerable<Staff>> GetStaffBySchoolAndRole(Guid SchoolID, string RoleName)
        {
            var result = await _contextProcedures.usp_GetStaffBySchoolAndRoleAsync(SchoolID, RoleName);
            return result.Select(x => new Staff
            {
                StaffID = x.StaffID,
                SchoolID = x.SchoolID,
                StaffName = x.StaffName,
                DeanForGender = x.DeanForGender,
                StaffType = x.StaffType,
                isDean = x.isDean,
                UserID = x.UserID,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,

            }).ToList();
        }
        public async Task<IEnumerable<BusStaff>> GetBusStaffAsync(Guid SchoolID)
        {
            return await _context.BusStaffs
                .Where(x => x.SchoolId == SchoolID)
                .ToListAsync();
        }
        public async Task<IEnumerable<Bus>> GetSchoolBusFleetAsync(Guid SchoolID)
        {
            return await _context.Buses
                .Where(x => x.SchoolId == SchoolID)
                .ToListAsync();
        }
        public async Task<IEnumerable<BusRouteDto>> GetSchoolBusRoutes(Guid schoolId)
        {
            return await _context.BusRoutes
                .Where(r => r.SchoolId == schoolId)
                .Select(r => new BusRouteDto
                {
                    Id = r.RouteId,
                    Name = r.Name,
                    BusStops = r.BusStops
                        .OrderBy(s => s.Sequence)
                        .Select(s => new BusStopDto
                        {
                            Id = s.StopId,
                            Name = s.Name,
                            Order = s.Sequence,
                        })
                        .ToList()
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<Trip>> GetSchoolTrips(Guid SchoolId)
        {
            return await _context.Trips.Where(t => t.SchoolId == SchoolId).ToListAsync();
        }

        public async Task<IEnumerable<Trip>> GetDriverTripsForToday(string UserID)
        {
            var result = await _contextProcedures.GetUserTripsTodayAsync(UserID);
            return result.Select(x => new Trip
            {
                TripId = x.TripId,
                BusId = x.BusId,
                RouteId = x.RouteId,
                SchoolId = x.SchoolId,
                TripName = x.TripName,
                Direction = x.Direction,
                DepartureTime = x.DepartureTime,
                EstimatedArrivalTime = x.EstimatedArrivalTime,
                MaxCapacity = x.MaxCapacity,
                IsActive = x.IsActive,
                CreatedByStaffId = x.CreatedByStaffId,
                CreatedDate = x.CreatedDate,
                Notes = x.Notes,
                RecurringDays = x.RecurringDays,
                DriverID = x.DriverID,
                AttendantID = x.AttendantID,
                TripDate = x.TripDate,
                isRecurring = x.isRecurring,
                AcademicLevel = x.AcademicLevel,
                DaySchoolOnly = x.DaySchoolOnly,
                SchoolWide = x.SchoolWide,
                RequresParentBookIng = x.RequresParentBookIng
            }).ToList();
        }
        #endregion

        #region Parent Info
        public async Task<IEnumerable<Parent>> GetParentByUserID(string id)
        {
            var result = await _context.Parents.Where(x => x.UserId == id).ToListAsync();

            return result;
        }
        #endregion
    }

}

