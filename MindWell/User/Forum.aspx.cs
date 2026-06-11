// Purpose: User forum page for creating discussion topics and browsing recent module discussions.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class Forum : UserPageBase
    {
        protected string RenderForum()
        {
            // Topic creation captures the current user and selected module before refreshing the topic list.
            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            string message = Request.QueryString["invalidInput"] == "1" ? "<div class='alert alert-danger'>" + SecurityHelper.PlainTextValidationMessage + "</div>" : string.Empty;
            if (IsPostBack)
            {
                var form = Request.Unvalidated.Form;
                int moduleId = 0;
                if (!string.IsNullOrWhiteSpace(form["moduleId"]) && !SecurityHelper.TryGetPositiveIntForm("moduleId", out moduleId))
                    message = "<div class='alert alert-danger'>Invalid module selected.</div>";
                string title = SecurityHelper.SafeTrim(form["title"], 300);
                string content = SecurityHelper.SafeTrim(form["content"], 5000);
                if (string.IsNullOrEmpty(message) && (SecurityHelper.IsBlankAfterTrim(title) || SecurityHelper.IsBlankAfterTrim(content)))
                    message = "<div class='alert alert-danger'>Topic title and message are required.</div>";
                else if (string.IsNullOrEmpty(message) && (!SecurityHelper.IsPlainTextSafe(title) || !SecurityHelper.IsPlainTextSafe(content)))
                    message = "<div class='alert alert-danger'>" + SecurityHelper.PlainTextValidationMessage + "</div>";
                else if (string.IsNullOrEmpty(message))
                {
                    object moduleValue = moduleId > 0 ? (object)moduleId : DBNull.Value;
                    MindWellServices.NonQuery("INSERT INTO ForumTopics (ModuleID, UserID, Title, Content, ViewCount, CreatedDate) VALUES (@ModuleID,@UserID,@Title,@Content,0,GETDATE())", new SqlParameter("@ModuleID", moduleValue), new SqlParameter("@UserID", userId), new SqlParameter("@Title", title), new SqlParameter("@Content", content));
                    message = "<div class='alert alert-success'>Topic posted.</div>";
                }
            }
            DataTable topics = MindWellServices.Query(@"SELECT t.TopicID, t.Title, t.CreatedDate, u.Username, ISNULL(lm.ModuleName, 'General Discussion') AS ModuleName FROM ForumTopics t INNER JOIN Users u ON t.UserID=u.UserID LEFT JOIN LearningModules lm ON t.ModuleID=lm.ModuleID WHERE " + MindWellServices.ExcludeTestDataWhere("u.Username", "u.Email") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(t.Title, '')") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(t.Content, '')") + " AND (t.ModuleID IS NULL OR " + MindWellServices.ActiveModuleWhere + ") ORDER BY t.CreatedDate DESC");
            var sb = new StringBuilder("<section class='card'><h1>Forum</h1>" + message + "<p>Choose an optional module, or leave the topic under General Discussion.</p><div class='form-group'><label>Module <span class='content-kicker'>(optional)</span></label><select name='moduleId'><option value=''>General Discussion</option>")
                .Append(MindWellServices.RenderModuleOptions(0))
                .Append("</select></div><div class='form-group'><label>Topic title</label><input name='title' required /></div><div class='form-group'><label>Message</label><textarea name='content' required></textarea></div><button class='btn btn-primary'>Post Topic</button></section><section class='card'><h2>Discussion Topics</h2><table class='data-table'><tr><th>Topic</th><th>Module</th><th>By</th><th>Date</th></tr>");
            foreach (DataRow row in topics.Rows)
                sb.Append("<tr><td><a href='ForumTopic.aspx?TopicID=").Append(row["TopicID"]).Append("'>").Append(MindWellServices.Encode(row["Title"])).Append("</a></td><td>").Append(MindWellServices.Encode(row["ModuleName"])).Append("</td><td>").Append(MindWellServices.Encode(row["Username"])).Append("</td><td>").Append(MindWellServices.Encode(row["CreatedDate"])).Append("</td></tr>");
            return sb.Append("</table></section>").ToString();
        }
    }
}
