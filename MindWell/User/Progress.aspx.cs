// Purpose: User progress overview showing every module, completion percentage,
// and links back into module details or quizzes.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class Progress : UserPageBase
    {
        protected string RenderProgressOverview()
        {
            // Joins modules with the current user's progress so untouched modules still appear at 0%.
            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            int moduleFilter = 0;
            if (Request.QueryString["ModuleID"] != null && !SecurityHelper.TryGetPositiveIntQuery("ModuleID", out moduleFilter))
                return "<section class='card'><h1>Invalid module selected</h1><p class='page-actions'><a class='btn btn-outline' href='Progress.aspx'>View all progress</a></p></section>";

            QueryResult progressResult = MindWellServices.TryQuery(@"
SELECT lm.ModuleID, lm.ModuleName,
       CASE WHEN ISNULL(best.Score,0) >= ISNULL(quiz.PassingScore,70) THEN 100 ELSE ISNULL(up.PercentageComplete, ISNULL(best.Score,0)) END AS PercentageComplete,
       CASE WHEN ISNULL(best.Score,0) >= ISNULL(quiz.PassingScore,70) THEN 1 ELSE ISNULL(up.IsCompleted,0) END AS IsCompleted,
       latest.AttemptDate,
       best.Score
FROM LearningModules lm
LEFT JOIN UserProgress up ON lm.ModuleID=up.ModuleID AND up.UserID=@UserID
OUTER APPLY (
    SELECT TOP 1 q.PassingScore
    FROM Quizzes q
    WHERE q.ModuleID=lm.ModuleID AND " + MindWellServices.ActiveQuizWhere + @"
    ORDER BY q.QuizID
) quiz
OUTER APPLY (
    SELECT TOP 1 a.AttemptDate, a.Score, a.Passed
    FROM UserQuizAttempts a
    INNER JOIN Quizzes q ON a.QuizID=q.QuizID
    WHERE a.UserID=@UserID AND q.ModuleID=lm.ModuleID AND " + MindWellServices.ActiveQuizWhere + @"
    ORDER BY a.AttemptDate DESC
) latest
OUTER APPLY (
    SELECT TOP 1 a.Score
    FROM UserQuizAttempts a
    INNER JOIN Quizzes q ON a.QuizID=q.QuizID
    WHERE a.UserID=@UserID AND q.ModuleID=lm.ModuleID AND " + MindWellServices.ActiveQuizWhere + @"
    ORDER BY a.Score DESC, a.AttemptDate DESC
) best
WHERE " + MindWellServices.ActiveModuleWhere + @" AND (@ModuleID=0 OR lm.ModuleID=@ModuleID)
ORDER BY lm.OrderIndex", new SqlParameter("@UserID", userId), new SqlParameter("@ModuleID", moduleFilter));
            if (progressResult.HasError)
                return MindWellServices.RenderDataUnavailable("Your learning progress could not be loaded. Please try again.");
            DataTable rows = progressResult.Data;
            string moduleBackLink = moduleFilter > 0
                ? "<p class='back-link'><a class='btn btn-outline btn-sm' href='../Public/ModuleDetails.aspx?ModuleID=" + moduleFilter + "'>Back to Module</a></p>"
                : string.Empty;
            var sb = new StringBuilder(moduleBackLink + "<section class='section-header'><h1>Progress & Achievements</h1><p>Every small step counts. Here is your journey so far.</p></section><div class='progress-journey'>");
            if (rows.Rows.Count == 0)
                sb.Append(MindWellServices.RenderEmptyState("Your learning journey starts here.", "Published modules will appear here when they are ready."));
            foreach (DataRow row in rows.Rows)
                RenderModuleProgress(sb, row);
            sb.Append("</div>");
            return sb.ToString();
        }

        private void RenderModuleProgress(StringBuilder sb, DataRow row)
        {
            // Each module card uses the same action block so button alignment remains consistent across rows.
            int id = MindWellServices.ToInt(row["ModuleID"], 1);
            int percent = Math.Max(0, Math.Min(100, MindWellServices.ToInt(row["PercentageComplete"], 0)));
            sb.Append("<div class='card'>").Append(WellbeingUi.Artwork(id)).Append("<h2>").Append(MindWellServices.Encode(row["ModuleName"]))
                .Append("</h2><div class='progress-shell' role='progressbar' aria-label='Module completion' aria-valuemin='0' aria-valuemax='100' aria-valuenow='").Append(percent).Append("'><div class='progress-fill' style='width:").Append(percent)
                .Append("%'></div></div><p>").Append(percent).Append("% complete</p>");
            if (row["Score"] != DBNull.Value)
                sb.Append("<p>Best quiz score: <strong>").Append(MindWellServices.Encode(row["Score"])).Append("%</strong></p>");
            if (row["AttemptDate"] != DBNull.Value)
                sb.Append("<p>Latest attempt: ").Append(MindWellServices.Encode(row["AttemptDate"])).Append("</p>");

            sb.Append("<div class='progress-actions'><a class='btn btn-outline' href='../Public/ModuleDetails.aspx?ModuleID=").Append(id)
                .Append("'>Module Details</a><a class='btn btn-primary' href='Quiz.aspx?ModuleID=").Append(id)
                .Append("'>Quiz</a></div></div>");
        }
    }
}
