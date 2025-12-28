using EduSphereDomain.Repositories;
using EDUSphereSharedProject.AchievementModels;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.ServerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IgnisEducationSuite.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DynamicController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EduSphereRepository _repository;
        private readonly LicenseService _licenseService;
        public DynamicController(IServiceProvider serviceProvider, EduSphereRepository repository, LicenseService licenseService)
        {
            _serviceProvider = serviceProvider;
            _repository = repository;
            _licenseService = licenseService;
        }
        private IGenericRepository<T> GetRepository<T>() where T : class
        {
            return (IGenericRepository<T>)_serviceProvider.GetService(typeof(IGenericRepository<T>));
        }

        #region Non Generic  Old Modules

        public class StudentNumberLookupRequest
        {
            public Guid SchoolID { get; set; }
            public List<string> StudentNumbers { get; set; } = new();
        }

        [HttpGet("GetFullSchoolAcademicStructure/{SchoolID}")]
        public async Task<IActionResult> GetSchoolStructure(Guid SchoolID)
        {
            var result = await _repository.GetFullAcademicStructureForSchool(SchoolID);
            return Ok(result);
        }

        [HttpPost("GetStudentsByStudentNumbers")]
        public async Task<IActionResult> GetStudentsByStudentNumbers(
            [FromBody] StudentNumberLookupRequest request)
        {
            var students = await _repository
                .GetStudentsByStudentNumbers(request.StudentNumbers, request.SchoolID);

            return Ok(students);
        }

        [HttpGet("generateUsername")]
        public async Task<IActionResult> GenerateUsername([FromQuery] string firstName, [FromQuery] string lastName)
        {
            try
            {
                var username = await _repository.GenerateNextUsernameAsync(firstName, lastName);
                return Ok(username);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("generateUsernames")]
        public async Task<IActionResult> GenerateUsernames([FromBody] List<UserNameRequest> requests)
        {
            if (requests == null || !requests.Any())
                return BadRequest("No names provided.");

            var nameTuples = requests
                .Select(x => (x.FirstName, x.LastName))
                .ToList();

            var usernames = await _repository.GenerateNextUsernamesAsync(nameTuples);

            // Keep index alignment with incoming list
            var response = usernames
                .Select((username, index) => new UsernameResult
                {
                    Index = index,
                    Username = username
                })
                .ToList();

            return Ok(response);
        }

        [HttpGet("GetInitializationData/{ID}")]
        public async Task<IActionResult> GetInitializationData(string ID)
        {
            var result = await _repository.GetInitializationDataResults(ID);
            return Ok(result);
        }

        [HttpGet("GetLessonMedia/{ID}")]
        public async Task<IActionResult> GetLessonMedia(Guid ID)
        {
            var result = await _repository.GetLessonMedia(ID);
            return Ok(result);
        }
        [HttpGet("GetQuestionChoicesAssignment/{ID}")]
        public async Task<IActionResult> GetMultipleChoiceAssignment(Guid ID)
        {
            var result = await _repository.GetAssingmentMultipleChoices(ID);
            return Ok(result);
        }
        [HttpGet("GetQuestionChoicesExams/{ID}")]
        public async Task<IActionResult> GetMultipleChoiceExams(Guid ID)
        {
            var result = await _repository.GetExamMultipleChoices(ID);
            return Ok(result);
        }
        [HttpGet("GetTeacherCourses/{ID}")]
        public async Task<IActionResult> GetTeacherCourses(Guid ID)
        {
            var result = await _repository.GetTeacherCourses(ID);
            return Ok(result);
        }
        [HttpGet("GetStudentDashboardState/{SchoolID}")]
        public async Task<IActionResult> GetStudentDashBoardState(string SchoolID)
        {
            var result = await _repository.GetStudentDashboardState(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetBestPerfomingStudentsBySchool/{SchoolID}")]
        public async Task<IActionResult> GetBestPerfomingStudentsBySchool(string SchoolID)
        {
            var result = await _repository.GetBestPerformingStudents(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetSystemActivities")]
        public async Task<IActionResult> GetActivities()
        {
            var result = await _repository.GetSystemActivities();
            return Ok(result);
        }
        [HttpGet("GetUserBadges/{UserID}")]
        public async Task<IActionResult> GetUserBadges(string UserID)
        {
            var result = await _repository.GetUserBadges(UserID);
            return Ok(result);
        }
        [HttpPost("SaveNewUserBadge")]
        public async Task<IActionResult> SaveNewUserBadge(UserBadge userBadge)
        {
            var result = await _repository.SaveUserBadge(userBadge);
            return Ok(result);
        }
        [HttpPost("SaveNewUserActivity")]
        public async Task<IActionResult> SaveUserActivity(UserActivity userActivity)
        {
            var result = await _repository.SaveUserActivity(userActivity);
            return Ok(result);
        }
        [HttpGet("GetLessonCountBySchool/{SchoolID}")]
        public async Task<IActionResult> GetLessonCountBySchool(string SchoolID)
        {
            var result = await _repository.GetLessonCountBySchool(SchoolID);
            return Ok(result);
        }


        [HttpGet("GetAllSystemBadges")]
        public async Task<IActionResult> GetSystemBadges()
        {
            var result = await _repository.GetSystemBadges();
            return Ok(result);
        }
        [HttpGet("GetAllUserActivities/{UserID}")]
        public async Task<IActionResult> GetUserActivities(string UserID)
        {
            var result = await _repository.GetUserActivities(UserID);
            return Ok(result);
        }
        [HttpGet("GetCourseDetailsByCourseID/{CourseID}")]
        public async Task<IActionResult> GetCourseDetailsByID(Guid CourseID)
        {
            var result = await _repository.GetCourseDetailsByID(CourseID);
            return Ok(result);
        }
        [HttpGet("GetCoursesBySchool/{SchoolID}")]
        public async Task<IActionResult> GetCoursesBySchool(Guid SchoolID)
        {
            var result = await _repository.GetCoursesBySchool(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetTeacherSubjectsByID/{id}")]
        public async Task<IActionResult> GetSubjectsByTeacherID(Guid id)
        {
            var result = await _repository.GetTeacherSubjectsByID(id);
            return Ok(result);
        }
        [HttpGet("GetStudentLessons/{id}")]
        public async Task<IActionResult> GetStudentLessons(string id)
        {
            var result = await _repository.GetstudentLessons(id);
            return Ok(result);
        }
        [HttpGet("GetStudentCompletedLessons/{id}")]
        public async Task<IActionResult> GetStudentCompletedLessons(string id)
        {
            var result = await _repository.GetstudentCompletedLessons(id);
            return Ok(result);
        }
        [HttpGet("GetAssignmentQuestions/{id}")]
        public async Task<IActionResult> GetAssignmentQuestions(Guid id)
        {
            var result = await _repository.GetAssignmentQuestions(id);
            return Ok(result);
        }
        [HttpGet("GetAssignmentAnswers/{id}")]
        public async Task<IActionResult> GetAssignmentAnswers(Guid id)
        {
            var result = await _repository.GetAssignmentAnswers(id);
            return Ok(result);
        }
        [HttpGet("GetAssignment/{id}")]
        public async Task<IActionResult> GetStudentAssignments(string id)
        {
            var result = await _repository.GetStudentAssignments(id);
            return Ok(result);
        }
        [HttpGet("GetTeacherAssignments/{id}")]
        public async Task<IActionResult> GetTeacherAssignments(string id)
        {
            var result = await _repository.GetTeacherAssignments(id);
            return Ok(result);
        }
        [HttpGet("CheckIfStudentIDExists/{id}/{SchoolID}")]
        public async Task<IActionResult> CheckIfStudentIDisTake(string id, string SchoolID)
        {
            var result = await _repository.CheckIfStudentNumberIsTaken(id, SchoolID);
            return Ok(result);
        }
        [HttpGet("GetStudentClassInfo/{id}")]
        public async Task<IActionResult> GetStudentClassinformation(Guid id)
        {
            var result = await _repository.GetStudentClassInfo(id);
            return Ok(result);
        }
        [HttpGet("GetStudentsInGrade/{grade}/{SchoolID}")]
        public async Task<IActionResult> GetStudentsInGrade(int grade, string SchoolID)
        {
            var result = await _repository.GetStudentsInGrade(grade, SchoolID);
            return Ok(result);
        }

        [HttpGet("GetStudentsOfClassDetail/{grade}/{SchoolID}")]
        public async Task<IActionResult> GetStudentsInGradeDetailed(int grade, string SchoolID)
        {
            var result = await _repository.GetStudentsByGrade(grade, SchoolID);
            return Ok(result);
        }
        [HttpGet("GetStudentsInGradeWithoutSchedule/{grade}/{schoolID}/{ClassSection}")]
        public async Task<IActionResult> GetStudentsInGradeWithoutSchedule(int grade, string schoolID, string ClassSection)
        {
            var result = await _repository.GetStudentsInGradeWithoutSchedule(grade, schoolID, ClassSection);
            return Ok(result);
        }
        [HttpGet("GetActiveScheduleByGrade/{grade}/{ClassSection}/{SchoolID}")]
        public async Task<IActionResult> GetActiveScheduleByGrade(int grade, string ClassSection, string SchoolID)
        {
            var result = await _repository.GetActiveClassSchedules(grade, ClassSection, SchoolID);
            return Ok(result);
        }

        [HttpGet("GetStudentPerfomanceData/{StudentID}")]
        public async Task<IActionResult> GetStudentPerfomanceData(string StudentID)
        {
            var result = await _repository.StudentYearlyPerfomance(StudentID);
            return Ok(result);
        }
        [HttpGet("GetAttendanceTrends/{StudentID}")]
        public async Task<IActionResult> GetAttendanceTrends(string StudentID)
        {
            var result = await _repository.GetAttendanceTrendForPastSeven(StudentID);
            return Ok(result);
        }
        [HttpGet("GetMissedClasses/{StudentID}")]
        public async Task<IActionResult> GetMissedClasses(string StudentID)
        {
            var result = await _repository.GetMissedClassesForPastWeekResults(StudentID);
            return Ok(result);
        }
        [HttpGet("GetStudentPerfomanceDataParent/{StudentID}")]
        public async Task<IActionResult> GetStudentPerfomanceDataParent(string StudentID)
        {
            var result = await _repository.StudentYearlyPerfomanceParent(StudentID);
            return Ok(result);
        }
        [HttpGet("GetStudentPendingClasses/{StudentID}")]
        public async Task<IActionResult> GetStudentPendingClasses(string StudentID)
        {
            var result = await _repository.GetStudentUnCompletedLessons(StudentID);
            return Ok(result);
        }
        [HttpGet("GetActiveStudentTimeTable/{Student}")]
        public async Task<IActionResult> GetActiveStudentTimeTable(string Student)
        {
            var result = await _repository.GetStudentClasses(Student);
            return Ok(result);
        }
        [HttpGet("GetStudentAttendance/{UserID}/{date}")]
        public async Task<IActionResult> GetActiveStudentAttendanceSheets(string UserID, DateTime date)
        {
            var result = await _repository.GetAttendances(UserID, date);
            return Ok(result);
        }
        [HttpGet("GetStudentAttendances/{StudentID}")]
        public async Task<IActionResult> GetStudentAttendance(string StudentID)
        {
            var result = await _repository.StudentAttendances(StudentID);
            return Ok(result);
        }
        [HttpGet("GetStudentDemoCountry/{SchoolID}")]
        public async Task<IActionResult> GetStudentCountryOriginCount(string SchoolID)
        {
            var result = await _repository.GetStudentDemographicsCountries(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetStudentDemoCity/{SchoolID}")]
        public async Task<IActionResult> GetStudentCityOriginCount(string SchoolID)
        {
            var result = await _repository.GetStudentDemographicsCities(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetstudentReportCards/{userID}")]
        public async Task<IActionResult> GetstudentReportCards(string userID)
        {
            var result = await _repository.GetReportCardsByStudents(userID);
            return Ok(result);
        }
        [HttpGet("GetstudentReportCardsParent/{userID}")]
        public async Task<IActionResult> GetstudentReportCardsParent(string userID)
        {
            var result = await _repository.GetReportCardsByParent(userID);
            return Ok(result);
        }
        [HttpGet("GetReportCardAttendanceDate/{userID}/{StartDate}/{EndDate}")]
        public async Task<IActionResult> GetstudentReportCardAttendanceData(Guid userID, DateTime StartDate, DateTime EndDate)
        {
            var result = await _repository.GetAttendanceSummaryResults(userID, StartDate, EndDate);
            return Ok(result);
        }
        [HttpGet("GetReportCardDetailsByReportCardID/{ReportCardID}")]
        public async Task<IActionResult> GetReportCardDetailsByID(Guid ReportCardID)
        {
            var result = await _repository.GetReportCardDetailsResults(ReportCardID);
            return Ok(result);
        }
        [HttpGet("GetUpcomingExamsAndQuizzes/{GradeLevel}")]
        public async Task<IActionResult> GetUpcomingExams(int GradeLevel)
        {
            var result = await _repository.GetUpcomingExamsOrQuizzesResults(GradeLevel);
            return Ok(result);
        }
        [HttpGet("GetExamTestQuizQuestions/{ExamID}")]
        public async Task<IActionResult> GetExamQuestions(Guid ExamID)
        {
            var result = await _repository.ExamQuizTestQuestions(ExamID);
            return Ok(result);
        }
        [HttpGet("GetStudentExamDetails/{UserID}")]
        public async Task<IActionResult> GetStudentExamsByTeacherID(string UserID)
        {
            var data = await _repository.GetStudentExamDetails(UserID);
            return Ok(data);
        }
        [HttpGet("GetStudentExamAnswers/{HeaderID}")]
        public async Task<IActionResult> GetStudentExamAnswers(Guid HeaderID)
        {
            var data = await _repository.ExamQuizTestAnswers(HeaderID);
            return Ok(data);
        }
        [HttpGet("GetAdminByUserID/{UserID}")]
        public async Task<IActionResult> GetAdminDataByUserID(string UserID)
        {
            var data = await _repository.GetClientAdmins(UserID);
            return Ok(data);
        }
        [HttpGet("GetStudentsBySchoolID/{SchoolID}")]
        public async Task<IActionResult> GetStudentsBySchool(Guid SchoolID)
        {
            var result = await _repository.GetStudentsBySchool(SchoolID);
            return Ok(result);
        }

        [HttpGet("GetTeachersBySchoolID/{SchoolID}")]
        public async Task<IActionResult> GetTeachersBySchool(Guid SchoolID)
        {
            var result = await _repository.GetTeachersBySchool(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetClassesBySchoolID/{SchoolID}")]
        public async Task<IActionResult> GetClassesBySchool(Guid SchoolID)
        {
            var result = await _repository.GetClassesBySchool(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetTeacherByUserID/{UserID}")]
        public async Task<IActionResult> GetTeachersByUserID(string UserID)
        {
            var result = await _repository.GetTeachersByUser(UserID);
            return Ok(result);
        }
        [HttpGet("GetLessonsBySchool/{SchoolID}")]
        public async Task<IActionResult> GetLessonsBySchoolID(string SchoolID)
        {
            var result = await _repository.GetLessonsBySchool(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetStudentGrowthBySchool/{SchoolID}")]
        public async Task<IActionResult> GetStudentGrowthBySchoolID(string SchoolID)
        {
            var result = await _repository.GetStudentGrowth(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetLessonSummaryBySchool/{SchoolID}")]
        public async Task<IActionResult> GetHighestSchoolRatedLessons(string SchoolID)
        {
            var result = await _repository.GetHighestRatedClasses(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetTopTeachers/{SchoolID}")]
        public async Task<IActionResult> GetTopRatedTeachers(string SchoolID)
        {
            var result = await _repository.GetTop5TeachersByHighRatings(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetTeacherLessons/{TeacherID}")]
        public async Task<IActionResult> GetTeacherLessons(Guid TeacherID)
        {
            var result = await _repository.GetTeacherLessonsAsync(TeacherID);
            return Ok(result);
        }
        #endregion

        #region Generic Endpoints

        [HttpGet("GetEntity/{entity}")]
        public async Task<IActionResult> GetAll(string entity)
        {
            var repository = GetRepositoryFromEntityName(entity);
            if (repository == null)
            {
                return BadRequest($"Unknown entity: {entity}");
            }

            var result = await repository.GetAllAsync();
            return Ok(result);
        }

        // GET: api/{entity}/{id}
        [HttpGet("GetEntityById/{entity}/{id}")]
        public async Task<IActionResult> Get(string entity, Guid id)
        {
            var repository = GetRepositoryFromEntityName(entity);
            if (repository == null)
            {
                return BadRequest($"Unknown entity: {entity}");
            }

            var result = await repository.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        // POST: api/{entity}
        [HttpPost("PostEntity")]
        public async Task<IActionResult> Post(string entity, [FromBody] object obj)
        {
            try
            {
                var repository = GetRepositoryFromEntityName(entity);
                if (repository == null)
                {
                    return BadRequest($"Unknown entity: {entity}");
                }

                var entityObj = ConvertToEntityType(entity, obj);
                await repository.AddAsync(entityObj);
                return Ok(entityObj);
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                throw;
            }

        }
        [HttpPost("PostEntities")]
        public async Task<IActionResult> PostEntities(string entity, [FromBody] List<object> objList)
        {
            try
            {
                var repository = GetRepositoryFromEntityName(entity);
                if (repository == null)
                {
                    return BadRequest($"Unknown entity: {entity}");
                }

                var entityList = objList.Select(obj => ConvertToEntityType(entity, obj)).ToList();
                foreach (var e in entityList)
                {
                    await repository.AddAsync(e);
                }
                return Ok(entityList);
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                throw;
            }
        }

        // PUT: api/{entity}/{id}
        [HttpPut("UpdateEntity")]
        public async Task<IActionResult> Put(string entity, [FromBody] object obj)
        {

            var repository = GetRepositoryFromEntityName(entity);
            if (repository == null)
            {
                return BadRequest($"Unknown entity: {entity}");
            }

            var entityObj = ConvertToEntityType(entity, obj);
            await repository.UpdateAsync(entityObj);
            return Ok(entityObj);
        }

        // PUT: api/{entity}/{id}
        [HttpPut("UpdateEntities")]
        public async Task<IActionResult> Update(string entity, [FromBody] List<object> objList)
        {

            var repository = GetRepositoryFromEntityName(entity);
            if (repository == null)
            {
                return BadRequest($"Unknown entity: {entity}");
            }

            var entityObjList = objList.Select(obj => ConvertToEntityType(entity, obj));
            foreach (var e in entityObjList)
            {
                await repository.UpdateAsync(e);
            }
            return Ok(entityObjList);
        }
        // DELETE: api/{entity}/{id}
        [HttpDelete("DeleteEntity/{entity}/{id}")]
        public async Task<IActionResult> Delete(string entity, Guid id)
        {
            var repository = GetRepositoryFromEntityName(entity);
            if (repository == null)
            {
                return BadRequest($"Unknown entity: {entity}");
            }

            await repository.DeleteAsync(id);
            return NoContent();
        }

        private dynamic GetRepositoryFromEntityName(string entity)
        {
            // Map the entity name to the actual repository type
#pragma warning disable CS8603 // Possible null reference return.
            return entity.ToLower() switch
            {
                "genders" => GetRepository<Gender>(),
                "student" => GetRepository<Student>(),
                "studentclasses" => GetRepository<StudentClass>(),
                "parent" => GetRepository<Parent>(),
                "teacher" => GetRepository<Teacher>(),
                "subjects" => GetRepository<Subject>(),
                "teachersubjectnormalized" => GetRepository<TeacherSubjectNormalized>(),
                "class" => GetRepository<Class>(),
                "lesson" => GetRepository<Lesson>(),
                "studentcompletedlesson" => GetRepository<StudentCompletedLesson>(),
                "assignment" => GetRepository<Assignment>(),
                "assignmentquestions" => GetRepository<AssignmentQuestion>(), //assignmentQuestions
                "studentassignment" => GetRepository<StudentAssignment>(),
                "studentassignmentanswers" => GetRepository<StudentAssignmentAnswer>(),
                "studentgrowth" => GetRepository<vw_StudentGrowth>(),
                "highratedclasses" => GetRepository<vw_ClassLessonSummary>(),
                "timeslot" => GetRepository<TimeSlot>(),
                "days" => GetRepository<DayofTheWeek>(),
                "classschedule" => GetRepository<ClassSchedule>(),
                "studentclassschedule" => GetRepository<StudentClassSchedule>(),
                "studentattendance" => GetRepository<StudentAttendance>(),
                "reportcard" => GetRepository<ReportCard>(),
                "reportcarddetail" => GetRepository<ReportCardDetail>(),
                "gradingscale" => GetRepository<GradingScale>(),
                "classteacherdetails" => GetRepository<vw_ClassTeacherDetail>(),
                "examquiztestheader" => GetRepository<ExamQuizTestHeader>(),
                "examquestion" => GetRepository<ExamQuizTestQuestion>(),
                "examanswers" => GetRepository<StudentExamQuizAndTestAnswer>(),
                "studentexamsheader" => GetRepository<StudentExamsTestsAndQuiz>(),
                "school" => GetRepository<School>(),
                "clientadmin" => GetRepository<ClientAdmin>(),
                "course" => GetRepository<Course>(),
                "coursedetail" => GetRepository<CourseDetail>(),
                "assignmentmultiplechoices" => GetRepository<MultipleChoiceAssignmentAnswer>(),
                "exammultiplechoices" => GetRepository<ExamTestQuizMultipleChoiceAnswer>(),
                "lessonmedia" => GetRepository<LessonMedium>(),
                "livemeeting" => GetRepository<LiveMeeting>(),
                "termsetting" => GetRepository<TermSetting>(),
                "hostel" => GetRepository<Hostel>(),
                "room" => GetRepository<Room>(),
                "roomallocation" => GetRepository<RoomAllocation>(),
                "maintainancerequest" => GetRepository<MaintainanceRequest>(),
                "dininghall" => GetRepository<DiningHall>(),
                "meal" => GetRepository<Meal>(),
                "menuitem" => GetRepository<DiningMenu>(),
                "specialdiet" => GetRepository<DiningSpecialDiet>(),
                "academiclevel" => GetRepository<AcademicLevel>(),
                "meds" => GetRepository<ClinicMedication>(),
                "clinic" => GetRepository<Clinic>(),
                "staff" => GetRepository<Staff>(),
                // Add more entities here as needed
                _ => null
            };
#pragma warning restore CS8603 // Possible null reference return.
        }

        private dynamic ConvertToEntityType(string entity, object obj)
        {
            // Dynamically convert the object to the right entity type
#pragma warning disable CS8603 // Possible null reference return.
            return entity.ToLower() switch
            {
                "genders" => JsonSerializer.Deserialize<Gender>(obj.ToString()),
                "student" => JsonSerializer.Deserialize<Student>(obj.ToString()),
                "studentclasses" => JsonSerializer.Deserialize<StudentClass>(obj.ToString()),
                "parent" => JsonSerializer.Deserialize<Parent>(obj.ToString()),
                "teacher" => JsonSerializer.Deserialize<Teacher>(obj.ToString()),
                "subjects" => JsonSerializer.Deserialize<Subject>(obj.ToString()),
                "teachersubjectnormalized" => JsonSerializer.Deserialize<TeacherSubjectNormalized>(obj.ToString()),
                "class" => JsonSerializer.Deserialize<Class>(obj.ToString()),
                "lesson" => JsonSerializer.Deserialize<Lesson>(obj.ToString()),
                "studentcompletedlesson" => JsonSerializer.Deserialize<StudentCompletedLesson>(obj.ToString()),
                "assignment" => JsonSerializer.Deserialize<Assignment>(obj.ToString()),
                "assignmentquestions" => JsonSerializer.Deserialize<AssignmentQuestion>(obj.ToString()),
                "studentassignment" => JsonSerializer.Deserialize<StudentAssignment>(obj.ToString()),
                "studentassignmentanswers" => JsonSerializer.Deserialize<StudentAssignmentAnswer>(obj.ToString()),
                "studentgrowth" => JsonSerializer.Deserialize<vw_StudentGrowth>(obj.ToString()),
                "highratedclasses" => JsonSerializer.Deserialize<vw_ClassLessonSummary>(obj.ToString()),
                "timeslot" => JsonSerializer.Deserialize<TimeSlot>(obj.ToString()),
                "days" => JsonSerializer.Deserialize<DayofTheWeek>(obj.ToString()),
                "classschedule" => JsonSerializer.Deserialize<ClassSchedule>(obj.ToString()),
                "studentclassschedule" => JsonSerializer.Deserialize<StudentClassSchedule>(obj.ToString()),
                "studentattendance" => JsonSerializer.Deserialize<StudentAttendance>(obj.ToString()),
                "reportcard" => JsonSerializer.Deserialize<ReportCard>(obj.ToString()),
                "reportcarddetail" => JsonSerializer.Deserialize<ReportCardDetail>(obj.ToString()),
                "gradingscale" => JsonSerializer.Deserialize<GradingScale>(obj.ToString()),
                "classteacherdetails" => JsonSerializer.Deserialize<vw_ClassTeacherDetail>(obj.ToString()),
                "examquiztestheader" => JsonSerializer.Deserialize<ExamQuizTestHeader>(obj.ToString()),
                "examquestion" => JsonSerializer.Deserialize<ExamQuizTestQuestion>(obj.ToString()),
                "examanswers" => JsonSerializer.Deserialize<StudentExamQuizAndTestAnswer>(obj.ToString()),
                "studentexamsheader" => JsonSerializer.Deserialize<StudentExamsTestsAndQuiz>(obj.ToString()),
                "school" => JsonSerializer.Deserialize<School>(obj.ToString()),
                "clientadmin" => JsonSerializer.Deserialize<ClientAdmin>(obj.ToString()),
                "course" => JsonSerializer.Deserialize<Course>(obj.ToString()),
                "coursedetail" => JsonSerializer.Deserialize<CourseDetail>(obj.ToString()),
                "assignmentmultiplechoices" => JsonSerializer.Deserialize<MultipleChoiceAssignmentAnswer>(obj.ToString()),
                "exammultiplechoices" => JsonSerializer.Deserialize<ExamTestQuizMultipleChoiceAnswer>(obj.ToString()),
                "lessonmedia" => JsonSerializer.Deserialize<LessonMedium>(obj.ToString()),
                "livemeeting" => JsonSerializer.Deserialize<LiveMeeting>(obj.ToString()),
                "termsetting" => JsonSerializer.Deserialize<TermSetting>(obj.ToString()),
                "hostel" => JsonSerializer.Deserialize<Hostel>(obj.ToString()),
                "room" => JsonSerializer.Deserialize<Room>(obj.ToString()),
                "roomallocation" => JsonSerializer.Deserialize<RoomAllocation>(obj.ToString()),
                "maintainancerequest" => JsonSerializer.Deserialize<MaintainanceRequest>(obj.ToString()),
                "dininghall" => JsonSerializer.Deserialize<DiningHall>(obj.ToString()),
                "meal" => JsonSerializer.Deserialize<Meal>(obj.ToString()),
                "menuitem" => JsonSerializer.Deserialize<DiningMenu>(obj.ToString()),
                "specialdiet" => JsonSerializer.Deserialize<DiningSpecialDiet>(obj.ToString()),
                "academiclevel" => JsonSerializer.Deserialize<AcademicLevel>(obj.ToString()),
                "meds" => JsonSerializer.Deserialize<ClinicMedication>(obj.ToString()),
                "clinic" => JsonSerializer.Deserialize<Clinic>(obj.ToString()),
                "staff" => JsonSerializer.Deserialize<Staff>(obj.ToString()),

                // Add more entity conversions here as needed
                _ => null
            };
#pragma warning restore CS8603 // Possible null reference return.
        }

        #endregion

        #region Non Generic New Modules
        [HttpGet("GetStaffBySchoolAndRole/{SchoolID}/{RoleName}")]
        public async Task<IActionResult> GetStaff(Guid SchoolID, string RoleName)
        {
            var result = await _repository.GetStaffBySchoolAndRole(SchoolID, RoleName);
            return Ok(result);
        }
        [HttpGet("GetLicenseStatus/{companyId}")]
        public async Task<IActionResult> GetLicenseByCompany(Guid companyId)
        {
            var result = await _licenseService.GetCompanyLicense(companyId);
            return Ok(result);
        }
        [HttpGet("GetMeetingData/{MeetingID}")]
        public async Task<IActionResult> GetMeetingData(string MeetingID)
        {
            var result = await _repository.GetMeetingsAsync(MeetingID);
            return Ok(result);
        }
        [HttpGet("GetSchoolAcademicStructure/{schoolID}")]
        public async Task<IActionResult> GetAcademicLevel(string schoolID)
        {
            var result = await _repository.GetAcademicLevelsAsync(schoolID);
            return Ok(result);
        }
        [HttpGet("GetStudentGradedExams/{StudentID}")]
        public async Task<IActionResult> GetStudentGradedExams(string StudentID)
        {
            var result = await _repository.GetGradedExamsByStudent(StudentID);
            return Ok(result);
        }
        [HttpGet("GetStudentGradedAssignments/{StudentID}")]
        public async Task<IActionResult> GetStudentGradedAssignments(string StudentID)
        {
            var result = await _repository.GetGradedAssignmentsByStudent(StudentID);
            return Ok(result);
        }

        [HttpGet("GetStudentsByUser/{UserID}")]
        public async Task<IActionResult> GetStudentByUserID(string UserID)
        {
            var result = await _repository.GetStudentByUserID(UserID);
            return Ok(result);
        }
        [HttpGet("GetStudentsBySchool/{SchoolID}")]
        public async Task<IActionResult> GetStudentsBySchool(string SchoolID)
        {
            var result = await _repository.GetStudentsBySchool(SchoolID);
            return Ok(result);
        }

        [HttpGet("GetTermSettings/{SchoolID}")]
        public async Task<IActionResult> GetTermSetting(Guid SchoolID)
        {
            var result = await _repository.GetCurrentActiveTermsAsync(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetTeachersBySchool/{SchoolID}")]
        public async Task<IActionResult> GetTeachersBySchool(string SchoolID)
        {
            var result = await _repository.GetTeachersBySchool(Guid.Parse(SchoolID));
            return Ok(result);
        }
        [HttpGet("GetClassesBySchool/{SchoolID}")]
        public async Task<IActionResult> GetClassesBySchool(string SchoolID)
        {
            var result = await _repository.GetClassesBySchool(Guid.Parse(SchoolID));
            return Ok(result);
        }
        [HttpGet("GetGradingScaleBySchool/{SchoolID}")]
        public async Task<IActionResult> GetGradingScaleBySchool(string SchoolID)
        {
            var result = await _repository.GetGradingScalesBySchoolAsync(Guid.Parse(SchoolID));
            return Ok(result);
        }
        [HttpGet("GetStudentsAndStudentClasses/{SchoolID}")]
        public async Task<IActionResult> GetStudentsAndStudentClasses(string SchoolID)
        {
            var result = await _repository.GetStudentsWithClassesBySchoolAsync(Guid.Parse(SchoolID));
            return Ok(result);
        }

        [HttpGet("GetStudentReportCardHeader/{StudentID}")]
        public async Task<IActionResult> GetStudentReportCardHeader(string StudentID)
        {
            var result = await _repository.GetReportCardHeaderByStudent(Guid.Parse(StudentID));
            return Ok(result);
        }

        [HttpGet("GetHostelsBySchool/{SchoolID}")]
        public async Task<IActionResult> GetHostelBySchool(string SchoolID)
        {

            var result = await _repository.GetHostelsBySchool(Guid.Parse(SchoolID));
            return Ok(result);
        }
        [HttpGet("GetRoomsBySchool/{SchoolID}")]
        public async Task<IActionResult> GetRoomsBySchools(string SchoolID)
        {
            var result = await _repository.GetRoomsBySchools(Guid.Parse(SchoolID));
            return Ok(result);
        }

        [HttpGet("GetStudentsInRoom/{RoomID}")]
        public async Task<IActionResult> GetStudentsInRoom(Guid RoomID)
        {
            var result = await _repository.StudentsInRoom(RoomID);
            return Ok(result);
        }
        [HttpGet("GetStudentsWithoutRoomByGender/{GenderID}/{SchoolID}")]
        public async Task<IActionResult> GetStudentsWithoutRooms(Guid GenderID, string SchoolID)
        {
            var result = await _repository.StudentsWithoutRooms(GenderID, Guid.Parse(SchoolID));
            return Ok(result);

        }
        [HttpGet("GetHostelMaintainanceRequests/{SchoolID}")]
        public async Task<IActionResult> GetHostelMaintainanceRequests(string SchoolID)
        {
            var result = await _repository.GetMaintainanceRequests(Guid.Parse(SchoolID));
            return Ok(result);
        }
        #region DINING MANAGMENT
        [HttpGet("GetSchoolDiningHalls/{SchoolID}")]
        public async Task<IActionResult> GetSchoolHalls(Guid SchoolID)
        {
            var result = await _repository.GetDiningHallsBySchoolAsync(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetSchoolMealSession/{SchoolID}")]
        public async Task<IActionResult> GetMealSessions(Guid SchoolID)
        {
            var result = await _repository.GetSchoolMealSessions(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetSchoolDiningMenus/{SchoolID}")]
        public async Task<IActionResult> GetSchoolMenus(Guid SchoolID)
        {
            var result = await _repository.GetDiningMenus(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetSpecialDiets/{SchoolID}")]
        public async Task<IActionResult> GetStudentsWithSpecialDiets(Guid SchoolID)
        {
            var result = await _repository.GetSpecialDiets(SchoolID);
            return Ok(result);
        }
        [HttpGet("GetSchoolActivities/{SchoolID}")]
        public async Task<IActionResult> GetSchoolActivities(string SchoolID)
        {
            var result = await _repository.GetSchoolActivities(Guid.Parse(SchoolID));
            return Ok(result);
        }



        #endregion

        #region Clinic End Points
        [HttpGet("GetSchoolClinics/{SchoolID}")]
        public async Task<IActionResult> GetClinicsBySchool(string SchoolID)
        {
            var res = await _repository.GetSchoolClinics(Guid.Parse(SchoolID));
            return Ok(res);
        }

        [HttpGet("GetSchoolMedicationStocks/{SchoolID}")]
        public async Task<IActionResult> GetSchoolMedications(string SchoolID)
        {
            var result = await _repository.GetMedicationsBySchoolAsync(Guid.Parse(SchoolID));
            return Ok(result);
        }
        #endregion

        #region Transport Management
        [HttpGet("GetTransportStaffBySchool/{SchoolID}")]
        public async Task<IActionResult> GetBusStaffAsync(Guid SchoolID)
        {
            var result = await _repository.GetBusStaffAsync(SchoolID);
            return Ok(result);
        }
        #endregion

        #endregion
    }
}
