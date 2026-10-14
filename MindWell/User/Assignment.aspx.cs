// Purpose: User activity-submission page for module-linked reflection assignments.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class Assignment : UserPageBase
    {
        protected string RenderAssignment()
        {
            // Stores the submitted activity response and lists recent submissions for confirmation/history.
            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            int moduleFilter = 0;
            string message = Request.QueryString["invalidInput"] == "1" ? "<div id='assignmentMessage' class='alert alert-danger' role='alert'>" + SecurityHelper.PlainTextValidationMessage + "</div>" : string.Empty;
            if (Request.QueryString["ModuleID"] != null && !SecurityHelper.TryGetPositiveIntQuery("ModuleID", out moduleFilter))
                message = "<div id='assignmentMessage' class='alert alert-danger' role='alert'>Invalid module selected.</div>";

            if (IsPostBack)
            {
                int activityId;
                string response = SecurityHelper.SafeTrim(Request.Unvalidated.Form["response"], 5000);
                if (!SecurityHelper.TryGetPositiveIntForm("activityId", out activityId))
                {
                    message = "<div id='assignmentMessage' class='alert alert-danger' role='alert'>Please choose a guided activity.</div>";
                }
                else
                {
                    DataTable activity = MindWellServices.Query("SELECT TOP 1 ActivityID, ModuleID, Title FROM Activities WHERE ActivityID=@ActivityID AND IsPublished=1", new SqlParameter("@ActivityID", activityId));
                    if (activity.Rows.Count == 0)
                    {
                        message = "<div id='assignmentMessage' class='alert alert-danger' role='alert'>Please choose a guided activity.</div>";
                    }
                    else if (SecurityHelper.IsBlankAfterTrim(response))
                    {
                        message = "<div id='assignmentMessage' class='alert alert-danger' role='alert'>Response is required.</div>";
                    }
                    else if (!SecurityHelper.IsPlainTextSafe(response))
                    {
                        message = "<div id='assignmentMessage' class='alert alert-danger' role='alert'>" + SecurityHelper.PlainTextValidationMessage + "</div>";
                    }
                    else
                    {
                        DataRow selected = activity.Rows[0];
                        MindWellServices.NonQuery("INSERT INTO ActivitySubmissions (UserID, ActivityID, ModuleID, ActivityTitle, ResponseText, SubmittedDate) VALUES (@UserID,@ActivityID,@ModuleID,@Title,@Response,GETDATE())", new SqlParameter("@UserID", userId), new SqlParameter("@ActivityID", activityId), new SqlParameter("@ModuleID", MindWellServices.ToInt(selected["ModuleID"], 0)), new SqlParameter("@Title", SecurityHelper.SafeTrim(Convert.ToString(selected["Title"]), 200)), new SqlParameter("@Response", response));
                        message = "<div class='alert alert-success'>Activity submitted.</div>";
                    }
                }
            }
            DataTable activities = MindWellServices.Query(@"SELECT a.ActivityID, a.Title, a.Prompt, a.ModuleID, lm.ModuleName FROM Activities a INNER JOIN LearningModules lm ON a.ModuleID=lm.ModuleID WHERE a.IsPublished=1 AND " + MindWellServices.ActiveModuleWhere + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(a.Title, '')") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(a.Prompt, '')") + " AND (@ModuleID=0 OR a.ModuleID=@ModuleID) ORDER BY lm.OrderIndex, a.SortOrder, a.Title", new SqlParameter("@ModuleID", moduleFilter));
            DataTable submissions = MindWellServices.Query("SELECT TOP 10 ActivityTitle, SubmittedDate FROM ActivitySubmissions WHERE UserID=@UserID AND " + MindWellServices.CleanDemoTextWhere("ISNULL(ActivityTitle, '')") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(ResponseText, '')") + " ORDER BY SubmittedDate DESC", new SqlParameter("@UserID", userId));
            string moduleBackLink = moduleFilter > 0
                ? "<p class='back-link'><a class='btn btn-outline btn-sm' href='../Public/ModuleDetails.aspx?ModuleID=" + moduleFilter + "'>Back to Module</a></p>"
                : string.Empty;
            var sb = new StringBuilder(moduleBackLink + "<section class='card'><h1>Activity Submission</h1>" + message + "<p>Complete a seeded guided activity and submit your reflection or answer.</p><div class='row cols-2 activity-selector'>");
            if (activities.Rows.Count == 0)
                sb.Append("<div class='empty-state'><p>No guided activities are available yet.</p></div>");

            foreach (DataRow activity in activities.Rows)
            {
                string activityId = MindWellServices.Encode(activity["ActivityID"]);
                sb.Append("<label for='activity-").Append(activityId).Append("' class='guided-activity-card'><input id='activity-").Append(activityId).Append("' type='radio' name='activityId' value='").Append(activity["ActivityID"]).Append("' autocomplete='off' /> <span class='content-kicker'>")
                    .Append(MindWellServices.Encode(activity["ModuleName"])).Append("</span><strong>").Append(MindWellServices.Encode(activity["Title"])).Append("</strong><span>Prompt: ")
                    .Append(MindWellServices.Encode(activity["Prompt"])).Append("</span></label>");
            }

            sb.Append("</div><p id='assignmentPrivacy' class='form-helper'>Drafts are not stored in browser storage.</p><div class='form-group'><label for='assignmentResponse'>Your response</label><textarea id='assignmentResponse' name='response' rows='6' autocomplete='off' aria-describedby='assignmentPrivacy'></textarea></div><button class='btn btn-primary'>Submit Activity</button></section><section class='card'><h2>Recent Submissions</h2>");
            if (submissions.Rows.Count == 0)
                sb.Append("<div class='empty-state'><p>No activity submissions yet.</p></div>");

            foreach (DataRow row in submissions.Rows)
                sb.Append("<article class='submission-card'><strong>").Append(MindWellServices.Encode(row["ActivityTitle"])).Append("</strong><p>").Append(MindWellServices.Encode(row["SubmittedDate"])).Append("</p></article>");

            return sb.Append("</section>").ToString();
        }
    }
}
