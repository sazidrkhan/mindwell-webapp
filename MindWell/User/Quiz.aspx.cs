// Purpose: User quiz page that either lists available quizzes or renders/submits a selected module quiz.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class Quiz : UserPageBase
    {
        protected string RenderQuiz()
        {
            // POST requests score the quiz; GET requests either show quiz selection or a selected module quiz.
            if (IsPostBack && Request.Form["quizAction"] == "submit")
                return SubmitQuiz();

            string requestedModule = Request.QueryString["ModuleID"];
            if (string.IsNullOrWhiteSpace(requestedModule))
                return RenderQuizSelection();

            int moduleId;
            if (!SecurityHelper.TryGetPositiveIntQuery("ModuleID", out moduleId))
                return "<section class='card'><h1>Invalid module selected</h1><p>Invalid module selected. Choose a quiz from the list.</p><p class='page-actions'><a class='btn btn-outline' href='Quiz.aspx'>Choose Quiz</a></p></section>";

            DataTable quiz = MindWellServices.Query(@"SELECT q.QuizID, q.QuizTitle, q.Description, q.PassingScore
FROM LearningModules lm
CROSS APPLY (
    SELECT TOP 1 q.QuizID, q.QuizTitle, q.Description, q.PassingScore
    FROM Quizzes q
    WHERE q.ModuleID=lm.ModuleID AND " + MindWellServices.ActiveQuizWhere + @"
    ORDER BY q.QuizID
) q
WHERE lm.ModuleID=@ModuleID AND " + MindWellServices.ActiveModuleWhere, new SqlParameter("@ModuleID", moduleId));
            if (quiz.Rows.Count == 0) return "<section class='card'><h1>Quiz unavailable</h1><p>No quiz questions are available for this module yet.</p><p class='page-actions'><a class='btn btn-outline' href='Quiz.aspx'>Choose Quiz</a></p></section>";
            int quizId = MindWellServices.ToInt(quiz.Rows[0]["QuizID"], 0);
            DataTable questions = MindWellServices.Query("SELECT QuestionID, QuestionText, OptionA, OptionB, OptionC, OptionD FROM QuizQuestions WHERE QuizID=@QuizID AND NULLIF(LTRIM(RTRIM(QuestionText)), '') IS NOT NULL AND " + MindWellServices.CleanDemoTextWhere("QuestionText") + " ORDER BY ISNULL(SortOrder, OrderIndex), QuestionID", new SqlParameter("@QuizID", quizId));
            if (questions.Rows.Count == 0)
                return "<section class='card'><h1>Quiz unavailable</h1><p>No quiz questions are available for this module yet.</p><p class='page-actions'><a class='btn btn-outline' href='Quiz.aspx'>Choose Quiz</a></p></section>";
            var sb = new StringBuilder("<section class='card'><h1>");
            sb.Append(MindWellServices.Encode(quiz.Rows[0]["QuizTitle"])).Append("</h1><p>").Append(MindWellServices.Encode(quiz.Rows[0]["Description"])).Append("</p><p class='content-kicker'>Passing score: ").Append(quiz.Rows[0]["PassingScore"]).Append("%</p><p class='page-actions'><a class='btn btn-outline btn-sm' href='../Public/ModuleDetails.aspx?ModuleID=").Append(moduleId).Append("'>Back to Module Details</a></p><input type='hidden' name='quizAction' value='submit' /><input type='hidden' name='quizId' value='").Append(quizId).Append("' /><input type='hidden' name='moduleId' value='").Append(moduleId).Append("' />");
            foreach (DataRow row in questions.Rows)
            {
                string id = row["QuestionID"].ToString();
                sb.Append("<fieldset class='question-panel'><legend>").Append(MindWellServices.Encode(row["QuestionText"])).Append("</legend>");
                AppendOption(sb, id, "A", row["OptionA"]); AppendOption(sb, id, "B", row["OptionB"]); AppendOption(sb, id, "C", row["OptionC"]); AppendOption(sb, id, "D", row["OptionD"]);
                sb.Append("</fieldset>");
            }
            return sb.Append("<p class='page-actions'><button class='btn btn-primary'>Submit Quiz</button></p></section>").ToString();
        }

        private string RenderQuizSelection()
        {
            // Lists all quizzes with the user's latest attempt so learners can choose what to retry or continue.
            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            DataTable quizzes = MindWellServices.Query(@"
SELECT q.QuizID, q.QuizTitle, q.Description, lm.ModuleID, q.PassingScore, lm.ModuleName, latest.Score, latest.Passed, latest.AttemptDate
FROM LearningModules lm
CROSS APPLY (
    SELECT TOP 1 q.QuizID, q.QuizTitle, q.Description, q.ModuleID, q.PassingScore
    FROM Quizzes q
    WHERE q.ModuleID=lm.ModuleID AND " + MindWellServices.ActiveQuizWhere + @"
    ORDER BY q.QuizID
) q
OUTER APPLY (
    SELECT TOP 1 Score, Passed, AttemptDate
    FROM UserQuizAttempts a
    WHERE a.UserID = @UserID AND a.QuizID = q.QuizID
    ORDER BY AttemptDate DESC
) latest
WHERE " + MindWellServices.ActiveModuleWhere + @"
ORDER BY lm.OrderIndex", new SqlParameter("@UserID", userId));

            var sb = new StringBuilder("<section class='section-header'><h1>Choose a Quiz</h1><p>Select any module quiz and review your latest score feedback.</p></section><div class='row cols-2'>");
            foreach (DataRow row in quizzes.Rows)
            {
                sb.Append("<article class='card quiz-card'><p class='content-kicker'>")
                    .Append(MindWellServices.Encode(row["ModuleName"]))
                    .Append("</p><h2>")
                    .Append(MindWellServices.Encode(row["QuizTitle"]))
                    .Append("</h2><p>")
                    .Append(MindWellServices.Encode(row["Description"]))
                    .Append("</p><p class='content-kicker'>Passing score: ").Append(row["PassingScore"]).Append("%</p>");

                if (row["Score"] != DBNull.Value)
                {
                    bool passed = Convert.ToBoolean(row["Passed"]);
                    sb.Append("<p class='quiz-feedback ")
                        .Append(passed ? "passed" : "needs-review")
                        .Append("'>Latest score: <strong>")
                        .Append(row["Score"])
                        .Append("%</strong> - ")
                        .Append(passed ? "Passed" : "Review recommended")
                        .Append("</p>");
                }
                else
                {
                    sb.Append("<p class='quiz-feedback'>No attempt yet.</p>");
                }

                sb.Append("<div class='button-row'><a class='btn btn-primary' href='Quiz.aspx?ModuleID=")
                    .Append(row["ModuleID"])
                    .Append("'>Start Quiz</a><a class='btn btn-outline' href='../Public/ModuleDetails.aspx?ModuleID=")
                    .Append(row["ModuleID"])
                    .Append("'>Module Details</a></div></article>");
            }

            return sb.Append("</div>").ToString();
        }

        private static void AppendOption(StringBuilder sb, string questionId, string value, object text)
        {
            sb.Append("<label class='quiz-option'><input type='radio' required name='q").Append(questionId).Append("' value='").Append(value).Append("' /> ").Append(MindWellServices.Encode(text)).Append("</label>");
        }

        private string SubmitQuiz()
        {
            // Compares posted answers with correct answers, records the attempt, and updates module progress.
            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            int quizId;
            int moduleId;
            if (!SecurityHelper.TryGetPositiveIntForm("quizId", out quizId) || !SecurityHelper.TryGetPositiveIntForm("moduleId", out moduleId))
                return "<section class='card'><h1>Invalid module selected</h1><p>Please choose a published quiz and try again.</p></section>";

            DataTable quiz = MindWellServices.Query(@"SELECT TOP 1 q.PassingScore
FROM Quizzes q
INNER JOIN LearningModules lm ON q.ModuleID=lm.ModuleID
WHERE q.QuizID=@QuizID AND q.ModuleID=@ModuleID AND " + MindWellServices.ActiveQuizWhere + " AND " + MindWellServices.ActiveModuleWhere, new SqlParameter("@QuizID", quizId), new SqlParameter("@ModuleID", moduleId));
            if (quiz.Rows.Count == 0)
                return "<section class='card'><h1>Quiz unavailable</h1></section>";

            int passingScore = MindWellServices.ToInt(quiz.Rows[0]["PassingScore"], 70);
            DataTable answers = MindWellServices.Query("SELECT QuestionID, CorrectAnswer FROM QuizQuestions WHERE QuizID=@QuizID AND NULLIF(LTRIM(RTRIM(QuestionText)), '') IS NOT NULL AND " + MindWellServices.CleanDemoTextWhere("QuestionText") + " ORDER BY ISNULL(SortOrder, OrderIndex), QuestionID", new SqlParameter("@QuizID", quizId));
            if (answers.Rows.Count == 0)
                return "<section class='card'><h1>Quiz unavailable</h1><p>No quiz questions are available for this module yet.</p></section>";
            int correct = 0;
            foreach (DataRow row in answers.Rows)
                if ((Request.Form["q" + row["QuestionID"]] ?? string.Empty) == row["CorrectAnswer"].ToString()) correct++;
            int score = answers.Rows.Count == 0 ? 0 : (correct * 100) / answers.Rows.Count;
            bool passed = score >= passingScore;
            object attempt = MindWellServices.Scalar(@"INSERT INTO UserQuizAttempts (UserID, QuizID, Score, Passed, AttemptDate, TimeSpentSeconds) VALUES (@UserID,@QuizID,@Score,@Passed,GETDATE(),0); SELECT CAST(SCOPE_IDENTITY() AS int);", new SqlParameter("@UserID", userId), new SqlParameter("@QuizID", quizId), new SqlParameter("@Score", score), new SqlParameter("@Passed", passed));
            int attemptId = MindWellServices.ToInt(attempt, 0);
            foreach (DataRow row in answers.Rows)
            {
                string selectedAnswer = Request.Form["q" + row["QuestionID"]] ?? string.Empty;
                bool isCorrect = selectedAnswer == row["CorrectAnswer"].ToString();
                MindWellServices.NonQuery("INSERT INTO UserQuizAttemptAnswers (AttemptID, QuestionID, SelectedAnswer, IsCorrect, CreatedDate) VALUES (@AttemptID,@QuestionID,@SelectedAnswer,@IsCorrect,GETDATE())", new SqlParameter("@AttemptID", attemptId), new SqlParameter("@QuestionID", MindWellServices.ToInt(row["QuestionID"], 0)), new SqlParameter("@SelectedAnswer", string.IsNullOrEmpty(selectedAnswer) ? (object)DBNull.Value : selectedAnswer), new SqlParameter("@IsCorrect", isCorrect));
            }
            MindWellServices.UpsertProgress(userId, moduleId, passed ? 100 : 60, passed);
            Response.Redirect("QuizResult.aspx?AttemptID=" + attemptId, false);
            Context.ApplicationInstance.CompleteRequest();
            return "<section class='card'><p>Saving result...</p></section>";
        }
    }
}
