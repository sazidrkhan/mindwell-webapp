// Purpose: Admin feedback page for reviewing contact messages and marking them as resolved.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.Admin
{
    public partial class ManageFeedback : AdminPageBase
    {
        private static readonly string ExcludeTestDataWhere = MindWellServices.CleanDemoTextWhere("Name") + " AND " + MindWellServices.CleanDemoTextWhere("Email") + " AND Email NOT LIKE '%@example.test' AND Email NOT LIKE 'deepauth%' AND Email NOT LIKE 'octest%' AND Email NOT LIKE 'qa%'";

        protected string RenderManageFeedback()
        {
            // Status actions update the message state before the current inbox is rendered.
            string message = string.Empty;
            var form = Request.Unvalidated.Form;
            if (IsPostBack && form["feedbackAction"] != null && form["feedbackAction"].StartsWith("save:", StringComparison.Ordinal))
            {
                int contactId = MindWellServices.ToInt(form["feedbackAction"].Split(':')[1], 0);
                string suffix = "_" + contactId;
                string status = NormalizeStatus(form["Status" + suffix]);
                bool resolved = status == "Resolved" || status == "Closed";
                MindWellServices.NonQuery(@"UPDATE Contacts SET Status=@Status, AdminNotes=@AdminNotes, IsResolved=@IsResolved, UpdatedDate=GETDATE(), ResolvedDate=CASE WHEN @IsResolved=1 THEN GETDATE() ELSE NULL END WHERE ContactID=@ContactID",
                    new SqlParameter("@Status", status),
                    new SqlParameter("@AdminNotes", SecurityHelper.SafeTrim(form["AdminNotes" + suffix], 1000)),
                    new SqlParameter("@IsResolved", resolved),
                    new SqlParameter("@ContactID", contactId));
                message = "<div class='alert alert-success'>Feedback updated.</div>";
            }

            string feedbackStatus = SecurityHelper.SafeTrim(Request.QueryString["feedbackStatus"], 20);
            string feedbackQuery = SecurityHelper.SafeTrim(Request.QueryString["feedbackQuery"], 100);
            DataTable contacts = MindWellServices.Query(@"
SELECT ContactID, Name, Email, Subject, Message, IsResolved, Status, AdminNotes, CreatedDate, UpdatedDate
FROM Contacts
WHERE " + ExcludeTestDataWhere + @" AND (@Status='' OR Status=@Status) AND (@Query='' OR Name LIKE '%' + @Query + '%' OR Email LIKE '%' + @Query + '%' OR Subject LIKE '%' + @Query + '%' OR Message LIKE '%' + @Query + '%' OR AdminNotes LIKE '%' + @Query + '%')
ORDER BY CreatedDate DESC", new SqlParameter("@Status", feedbackStatus), new SqlParameter("@Query", feedbackQuery));
            var sb = new StringBuilder("<p class='back-link'><a class='btn btn-outline btn-sm' href='Dashboard.aspx'>Back to Admin Dashboard</a></p><section class='section-header'><h1>Manage Feedback / Contact Messages</h1><p>Search contact messages, password reset requests, and admin follow-up status.</p></section>")
                .Append(message)
                .Append("<section class='card'><h2>Filters</h2><div class='admin-filter-grid'><label>Status<select id='feedbackStatus' name='feedbackStatus'>")
                .Append(RenderFeedbackStatusFilterOptions(feedbackStatus))
                .Append("</select></label><label>Search<input id='feedbackQuery' name='feedbackQuery' value='")
                .Append(MindWellServices.Encode(feedbackQuery))
                .Append("' /></label><button type='button' class='btn btn-primary' onclick=\"window.location='ManageFeedback.aspx?feedbackStatus='+encodeURIComponent(document.getElementById('feedbackStatus').value)+'&feedbackQuery='+encodeURIComponent(document.getElementById('feedbackQuery').value)\">Apply Filters</button></div><p>Use search for names, emails, subjects, messages, admin notes, or password reset request text.</p></section><section class='card'><div class='table-responsive'><table class='data-table manage-feedback-table'><tr><th>Name / Email</th><th>Subject / Message</th><th>Status</th><th>Admin Notes</th><th>Action</th></tr>");
            foreach (DataRow row in contacts.Rows)
            {
                int contactId = MindWellServices.ToInt(row["ContactID"], 0);
                sb.Append("<tr><td><strong>").Append(MindWellServices.Encode(row["Name"])).Append("</strong><span class='form-helper'>").Append(MindWellServices.Encode(row["Email"])).Append("</span></td><td><strong>").Append(MindWellServices.Encode(row["Subject"])).Append("</strong><p>").Append(MindWellServices.Encode(row["Message"])).Append("</p></td><td><select name='Status_").Append(contactId).Append("'>")
                    .Append(RenderStatusOptions(Convert.ToString(row["Status"]))).Append("</select></td><td><textarea name='AdminNotes_").Append(contactId).Append("' rows='4'>").Append(MindWellServices.Encode(row["AdminNotes"])).Append("</textarea></td><td><button class='btn btn-primary btn-sm' name='feedbackAction' value='save:").Append(contactId).Append("'>Save</button></td></tr>");
            }
            return sb.Append("</table></div></section>").ToString();
        }

        private string NormalizeStatus(string status)
        {
            status = SecurityHelper.SafeTrim(status, 20);
            if (status == "In Progress" || status == "Resolved" || status == "Reopened" || status == "Closed")
                return status;

            return "New";
        }

        private string RenderStatusOptions(string selected)
        {
            string[] statuses = { "New", "In Progress", "Resolved", "Reopened", "Closed" };
            var sb = new StringBuilder();
            foreach (string status in statuses)
            {
                sb.Append("<option");
                if (string.Equals(status, selected, StringComparison.OrdinalIgnoreCase))
                    sb.Append(" selected");
                sb.Append(">").Append(status).Append("</option>");
            }

            return sb.ToString();
        }

        private string RenderFeedbackStatusFilterOptions(string selected)
        {
            string[] statuses = { "", "New", "In Progress", "Resolved", "Reopened", "Closed" };
            var sb = new StringBuilder();
            foreach (string status in statuses)
            {
                string label = string.IsNullOrEmpty(status) ? "All" : status;
                sb.Append("<option value='").Append(MindWellServices.Encode(status)).Append("'");
                if (string.Equals(status, selected, StringComparison.OrdinalIgnoreCase))
                    sb.Append(" selected");
                sb.Append(">").Append(label).Append("</option>");
            }

            return sb.ToString();
        }
    }
}
