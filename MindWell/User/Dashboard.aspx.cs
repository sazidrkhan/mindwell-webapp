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
            bool nameUnavailable;
            string name = GetDashboardDisplayName(userId, out nameUnavailable);
            ScalarResult completedResult = MindWellServices.TryScalar(@"SELECT COUNT(DISTINCT ModuleID)
FROM (
    SELECT q.ModuleID
    FROM UserQuizAttempts a
    INNER JOIN Quizzes q ON a.QuizID=q.QuizID
    INNER JOIN LearningModules lm ON q.ModuleID=lm.ModuleID
    WHERE a.UserID = @UserID AND a.Passed=1 AND " + MindWellServices.ActiveQuizWhere + " AND " + MindWellServices.ActiveModuleWhere + @"
) passedModules", new SqlParameter("@UserID", userId));
            ScalarResult totalModulesResult = MindWellServices.TryScalar(MindWellServices.ActiveModuleCountSql);
            ScalarResult attemptsResult = MindWellServices.TryScalar(@"SELECT COUNT(*)
FROM UserQuizAttempts a
INNER JOIN Quizzes q ON a.QuizID=q.QuizID
INNER JOIN LearningModules lm ON q.ModuleID=lm.ModuleID
WHERE a.UserID = @UserID AND " + MindWellServices.ActiveQuizWhere + " AND " + MindWellServices.ActiveModuleWhere, new SqlParameter("@UserID", userId));
            ScalarResult moodCountResult = MindWellServices.TryScalar("SELECT COUNT(*) FROM MoodEntries WHERE UserID = @UserID", new SqlParameter("@UserID", userId));

            QueryResult modulesResult = MindWellServices.TryQuery(MindWellServices.ActiveModuleSelectSql);
            if (nameUnavailable || completedResult.HasError || totalModulesResult.HasError || attemptsResult.HasError || moodCountResult.HasError || modulesResult.HasError)
                return MindWellServices.RenderDataUnavailable("Your dashboard data is temporarily unavailable.");

            DataTable modules = modulesResult.Data;
            int totalModuleCount = MindWellServices.ToInt(totalModulesResult.Value, 0);
            string moduleDenominator = totalModuleCount > 0 ? "/" + totalModuleCount : string.Empty;
            int completedModuleCount = MindWellServices.ToInt(completedResult.Value, 0);
            string completionMeter = totalModuleCount > 0
                ? "<progress class='dashboard-meter' max='" + totalModuleCount + "' value='" + Math.Max(0, Math.Min(completedModuleCount, totalModuleCount)) + "' aria-label='Completed modules'></progress>"
                : string.Empty;
            var sb = new StringBuilder();
            sb.Append("<div class='dashboard-page'><section class='dashboard-hero' data-reveal='dashboard-hero'><div class='dashboard-hero-content'><p class='dashboard-eyebrow'>Your learning space</p><h1>Welcome, ")
                .Append(MindWellServices.Encode(name))
                .Append("</h1><p>A little learning, a moment to reflect. What feels right for today?</p></div>")
                .Append(WellbeingUi.Scene(1, "dashboard")).Append("</section><div class='dashboard-layout'><div class='dashboard-main'>")
                .Append("<section class='dashboard-stat-grid' aria-label='Learning snapshot' data-reveal='dashboard-stat-grid'>")
                .Append("<div class='card stat-card' data-reveal='dashboard-stat'><h2>Completed Modules</h2><p class='stat-number'>").Append(completedModuleCount).Append(moduleDenominator).Append("</p>").Append(completionMeter).Append("</div>")
                .Append("<div class='card stat-card' data-reveal='dashboard-stat'><h2>Quiz Attempts</h2><p class='stat-number'>").Append(MindWellServices.ToInt(attemptsResult.Value, 0)).Append("</p></div>")
                .Append("<div class='card stat-card' data-reveal='dashboard-stat'><h2>Mood Entries</h2><p class='stat-number'>").Append(MindWellServices.ToInt(moodCountResult.Value, 0)).Append("</p></div>")
                .Append("</section>");

            var rail = new StringBuilder("<aside class='dashboard-rail' aria-label='Tools for your day'><nav class='dashboard-shortcuts' aria-label='Your next small step'><h2>A moment for you</h2><a href='../Public/Modules.aspx'><span>Learn something new<small>Explore wellbeing modules</small></span><span aria-hidden='true'>↗</span></a><a href='MoodTracker.aspx'><span>Check in with yourself<small>Make room for how you feel</small></span><span aria-hidden='true'>↗</span></a><a href='Journal.aspx'><span>Put thoughts into words<small>Open your reflection journal</small></span><span aria-hidden='true'>↗</span></a></nav><section class='card dashboard-next-step' aria-labelledby='dashboard-next-step-title' data-reveal='dashboard-next-step'><p class='dashboard-eyebrow'>Next step</p>");

            if (modules.Rows.Count > 0)
            {
                rail.Append("<h2 id='dashboard-next-step-title'>Choose a module to explore</h2><p>Open a module to read its lesson at your own pace. When you are ready, use its quiz to check your understanding.</p>");
            }
            else
            {
                rail.Append("<h2 id='dashboard-next-step-title'>Check back for learning modules</h2><p>Learning modules are not available right now. Check back later for new content.</p>");
            }

            rail.Append("</section><details class='dashboard-pause'><summary>Take a one-minute pause</summary>").Append(WellbeingUi.BreathingExercise("dashboard-breath")).Append("</details></aside>");
            sb.Append("<section class='dashboard-modules-section' aria-labelledby='continue-learning-title' data-reveal='dashboard-modules'><div class='section-header'><h2 id='continue-learning-title'>Continue Learning</h2></div>")
                .Append("<div class='dashboard-module-grid'>");

            if (modules.Rows.Count == 0)
            {
                sb.Append("<div class='dashboard-empty-state' data-reveal='dashboard-empty-state' role='status'><h3>No learning modules are available right now.</h3><p>Check back later for new learning content.</p></div>");
            }
            else
            {
                foreach (DataRow row in modules.Rows)
                {
                    // Each module card gives learners direct access to both content and assessment.
                    sb.Append("<div class='card dashboard-module-card' data-reveal='dashboard-module'>").Append(WellbeingUi.Scene(MindWellServices.ToInt(row["ModuleID"], 0))).Append("<h3>").Append(MindWellServices.Encode(row["ModuleName"]))
                        .Append("</h3><p>").Append(MindWellServices.Encode(row["Description"])).Append("</p>")
                        .Append("<div class='button-row dashboard-module-actions'>")
                        .Append("<a class='btn btn-primary' href='../Public/ModuleDetails.aspx?ModuleID=").Append(row["ModuleID"]).Append("'>Open Module</a>")
                        .Append("<a class='btn btn-outline' href='Quiz.aspx?ModuleID=").Append(row["ModuleID"]).Append("'>Take Quiz</a>")
                        .Append("</div></div>");
                }
            }

            sb.Append("</div></section></div>").Append(rail).Append("</div></div>");
            return sb.ToString();
        }

        private string GetDashboardDisplayName(int userId, out bool hasError)
        {
            QueryResult userResult = MindWellServices.TryQuery("SELECT TOP 1 FirstName, LastName, Username FROM Users WHERE UserID=@UserID", new SqlParameter("@UserID", userId));
            hasError = userResult.HasError;
            if (hasError)
                return "Learner";

            DataTable user = userResult.Data;
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
