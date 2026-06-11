// Purpose: Base classes for module-focused learning pages, including shared article,
// quiz, and progress rendering logic for authenticated users.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell
{
    public abstract class ModuleUserPageBase : UserPageBase
    {
        protected abstract string ModuleSlug { get; }
        protected int ModuleId { get { return MindWellServices.GetModuleId(ModuleSlug); } }
        protected string ModuleName { get { return MindWellServices.GetModuleName(ModuleSlug); } }
        protected int CurrentUserId { get { return SecurityHelper.GetCurrentUserId() ?? 0; } }
    }

    public abstract class ModuleArticlesPageBase : ModuleUserPageBase
    {
        protected string RenderArticles()
        {
            // Marks module engagement and lists published articles as learning entry points.
            MindWellServices.UpsertProgress(CurrentUserId, ModuleId, 25, false);
            DataTable articles = MindWellServices.Query(@"
SELECT ArticleID, Title, Content, CreatedDate
FROM Articles
WHERE ModuleID = @ModuleID AND IsPublished = 1
ORDER BY CreatedDate DESC",
                new SqlParameter("@ModuleID", ModuleId));

            var sb = new StringBuilder();
            sb.Append("<div class='section-header'><h1>").Append(MindWellServices.Encode(ModuleName)).Append(" Articles</h1>")
                .Append("<p>Read the core learning material, then take the module quiz.</p></div><div class='row'>");

            if (articles.Rows.Count == 0)
                sb.Append("<div class='card'><p>No articles are available yet. Seed the database to load module content.</p></div>");

            foreach (DataRow row in articles.Rows)
            {
                string preview = MindWellServices.HtmlOrEmpty(row["Content"]);
                if (preview.Length > 220)
                    preview = preview.Substring(0, 220) + "...";

                sb.Append("<article class='card'><h2>").Append(MindWellServices.Encode(row["Title"]))
                    .Append("</h2><div class='article-preview'>").Append(preview)
                    .Append("</div><p><a class='btn btn-primary' href='../Public/ArticleDetails.aspx?ArticleID=")
                    .Append(row["ArticleID"]).Append("'>Read Article</a></p></article>");
            }

            sb.Append("</div><p class='actions'><a class='btn btn-secondary' href='Quiz.aspx?ModuleID=").Append(ModuleId)
                .Append("'>Take Quiz</a> <a class='btn btn-outline' href='Progress.aspx'>View Progress</a></p>");
            return sb.ToString();
        }
    }

    public abstract class ModuleQuizPageBase : ModuleUserPageBase
    {
        protected string RenderQuiz()
        {
            if (IsPostBack && Request.Form["quizAction"] == "submit")
                return SubmitQuiz();

            return RenderQuizForm();
        }

        private string RenderQuizForm()
        {
            // Builds a module quiz form from database questions and answer options.
            DataTable quiz = MindWellServices.Query("SELECT TOP 1 QuizID, QuizTitle, Description FROM Quizzes WHERE ModuleID = @ModuleID", new SqlParameter("@ModuleID", ModuleId));
            if (quiz.Rows.Count == 0)
                return "<div class='card'><h1>Quiz unavailable</h1><p>No quiz questions are available for this module yet.</p></div>";

            int quizId = MindWellServices.ToInt(quiz.Rows[0]["QuizID"], 0);
            DataTable questions = MindWellServices.Query(@"
SELECT QuestionID, QuestionText, OptionA, OptionB, OptionC, OptionD
FROM QuizQuestions
WHERE QuizID = @QuizID
  AND NULLIF(LTRIM(RTRIM(QuestionText)), '') IS NOT NULL
  AND " + MindWellServices.CleanDemoTextWhere("QuestionText") + @"
ORDER BY OrderIndex",
                new SqlParameter("@QuizID", quizId));
            if (questions.Rows.Count == 0)
                return "<div class='card'><h1>Quiz unavailable</h1><p>No quiz questions are available for this module yet.</p></div>";

            var sb = new StringBuilder();
            sb.Append("<section class='card'><h1>").Append(MindWellServices.Encode(quiz.Rows[0]["QuizTitle"]))
                .Append("</h1><p>").Append(MindWellServices.Encode(quiz.Rows[0]["Description"]))
                .Append("</p><input type='hidden' name='quizAction' value='submit' /><input type='hidden' name='quizId' value='")
                .Append(quizId).Append("' />");

            foreach (DataRow row in questions.Rows)
            {
                string id = row["QuestionID"].ToString();
                sb.Append("<fieldset class='question-panel'><legend>").Append(MindWellServices.Encode(row["QuestionText"])).Append("</legend>");
                AppendOption(sb, id, "A", row["OptionA"]);
                AppendOption(sb, id, "B", row["OptionB"]);
                AppendOption(sb, id, "C", row["OptionC"]);
                AppendOption(sb, id, "D", row["OptionD"]);
                sb.Append("</fieldset>");
            }

            sb.Append("<button class='btn btn-primary' type='submit'>Submit Quiz</button></section>");
            return sb.ToString();
        }

        private static void AppendOption(StringBuilder sb, string questionId, string value, object text)
        {
            sb.Append("<label class='quiz-option'><input type='radio' required name='q")
                .Append(questionId).Append("' value='").Append(value).Append("' /> ")
                .Append(MindWellServices.Encode(text)).Append("</label>");
        }

        private string SubmitQuiz()
        {
            // Scores submitted answers, stores the attempt, and updates module progress/certification state.
            int quizId = MindWellServices.ToInt(Request.Form["quizId"], 0);
            DataTable answers = MindWellServices.Query("SELECT QuestionID, CorrectAnswer FROM QuizQuestions WHERE QuizID = @QuizID AND NULLIF(LTRIM(RTRIM(QuestionText)), '') IS NOT NULL AND " + MindWellServices.CleanDemoTextWhere("QuestionText"), new SqlParameter("@QuizID", quizId));
            if (answers.Rows.Count == 0)
                return "<div class='card'><h1>Quiz unavailable</h1><p>No quiz questions are available for this module yet.</p></div>";

            int correct = 0;
            foreach (DataRow row in answers.Rows)
            {
                string selected = Request.Form["q" + row["QuestionID"]];
                if (selected == row["CorrectAnswer"].ToString())
                    correct++;
            }

            int score = (correct * 100) / answers.Rows.Count;
            bool passed = score >= 70;

            MindWellServices.NonQuery(@"
INSERT INTO UserQuizAttempts (UserID, QuizID, Score, Passed, AttemptDate, TimeSpentSeconds)
VALUES (@UserID, @QuizID, @Score, @Passed, GETDATE(), 0)",
                new SqlParameter("@UserID", CurrentUserId),
                new SqlParameter("@QuizID", quizId),
                new SqlParameter("@Score", score),
                new SqlParameter("@Passed", passed));

            MindWellServices.UpsertProgress(CurrentUserId, ModuleId, passed ? 100 : 60, passed);
            SecurityHelper.LogAction(CurrentUserId, "QuizCompleted", "UserQuizAttempts", quizId, null, score + "%");

            return "<section class='card'><h1>Quiz Result</h1><p class='score-display'>Your score: <strong>" + score + "%</strong></p><p>" +
                (passed ? "Congratulations, you passed this module." : "Keep reviewing the articles and try again.") +
                "</p><p><a class='btn btn-primary' href='Progress.aspx'>View Progress</a> <a class='btn btn-outline' href='Quiz.aspx?ModuleID=" + ModuleId + "'>Retake Quiz</a></p></section>";
        }
    }

    public abstract class ModuleProgressPageBase : ModuleUserPageBase
    {
        protected string RenderProgress()
        {
            // Summarizes a learner's progress and best quiz score for one module.
            DataTable progress = MindWellServices.Query(@"
SELECT PercentageComplete, IsCompleted, CompletionDate
FROM UserProgress
WHERE UserID = @UserID AND ModuleID = @ModuleID",
                new SqlParameter("@UserID", CurrentUserId),
                new SqlParameter("@ModuleID", ModuleId));

            int percent = progress.Rows.Count == 0 ? 0 : MindWellServices.ToInt(progress.Rows[0]["PercentageComplete"], 0);
            bool completed = progress.Rows.Count > 0 && Convert.ToBoolean(progress.Rows[0]["IsCompleted"]);

            object best = MindWellServices.Scalar(@"
SELECT MAX(Score)
FROM UserQuizAttempts a
INNER JOIN Quizzes q ON a.QuizID = q.QuizID
WHERE a.UserID = @UserID AND q.ModuleID = @ModuleID",
                new SqlParameter("@UserID", CurrentUserId),
                new SqlParameter("@ModuleID", ModuleId));

            var sb = new StringBuilder();
            sb.Append("<section class='card'><h1>").Append(MindWellServices.Encode(ModuleName)).Append(" Progress</h1>")
                .Append("<div class='progress-shell'><div class='progress-fill' style='width:").Append(percent).Append("%'></div></div>")
                .Append("<p><strong>").Append(percent).Append("%</strong> complete</p>")
                .Append("<p>Best quiz score: <strong>").Append(best == null || best == DBNull.Value ? "Not attempted" : best + "%").Append("</strong></p>")
                .Append("<p>Status: <strong>").Append(completed ? "Completed" : "In progress").Append("</strong></p>");

            sb.Append("<p><a class='btn btn-outline' href='../Public/Articles.aspx?ModuleID=").Append(ModuleId).Append("'>Back to Articles</a></p></section>");
            return sb.ToString();
        }
    }
}
