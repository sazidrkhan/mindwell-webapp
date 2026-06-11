// Purpose: User reflection journal for saving private text entries and listing recent reflections.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class Journal : UserPageBase
    {
        protected string RenderJournal()
        {
            // New reflections are inserted on postback, then the latest entries are displayed below the form.
            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            string message = Request.QueryString["invalidInput"] == "1" ? "<div class='alert alert-danger'>" + SecurityHelper.PlainTextValidationMessage + "</div>" : string.Empty;
            if (IsPostBack)
            {
                var form = Request.Unvalidated.Form;
                string title = SecurityHelper.SafeTrim(form["title"], 200);
                string reflection = SecurityHelper.SafeTrim(form["reflection"], 5000);
                if (SecurityHelper.IsBlankAfterTrim(title) || SecurityHelper.IsBlankAfterTrim(reflection))
                {
                    message = "<div class='alert alert-danger'>Title and reflection are required.</div>";
                }
                else if (!SecurityHelper.IsPlainTextSafe(title) || !SecurityHelper.IsPlainTextSafe(reflection))
                {
                    message = "<div class='alert alert-danger'>" + SecurityHelper.PlainTextValidationMessage + "</div>";
                }
                else
                {
                    MindWellServices.NonQuery("INSERT INTO JournalEntries (UserID, Title, ReflectionText, CreatedDate) VALUES (@UserID,@Title,@Text,GETDATE())", new SqlParameter("@UserID", userId), new SqlParameter("@Title", title), new SqlParameter("@Text", reflection));
                    message = "<div class='alert alert-success'>Reflection saved.</div>";
                }
            }
            DataTable entries = MindWellServices.Query("SELECT TOP 10 Title, ReflectionText, CreatedDate FROM JournalEntries WHERE UserID=@UserID AND " + MindWellServices.CleanDemoTextWhere("ISNULL(Title, '')") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(ReflectionText, '')") + " ORDER BY CreatedDate DESC", new SqlParameter("@UserID", userId));
            var sb = new StringBuilder("<section class='card'><h1>Reflection Journal</h1>" + message + "<div class='form-group'><label>Title</label><input name='title' required /></div><div class='form-group'><label>Reflection</label><textarea name='reflection' rows='6' required></textarea></div><button class='btn btn-primary'>Save Reflection</button></section><section class='card'><h2>Recent Reflections</h2>");
            if (entries.Rows.Count == 0)
                sb.Append("<div class='empty-state'><p>No reflections saved yet.</p></div>");

            foreach (DataRow row in entries.Rows)
            {
                string formattedDate = Convert.ToDateTime(row["CreatedDate"]).ToString("d MMM yyyy, h:mm tt");
                sb.Append("<article class='card journal-entry-card'>")
                    .Append("<header class='journal-entry-header'><h3>").Append(MindWellServices.Encode(row["Title"]))
                    .Append("</h3><time class='journal-entry-date'>").Append(MindWellServices.Encode(formattedDate))
                    .Append("</time></header><p class='journal-entry-body'>")
                    .Append(MindWellServices.Encode(row["ReflectionText"]))
                    .Append("</p></article>");
            }
            return sb.Append("</section>").ToString();
        }

    }
}
