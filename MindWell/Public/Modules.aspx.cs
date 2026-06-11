// Purpose: Public module catalogue showing available learning topics and role-aware calls to action.
using System;
using System.Data;
using System.Text;

namespace MindWell.Public
{
    public partial class Modules : System.Web.UI.Page
    {
        protected string RenderModules()
        {
            // Public visitors preview modules; signed-in users continue into the same module detail flow.
            DataTable modules = MindWellServices.Query(@"
SELECT lm.ModuleID,
       lm.ModuleName,
       lm.Description,
       lm.LearningOutcomes,
       lm.EstimatedMinutes,
       (SELECT COUNT(*) FROM Articles a WHERE a.ModuleID=lm.ModuleID AND a.IsPublished=1) AS LessonCount,
       (SELECT COUNT(*) FROM Quizzes q WHERE q.ModuleID=lm.ModuleID AND q.IsPublished=1) AS QuizCount,
       (SELECT COUNT(*) FROM Activities act WHERE act.ModuleID=lm.ModuleID AND act.IsPublished=1) AS ActivityCount
FROM LearningModules lm
WHERE NULLIF(LTRIM(RTRIM(lm.ModuleName)), '') IS NOT NULL
  AND NULLIF(LTRIM(RTRIM(lm.Description)), '') IS NOT NULL
  AND lm.ModuleName NOT LIKE '[[]Archived] %'
  AND " + MindWellServices.CleanDemoTextWhere("lm.ModuleName") + @"
  AND " + MindWellServices.CleanDemoTextWhere("ISNULL(lm.Description, '')") + @"
ORDER BY lm.OrderIndex");
            bool authenticated = SecurityHelper.IsUserAuthenticated();
            string intro = authenticated
                ? "Continue your learning path across MindWell modules."
                : "Explore MindWell topics before creating an account or signing in.";
            string actionLabel = authenticated ? "Continue Module" : "View Details";

            var sb = new StringBuilder("<section class='section-header'><h1>Learning Modules</h1><p>")
                .Append(intro)
                .Append("</p></section><div class='row'>");
            if (modules.Rows.Count == 0)
                return MindWellServices.RenderDataUnavailable("Learning content is temporarily unavailable: learning modules returned no rows.");

            foreach (DataRow row in modules.Rows)
            {
                int minutes = MindWellServices.ToInt(row["EstimatedMinutes"], 0);
                int lessonCount = MindWellServices.ToInt(row["LessonCount"], 0);
                int quizCount = MindWellServices.ToInt(row["QuizCount"], 0);
                int activityCount = MindWellServices.ToInt(row["ActivityCount"], 0);
                sb.Append("<article class='card module-card'><p class='badge-row'><span class='access-badge'>").Append(authenticated ? "Member access" : "Preview").Append("</span>");
                if (minutes > 0)
                    sb.Append(" <span class='access-badge'>").Append(minutes).Append(" min</span>");

                sb.Append("</p><h2>").Append(MindWellServices.Encode(row["ModuleName"]))
                    .Append("</h2><p>").Append(MindWellServices.Encode(row["Description"]))
                    .Append("</p>").Append(MindWellServices.EncodeLinesAsList(Convert.ToString(row["LearningOutcomes"])))
                    .Append("<p class='badge-row'><span class='access-badge'>").Append(lessonCount).Append(lessonCount == 1 ? " lesson" : " lessons").Append("</span> ")
                    .Append("<span class='access-badge'>").Append(quizCount > 0 ? "Quiz available" : "Quiz pending").Append("</span> ")
                    .Append("<span class='access-badge'>").Append(activityCount > 0 ? "Activity available" : "Activity pending").Append("</span></p>");
                if (authenticated)
                {
                    sb.Append("<a class='btn btn-primary' href='ModuleDetails.aspx?ModuleID=").Append(row["ModuleID"]).Append("'>").Append(actionLabel).Append("</a></article>");
                }
                else
                {
                    sb.Append("<a class='btn btn-primary' href='ModuleDetails.aspx?ModuleID=").Append(row["ModuleID"]).Append("' data-requires-auth='true'>").Append(actionLabel).Append("</a></article>");
                }
            }
            sb.Append("</div>");
            if (!authenticated)
            {
                sb.Append(@"
<div id='moduleGateBackdrop' class='register-gate-backdrop' hidden></div>
<div id='moduleGateModal' class='register-gate-modal' role='dialog' aria-modal='true' aria-labelledby='moduleGateTitle' hidden>
  <p class='eyebrow'>Sign in to continue</p>
  <h2 id='moduleGateTitle'>Sign in to open this module</h2>
  <p>View full module details, lessons, quizzes, and guided activities after signing in.</p>
  <div class='button-row'>
    <a class='btn btn-primary' href='../Auth/Login.aspx' data-login-return='true'>Sign In</a>
    <button type='button' class='btn btn-ghost' data-module-gate-close>Cancel</button>
  </div>
</div>");
            }

            return sb.ToString();
        }
    }
}
