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
            string message = Request.QueryString["invalidInput"] == "1" ? "<div id='forumMessage' class='alert alert-danger' role='alert'>" + SecurityHelper.PlainTextValidationMessage + "</div>" : string.Empty;
            if (IsPostBack)
            {
                var form = Request.Unvalidated.Form;
                int moduleId = 0;
                if (!string.IsNullOrWhiteSpace(form["moduleId"]) && !SecurityHelper.TryGetPositiveIntForm("moduleId", out moduleId))
                    message = "<div id='forumMessage' class='alert alert-danger' role='alert'>Invalid module selected.</div>";
                string title = SecurityHelper.SafeTrim(form["title"], 300);
                string content = SecurityHelper.SafeTrim(form["content"], 5000);
                if (string.IsNullOrEmpty(message) && (SecurityHelper.IsBlankAfterTrim(title) || SecurityHelper.IsBlankAfterTrim(content)))
                    message = "<div id='forumMessage' class='alert alert-danger' role='alert'>Topic title and message are required.</div>";
                else if (string.IsNullOrEmpty(message) && (!SecurityHelper.IsPlainTextSafe(title) || !SecurityHelper.IsPlainTextSafe(content)))
                    message = "<div id='forumMessage' class='alert alert-danger' role='alert'>" + SecurityHelper.PlainTextValidationMessage + "</div>";
                else if (string.IsNullOrEmpty(message))
                {
                    object moduleValue = moduleId > 0 ? (object)moduleId : DBNull.Value;
                    MindWellServices.NonQuery("INSERT INTO ForumTopics (ModuleID, UserID, Title, Content, ViewCount, CreatedDate) VALUES (@ModuleID,@UserID,@Title,@Content,0,GETDATE())", new SqlParameter("@ModuleID", moduleValue), new SqlParameter("@UserID", userId), new SqlParameter("@Title", title), new SqlParameter("@Content", content));
                    message = "<div id='forumMessage' class='alert alert-success' role='status'>Topic posted.</div>";
                }
            }
            QueryResult topicsResult = MindWellServices.TryQuery(@"SELECT t.TopicID, t.Title, t.CreatedDate, u.Username, ISNULL(lm.ModuleName, 'General Discussion') AS ModuleName FROM ForumTopics t INNER JOIN Users u ON t.UserID=u.UserID LEFT JOIN LearningModules lm ON t.ModuleID=lm.ModuleID WHERE " + MindWellServices.ExcludeTestDataWhere("u.Username", "u.Email") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(t.Title, '')") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(t.Content, '')") + " AND (t.ModuleID IS NULL OR " + MindWellServices.ActiveModuleWhere + ") ORDER BY t.CreatedDate DESC");
            if (topicsResult.HasError)
                return message + MindWellServices.RenderDataUnavailable("Forum topics are temporarily unavailable.");

            DataTable topics = topicsResult.Data;
            QueryResult moduleOptionsResult = MindWellServices.TryGetModuleOptions();
            if (moduleOptionsResult.HasError)
                return message + MindWellServices.RenderDataUnavailable("Learning content is temporarily unavailable: forum module options could not be loaded.");

            DataTable moduleOptions = moduleOptionsResult.Data;
            var sb = new StringBuilder("<section class='card'><h1>Forum</h1>" + message + "<p id='forumModuleHelp'>Choose an optional module, or leave the topic under General Discussion.</p><div class='form-group'><label for='forumModule'>Module <span class='content-kicker'>(optional)</span></label><select id='forumModule' name='moduleId' autocomplete='off' aria-describedby='forumModuleHelp'><option value=''>General Discussion</option>")
                .Append(MindWellServices.RenderModuleOptions(moduleOptions, 0))
                .Append("</select></div><div class='form-group'><label for='forumTitle'>Topic title</label><input id='forumTitle' name='title' autocomplete='off' required /></div><div class='form-group'><label for='forumContent'>Message</label><textarea id='forumContent' name='content' autocomplete='off' required></textarea></div><button class='btn btn-primary'>Post Topic</button></section><section class='card'><h2>Discussion Topics</h2><table class='data-table'><tr><th>Topic</th><th>Module</th><th>By</th><th>Date</th></tr>");
            foreach (DataRow row in topics.Rows)
                sb.Append("<tr><td><a href='ForumTopic.aspx?TopicID=").Append(row["TopicID"]).Append("'>").Append(MindWellServices.Encode(row["Title"])).Append("</a></td><td>").Append(MindWellServices.Encode(row["ModuleName"])).Append("</td><td>").Append(MindWellServices.Encode(row["Username"])).Append("</td><td>").Append(MindWellServices.Encode(row["CreatedDate"])).Append("</td></tr>");
            return sb.Append("</table></section>").ToString();
        }
    }
}
