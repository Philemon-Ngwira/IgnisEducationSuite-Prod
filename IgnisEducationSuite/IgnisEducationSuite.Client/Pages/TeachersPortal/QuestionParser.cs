using EDUSphereSharedProject.Models;
using System.Text.RegularExpressions;

public class QuestionParser
{
    public List<AssignmentQuestion> ParseQuestionsFromResponse(string responseText)
    {
        var questions = new List<AssignmentQuestion>();
        int questionNumber = 1;

        // Regular expression pattern to capture numbered questions (e.g., "1. Question text")
        string pattern = @"^\d+\.\s*(.+)$";
        var matches = Regex.Matches(responseText, pattern, RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                questions.Add(new AssignmentQuestion
                {
                    QuestionNumber = questionNumber++,
                    QuestionText = match.Groups[1].Value.Trim()
                });
            }
        }

        return questions;
    }
     public List<ExamQuizTestQuestion> ParseExamQuestionsFromResponse(string responseText)
    {
        var questions = new List<ExamQuizTestQuestion>();
        int questionNumber = 1;

        // Regular expression pattern to capture numbered questions (e.g., "1. Question text")
        string pattern = @"^\d+\.\s*(.+)$";
        var matches = Regex.Matches(responseText, pattern, RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                questions.Add(new ExamQuizTestQuestion
                {
                    QuestionNumber = questionNumber++,
                    QuestionText = match.Groups[1].Value.Trim()
                });
            }
        }

        return questions;
    }
}