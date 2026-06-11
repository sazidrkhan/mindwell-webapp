// Purpose: User forum-topic page for reading a discussion thread and posting replies.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class ForumTopic : UserPageBase
    {
        protected string RenderForumTopic()
        {
            // Topic ID can come from either the query string or hidden reply form field.
            int topicId;
            if (!ResolveTopicId(out topicId))
                return "<section class='card'><h1>Topic not found</h1></section>";

            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            string message = Request.QueryString["invalidInput"] == "1" ? "<div class='alert alert-danger'>" + SecurityHelper.PlainTextValidationMessage + "</div>" : string.Empty;
            if (IsPostBack && topicId > 0)
            {
                string reply = SecurityHelper.SafeTrim(Request.Unvalidated.Form["reply"], 5000);
                if (SecurityHelper.IsBlankAfterTrim(reply))
                    message = "<div class='alert alert-danger'>Reply is required.</div>";
                else if (!SecurityHelper.IsPlainTextSafe(reply))
                    message = "<div class='alert alert-danger'>" + SecurityHelper.PlainTextValidationMessage + "</div>";
                else
                {
                    MindWellServices.NonQuery("INSERT INTO ForumReplies (TopicID, UserID, Content, CreatedDate) VALUES (@TopicID,@UserID,@Content,GETDATE())", new SqlParameter("@TopicID", topicId), new SqlParameter("@UserID", userId), new SqlParameter("@Content", reply));
                    message = "<div class='alert alert-success'>Reply posted.</div>";
                }
            }
            DataTable topic = MindWellServices.Query(@"SELECT t.Title, t.Content, t.CreatedDate, u.Username, ISNULL(lm.ModuleName, 'General Discussion') AS ModuleName FROM ForumTopics t INNER JOIN Users u ON t.UserID=u.UserID LEFT JOIN LearningModules lm ON t.ModuleID=lm.ModuleID WHERE t.TopicID=@TopicID AND " + MindWellServices.ExcludeTestDataWhere("u.Username", "u.Email") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(t.Title, '')") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(t.Content, '')") + " AND (t.ModuleID IS NULL OR " + MindWellServices.ActiveModuleWhere + ")", new SqlParameter("@TopicID", topicId));
            if (topic.Rows.Count == 0) return "<section class='card'><h1>Topic not found</h1></section>";
            DataTable replies = MindWellServices.Query(@"SELECT r.Content, r.CreatedDate, u.Username FROM ForumReplies r INNER JOIN Users u ON r.UserID=u.UserID WHERE r.TopicID=@TopicID AND " + MindWellServices.ExcludeTestDataWhere("u.Username", "u.Email") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(r.Content, '')") + " ORDER BY r.CreatedDate", new SqlParameter("@TopicID", topicId));
            // Replies are rendered chronologically so the conversation reads from oldest to newest.
            DataRow t = topic.Rows[0];
            var sb = new StringBuilder("<p class='back-link'><a class='btn btn-outline btn-sm' href='Forum.aspx'>Back to Forum</a></p><section class='card topic-card'><p class='content-kicker'>");
            sb.Append(MindWellServices.Encode(t["ModuleName"])).Append("</p><h1>").Append(MindWellServices.Encode(t["Title"])).Append("</h1><p>").Append(MindWellServices.Encode(t["Content"])).Append("</p><small>By ").Append(MindWellServices.Encode(t["Username"])).Append(" on ").Append(MindWellServices.Encode(t["CreatedDate"])).Append("</small></section><section class='card'><h2>Replies</h2>").Append(message);
            foreach (DataRow r in replies.Rows) sb.Append("<article class='reply-card'><p>").Append(MindWellServices.Encode(r["Content"])).Append("</p><small>").Append(MindWellServices.Encode(r["Username"])).Append(" - ").Append(MindWellServices.Encode(r["CreatedDate"])).Append("</small></article>");
            return sb.Append("<h3>Post Reply</h3><input type='hidden' name='topicId' value='").Append(topicId).Append("' /><textarea name='reply' required></textarea><p class='page-actions'><button class='btn btn-primary'>Reply</button></p></section>").ToString();
        }

        private bool ResolveTopicId(out int topicId)
        {
            topicId = 0;
            if (Request.QueryString["TopicID"] != null)
                return SecurityHelper.TryGetPositiveIntQuery("TopicID", out topicId);

            if (IsPostBack && Request.Unvalidated.Form["topicId"] != null)
                return SecurityHelper.TryGetPositiveIntForm("topicId", out topicId);

            return false;
        }
    }
}
