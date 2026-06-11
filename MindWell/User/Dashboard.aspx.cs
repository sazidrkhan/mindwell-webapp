// Purpose: User dashboard showing learning statistics and shortcuts to continue modules/quizzes.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class Dashboard : UserPageBase
    {
        protected string RenderDashboard()
        {
            // Aggregates the current user's completion, quiz, and mood counts before listing modules.
            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            string name = GetDashboardDisplayName(userId);
            object completed = MindWellServices.Scalar(@"SELECT COUNT(DISTINCT ModuleID)
FROM (
    SELECT q.ModuleID
    FROM UserQuizAttempts a
    INNER JOIN Quizzes q ON a.QuizID=q.QuizID
    INNER JOIN LearningModules lm ON q.ModuleID=lm.ModuleID
    WHERE a.UserID = @UserID AND a.Passed=1 AND " + MindWellServices.ActiveQuizWhere + " AND " + MindWellServices.ActiveModuleWhere + @"
) passedModules", new SqlParameter("@UserID", userId));
            object totalModules = MindWellServices.Scalar(MindWellServices.ActiveModuleCountSql);
            object attempts = MindWellServices.Scalar(@"SELECT COUNT(*)
FROM UserQuizAttempts a
INNER JOIN Quizzes q ON a.QuizID=q.QuizID
INNER JOIN LearningModules lm ON q.ModuleID=lm.ModuleID
WHERE a.UserID = @UserID AND " + MindWellServices.ActiveQuizWhere + " AND " + MindWellServices.ActiveModuleWhere, new SqlParameter("@UserID", userId));
            object moodCount = MindWellServices.Scalar("SELECT COUNT(*) FROM MoodEntries WHERE UserID = @UserID", new SqlParameter("@UserID", userId));

            DataTable modules = MindWellServices.Query(MindWellServices.ActiveModuleSelectSql);
            string moduleDenominator = "/" + MindWellServices.ToInt(totalModules, 4);
            if (moduleDenominator == "/0")
                moduleDenominator = "/4";
            var sb = new StringBuilder();
            sb.Append("<section class='section-header'><h1>Welcome, ").Append(MindWellServices.Encode(name)).Append("</h1><p>Your wellbeing learning hub.</p></section>")
                .Append("<div class='row cols-3'>")
                .Append("<div class='card stat-card'><h2>Completed Modules</h2><p class='stat-number'>").Append(MindWellServices.ToInt(completed, 0)).Append(moduleDenominator).Append("</p></div>")
                .Append("<div class='card stat-card'><h2>Quiz Attempts</h2><p class='stat-number'>").Append(MindWellServices.ToInt(attempts, 0)).Append("</p></div>")
                .Append("<div class='card stat-card'><h2>Mood Entries</h2><p class='stat-number'>").Append(MindWellServices.ToInt(moodCount, 0)).Append("</p></div>")
                .Append("</div>")
                .Append("<section class='section-header'><h2>Continue Learning</h2></section>")
                .Append("<div class='row cols-2'>");

            foreach (DataRow row in modules.Rows)
            {
                // Each module card gives learners direct access to both content and assessment.
                sb.Append("<div class='card'><h3>").Append(MindWellServices.Encode(row["ModuleName"]))
                    .Append("</h3><p>").Append(MindWellServices.Encode(row["Description"])).Append("</p>")
                    .Append("<div class='button-row'>")
                    .Append("<a class='btn btn-primary' href='../Public/ModuleDetails.aspx?ModuleID=").Append(row["ModuleID"]).Append("'>Open Module</a>")
                    .Append("<a class='btn btn-outline' href='Quiz.aspx?ModuleID=").Append(row["ModuleID"]).Append("'>Take Quiz</a>")
                    .Append("</div></div>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        private string GetDashboardDisplayName(int userId)
        {
            DataTable user = MindWellServices.Query("SELECT TOP 1 FirstName, LastName, Username FROM Users WHERE UserID=@UserID", new SqlParameter("@UserID", userId));
            if (user.Rows.Count == 0)
                return SecurityHelper.GetCurrentUsername() ?? "Learner";

            string firstName = Convert.ToString(user.Rows[0]["FirstName"]);
            string lastName = Convert.ToString(user.Rows[0]["LastName"]);
            string username = Convert.ToString(user.Rows[0]["Username"]);
            string fullName = ((firstName ?? string.Empty).Trim() + " " + (lastName ?? string.Empty).Trim()).Trim();

            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            return string.IsNullOrWhiteSpace(username) ? "Learner" : username;
        }
    }
}
