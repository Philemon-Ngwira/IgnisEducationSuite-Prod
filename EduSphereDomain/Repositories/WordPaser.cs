using EDUSphereSharedProject.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace EduSphereDomain.Repositories
{
    public class WordParser
    {
        public List<AssignmentQuestion> ExtractQuestionsFromWord(string filePath)
        {
            var questions = new List<AssignmentQuestion>();

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart.Document.Body;
                int questionNumber = 1;

                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    // Check if the paragraph is part of a numbered list and has text
                    if (IsListItem(paragraph))
                    {
                        string text = GetParagraphText(paragraph);

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            questions.Add(new AssignmentQuestion
                            {
                                QuestionNumber = questionNumber++,
                                QuestionText = text
                            });
                        }
                    }
                }
            }

            return questions;
        }

        // Check if paragraph is numbered by detecting NumberingProperties
        private bool IsListItem(Paragraph paragraph)
        {
            return paragraph.ParagraphProperties?.NumberingProperties != null;
        }

        // Retrieve all text content from a paragraph, handling potential nested runs
        private string GetParagraphText(Paragraph paragraph)
        {
            return string.Concat(paragraph.Descendants<Run>().Select(run => run.InnerText));
        }
    }
}
