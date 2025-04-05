using EDUSphereSharedProject.Models;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IgnisEducationSuite.Client.Pages.StudentPortal
{
    public partial class AssignmentDetails : ComponentBase
    {
        protected List<AssignmentQuestion> assignmentQuestions = new();
        protected List<StudentAssignmentAnswer> assignmentAnswers = new();
        [Inject] ChatClientService ChatClientService { get; set; } = default!;
        [Inject] LessonService LessonService { get; set; } = default!;
        [Inject] GenericServiceFactory GenericServiceFactory { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;
        [Inject] NavigationManager _navigationManager { get; set; } = default!;
        protected StudentAssignment studentAssignment = new();
        protected Guid studentID;
        protected bool isLoading = false;
        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            assignmentQuestions = LessonService.assignmentQuestions.OrderBy(x => x.QuestionNumber).ToList();
            studentID = LessonService.studentID;
            isLoading = false;

        }
        private StudentAssignmentAnswer GetOrCreateAnswer(Guid questionId)
        {
            // Look for an existing answer object for this question
            var answer = assignmentAnswers.FirstOrDefault(a => a.QuestionID == questionId);

            // If no answer exists, create a new one and add it to the list
            if (answer == null)
            {
                answer = new StudentAssignmentAnswer { QuestionID = questionId };
                assignmentAnswers.Add(answer);
            }

            return answer;
        }

        private void HandleMultipleChoiceSelection(Guid questionId, string selectedLetter, string AnswerText, bool isChecked)
        {
            if (isChecked)
            {
                // Create a new answer entry for this choice
                var newAnswer = new StudentAssignmentAnswer
                {
                    QuestionID = questionId,
                    AnswerText = $"{selectedLetter} : {AnswerText}" // Store the selected letter as the answer
                };

                assignmentAnswers.Add(newAnswer);
            }
            else
            {
                // Remove the answer if the checkbox is unchecked
                var existingAnswer = assignmentAnswers.FirstOrDefault(a => a.QuestionID == questionId && a.AnswerText == selectedLetter);
                if (existingAnswer != null)
                {
                    assignmentAnswers.Remove(existingAnswer);
                }
            }
        }

        private bool IsChecked(Guid questionId, string letter)
        {
            return assignmentAnswers.Any(a => a.QuestionID == questionId && a.AnswerText == letter);
        }


        //protected async Task ValidateAnswersWithChatGPT()
        //{
        //    foreach (var item in assignmentAnswers)
        //    {
        //        string prompt = $@"
        //For each question and answer provided, verify the answer exactly as given based on correctness and completeness. Do not alter or add to the user’s answer. If correct, specify 'Correct and Complete' or 'Correct but Incomplete' and explain why. If incorrect, provide the correct answer and a brief explanation. Use the following structure:

        //- QuestionID: {item.QuestionID}
        //- Question: {assignmentQuestions.FirstOrDefault(x => x.QuestionID == item.QuestionID)?.QuestionText}
        //- Answer: {item.AnswerText}
        //- Verification: Correct and Complete / Correct but Incomplete / Incorrect
        //- Explanation: [Provide an explanation for your verification and, if incomplete, include additional information to make the answer complete. Do not change the original answer.]

        //Example:

        //- QuestionID: 5c163da2-bbcb-4db9-bfc7-137251222f50
        //- Question: What are the main causes of erosion in Zambia?
        //- Answer: Floods, Improved drainage systems can help mitigate erosions
        //- Verification: Incorrect
        //- Explanation: The main causes of erosion in Zambia include deforestation, overgrazing, poor agricultural practices, and mining. Flooding contributes to erosion, but drainage solutions address water management rather than root causes.";

        //        try
        //        {
        //            // Call ChatGPT API with the prompt
        //            var result = await ChatClientService.GetChatResponseAsync("api/Chat/AskChatGPT", prompt);

        //            // Log or process each result with its QuestionID
        //            Console.WriteLine($"QuestionID: {item.QuestionID}, Verification Result: {result}");

        //            // Optional: You might store the result in a list or dictionary if further processing is needed
        //            // For example: verificationResults.Add(item.QuestionID, result);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error verifying QuestionID: {item.QuestionID}, Exception: {ex.Message}");
        //            // Optional: You might store the error in a list or dictionary if further processing is needed
        //            // For example: verificationResults.Add(item.QuestionID, $"Error: {ex.Message}");
        //        }
        //    }

        //    // Further process the collected results if needed
        //}

        protected async Task SaveAssignment()
        {
            isLoading = true;
            //save Student Assignment
            studentAssignment.StudentAssignmentID = Guid.NewGuid();
            studentAssignment.StudentID = studentID;
            studentAssignment.SubmissionDate = DateTime.Today;
            studentAssignment.AssignmentID = assignmentQuestions.Select(x => x.AssignmentID).FirstOrDefault();
            studentAssignment.Status = "Pending";
            var service = GenericServiceFactory.GetService<StudentAssignment>();
            var studentAssignmentresult = await service.PostAsync("api/Dynamic/PostEntity", "studentassignment", studentAssignment);
            if (studentAssignmentresult.IsSuccess)
            {
                foreach (var answer in assignmentAnswers)
                {

                    answer.AnswerID = Guid.NewGuid();
                    answer.StudentAssignmentID = studentAssignmentresult.Data.StudentAssignmentID;
                    answer.AnsweredDate = DateTime.Today;
                    answer.MarksObtained = 0;


                }
                //Save Student Answers
                var service1 = GenericServiceFactory.GetService<List<StudentAssignmentAnswer>>();
                var studentAnswers = await service1.PostAsync("api/Dynamic/PostEntities", "studentassignmentasnwers", assignmentAnswers);

                if (studentAnswers.IsSuccess)
                {

                    Snackbar.Add("Congrats! Assignment has been submitted successfully", Severity.Success);
                    _navigationManager.NavigateTo(_navigationManager.BaseUri);

                }
                else
                {
                    Snackbar.Add("Error Saving Assignment refresh your page and try again", Severity.Error);
                    return;
                }
            }
            else
            {
                Snackbar.Add("Error Saving Assignment refresh your page and try again", Severity.Error);
                return;
            }
            isLoading = false;
        }
    }
}