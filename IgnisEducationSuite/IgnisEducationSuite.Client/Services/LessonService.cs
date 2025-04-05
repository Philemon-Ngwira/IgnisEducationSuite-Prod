using EDUSphereSharedProject.Models;

namespace IgnisEducationSuite.Client.Services
{
    public class LessonService
    {
        public Lesson CurrentLesson { get; set; }
        public Course Course { get; set; }
        public List<AssignmentQuestion> assignmentQuestions { get; set; } = new();
        public List<MultipleChoiceAssignmentAnswer> AssignmentAnswerChoices { get; set; } = new();
        public List<ExamTestQuizMultipleChoiceAnswer> ExamAnswerChoices { get; set; } = new();
        public List<ExamQuizTestQuestion> examQuizTestQuestions { get; set; } = new();
        public List<StudentAssignmentAnswer> studentAssignmentAnswers { get; set; } = new();
        public List<StudentExamQuizAndTestAnswer> studentExamAnswers { get; set; } = new();
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
