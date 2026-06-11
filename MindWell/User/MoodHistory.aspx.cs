// Purpose: User mood-history page for reviewing saved mood entries in reverse chronological order.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class MoodHistory : UserPageBase
    {
        protected string RenderMoodHistory()
        {
            // Loads only the current user's entries so wellbeing notes remain private to that account.
            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            DataTable moods = MindWellServices.Query("SELECT CONVERT(varchar(10), EntryDate, 120) AS EntryDate, MoodLevel, Notes FROM MoodEntries WHERE UserID=@UserID AND " + MindWellServices.CleanDemoTextWhere("ISNULL(Notes, '')") + " ORDER BY EntryDate DESC", new SqlParameter("@UserID", userId));
            var sb = new StringBuilder("<section class='section-header'><h1>Mood History</h1><p>Review your previous mood entries.</p><p class='page-actions'><a class='btn btn-outline btn-sm' href='MoodTracker.aspx'>Back to Mood Tracker</a></p></section>");
            if (moods.Rows.Count == 0)
                return sb.Append("<section class='card empty-state'><h2>No mood entries yet</h2><p>Save your first mood entry to build a private wellbeing timeline.</p></section>").ToString();

            sb.Append("<section class='mood-timeline'>");
            foreach (DataRow row in moods.Rows)
            {
                int mood = MindWellServices.ToInt(row["MoodLevel"], 3);
                sb.Append("<article class='card mood-history-card'><div><p class='eyebrow'>")
                    .Append(MindWellServices.Encode(row["EntryDate"]))
                    .Append("</p><h2>").Append(MoodLabel(mood)).Append("</h2></div>")
                    .Append("<span class='mood-score-pill'>").Append(mood).Append("/5</span>")
                    .Append("<p class='mood-note'>").Append(MindWellServices.Encode(row["Notes"])).Append("</p></article>");
            }

            return sb.Append("</section>").ToString();
        }

        private static string MoodLabel(int value)
        {
            switch (value)
            {
                case 1: return "Very low";
                case 2: return "Low";
                case 4: return "Good";
                case 5: return "Great";
                default: return "Okay";
            }
        }

    }
}
