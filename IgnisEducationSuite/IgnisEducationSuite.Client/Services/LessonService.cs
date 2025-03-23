using EDUSphereSharedProject.Models;

namespace IgnisEducationSuite.Client.Services
{
    public class LessonService
    {
        public Lesson CurrentLesson { get; set; }
        public Course Course { get; set; }
        public List<AssignmentQuestion> assignmentQuestions { get; set; }
        public List<ExamQuizTestQuestion> examQuizTestQuestions { get; set; }
        public List<StudentAssignmentAnswer> studentAssignmentAnswers { get; set; }
        public List<StudentExamQuizAndTestAnswer> studentExamAnswers { get; set; }
        public List<CourseDetail> courseDetails { get; set; }
        public Guid studentID { get; set; }
        public Guid teacherID { get; set; }
        public Guid parentID { get; set; }
        public Guid assignmentID { get; set; }
        public Guid ExamTestQuizID { get; set; }
        public string BookID { get; set; }  
        public bool isGoogleBook { get; set; }
        public bool LessonCompleted { get; set; }
    }
}
