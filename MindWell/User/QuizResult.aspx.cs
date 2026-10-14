// Purpose: User quiz-result page that displays an attempt score and links to follow-up learning actions.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class QuizResult : UserPageBase
    {
        protected string RenderQuizResult()
        {
            // Result lookup is scoped to the current user so learners cannot view another user's attempt.
            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            int attemptId;
            if (!SecurityHelper.TryGetPositiveIntQuery("AttemptID", out attemptId))
                return "<section class='card'><h1>Result not found</h1></section>";

            DataTable result = MindWellServices.Query(@"SELECT a.Score, a.Passed, q.QuizTitle, q.ModuleID, lm.ModuleName FROM UserQuizAttempts a INNER JOIN Quizzes q ON a.QuizID=q.QuizID INNER JOIN LearningModules lm ON q.ModuleID=lm.ModuleID WHERE a.AttemptID=@AttemptID AND a.UserID=@UserID", new SqlParameter("@AttemptID", attemptId), new SqlParameter("@UserID", userId));
            if (result.Rows.Count == 0) return "<section class='card'><h1>Result not found</h1></section>";
            DataRow row = result.Rows[0];
            bool passed = Convert.ToBoolean(row["Passed"]);
            var sb = new StringBuilder();
            sb.Append("<section class='card quiz-result-card' data-passed='").Append(passed ? "true" : "false").Append("'><span class='result-emblem' aria-hidden='true'>").Append(passed ? "✓" : "↻").Append("</span><h1>Quiz Result</h1><h2>").Append(MindWellServices.Encode(row["QuizTitle"]))
                .Append("</h2><p class='score-display'>Score: <strong>").Append(row["Score"]).Append("%</strong></p><p>")
                .Append(passed ? "Great work. You passed this quiz." : "Review the module materials and try again.")
                .Append("</p><p class='page-actions'><a class='btn btn-primary' href='Progress.aspx?ModuleID=").Append(row["ModuleID"])
                .Append("'>View Progress</a> <a class='btn btn-outline' href='Quiz.aspx?ModuleID=").Append(row["ModuleID"])
                .Append("'>Retake Quiz</a> <a class='btn btn-outline' href='../Public/ModuleDetails.aspx?ModuleID=").Append(row["ModuleID"])
                .Append("'>Back to Module</a></p></section>")
                .Append(RenderAnswerReview(attemptId))
                .Append("<section class='card'><h2>Suggested Resources</h2><p>Review articles and resources for ").Append(MindWellServices.Encode(row["ModuleName"]))
                .Append(".</p><p class='page-actions'><a class='btn btn-outline' href='../Public/Articles.aspx?ModuleID=").Append(row["ModuleID"])
                .Append("'>Articles</a> <a class='btn btn-outline' href='../Public/Resources.aspx'>Resource Library</a></p></section>");
            return sb.ToString();
        }

        private string RenderAnswerReview(int attemptId)
        {
            DataTable answers = MindWellServices.Query(@"
SELECT q.QuestionText, q.OptionA, q.OptionB, q.OptionC, q.OptionD, q.CorrectAnswer, q.Explanation, a.SelectedAnswer, a.IsCorrect
FROM UserQuizAttemptAnswers a
INNER JOIN QuizQuestions q ON a.QuestionID=q.QuestionID
WHERE a.AttemptID=@AttemptID
ORDER BY ISNULL(q.SortOrder, q.OrderIndex), q.QuestionID", new SqlParameter("@AttemptID", attemptId));
            if (answers.Rows.Count == 0)
                return string.Empty;

            var sb = new StringBuilder("<section class='card'><h2>Answer Review</h2>");
            foreach (DataRow answer in answers.Rows)
            {
                string selected = Convert.ToString(answer["SelectedAnswer"]);
                string correct = Convert.ToString(answer["CorrectAnswer"]);
                sb.Append("<article class='quiz-review-card'><h3>").Append(MindWellServices.Encode(answer["QuestionText"])).Append("</h3><p><strong>Selected answer:</strong> ")
                    .Append(MindWellServices.Encode(FormatAnswer(answer, selected))).Append("</p><p><strong>Correct answer:</strong> ")
                    .Append(MindWellServices.Encode(FormatAnswer(answer, correct))).Append("</p><p><strong>Explanation:</strong> ")
                    .Append(MindWellServices.Encode(answer["Explanation"])).Append("</p></article>");
            }

            return sb.Append("</section>").ToString();
        }

        private string FormatAnswer(DataRow row, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "No answer selected";

            string columnName = "Option" + key.ToUpperInvariant();
            return key.ToUpperInvariant() + " - " + (row.Table.Columns.Contains(columnName) ? Convert.ToString(row[columnName]) : string.Empty);
        }
    }
}
