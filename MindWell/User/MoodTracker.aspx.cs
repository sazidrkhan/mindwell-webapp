// Purpose: User mood tracker for saving daily mood entries and rendering a chart of mood trends.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class MoodTracker : UserPageBase
    {
        protected string RenderMoodTracker()
        {
            // Saves or updates one mood entry per user/date, then reloads chart data for display.
            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            string message = string.Empty;
            string notes = string.Empty;
            int selectedMood = 3;
            if (IsPostBack && Request.Form["moodAction"] == "save")
            {
                DateTime date = DateTime.Today;
                int mood = MindWellServices.ToInt(Request.Form["moodLevel"], 3);
                if (mood < 1) mood = 1;
                if (mood > 5) mood = 5;
                selectedMood = mood;
                notes = SecurityHelper.SafeTrim(Request.Form["notes"], 500);
                if (!SecurityHelper.IsPlainTextSafe(notes))
                {
                    message = "<div class='alert alert-danger' role='alert'>" + SecurityHelper.PlainTextValidationMessage + "</div>";
                }
                else
                {
                    int saved = MindWellServices.NonQuery(@"IF EXISTS (SELECT 1 FROM MoodEntries WHERE UserID=@UserID AND EntryDate=@EntryDate) UPDATE MoodEntries SET MoodLevel=@MoodLevel, Notes=@Notes WHERE UserID=@UserID AND EntryDate=@EntryDate ELSE INSERT INTO MoodEntries (UserID, EntryDate, MoodLevel, Notes, CreatedDate) VALUES (@UserID, @EntryDate, @MoodLevel, @Notes, GETDATE())",
                        new SqlParameter("@UserID", userId), new SqlParameter("@EntryDate", date), new SqlParameter("@MoodLevel", mood), new SqlParameter("@Notes", notes));
                    if (saved > 0)
                    {
                        message = "<div class='alert alert-success' role='status'>Mood entry saved. Thank you for checking in with yourself.</div>";
                        notes = string.Empty;
                    }
                    else
                        message = "<div class='alert alert-danger' role='alert'>We could not save your mood entry. Your notes are still here; please try again.</div>";
                }
            }

            QueryResult moodsResult = MindWellServices.TryQuery("SELECT CONVERT(varchar(10), EntryDate, 120) AS EntryDate, MoodLevel FROM MoodEntries WHERE UserID=@UserID ORDER BY EntryDate", new SqlParameter("@UserID", userId));
            DataTable moods = moodsResult.Data;
            string json = MindWellServices.JsonArray(moods, "EntryDate", "MoodLevel");
            int count = moods.Rows.Count;
            int latest = count == 0 ? selectedMood : MindWellServices.ToInt(moods.Rows[count - 1]["MoodLevel"], selectedMood);
            int total = 0;
            foreach (DataRow row in moods.Rows)
                total += MindWellServices.ToInt(row["MoodLevel"], 0);
            string average = count == 0 ? "--" : ((decimal)total / count).ToString("0.0");

            var sb = new StringBuilder();
            sb.Append("<section class='section-header'><h1>How are we feeling today?</h1><p>Log today&apos;s mood and notice patterns over time.</p></section>")
                .Append("<section class='card mood-tracker-card'>").Append(message)
                .Append("<input type='hidden' name='moodAction' value='save' />")
                .Append("<p class='mood-current-date'>Recording for: <strong>").Append(DateTime.Today.ToString("dddd, d MMMM yyyy")).Append("</strong></p>")
                .Append("<fieldset class='mood-option-grid'><legend>How are you feeling today?</legend>");
            AppendMoodOption(sb, 1, "Very low", selectedMood);
            AppendMoodOption(sb, 2, "Low", selectedMood);
            AppendMoodOption(sb, 3, "Okay", selectedMood);
            AppendMoodOption(sb, 4, "Good", selectedMood);
            AppendMoodOption(sb, 5, "Great", selectedMood);
            sb.Append("</fieldset>")
                .Append("<p class='mood-selection-note' data-mood-note role='status'>Every feeling has a place here.</p>")
                .Append("<div class='form-group'><label for='moodNotes'>Anything on your mind? <small>(optional)</small></label><textarea id='moodNotes' name='notes' maxlength='500' rows='3' autocomplete='off' aria-describedby='moodNotesHelp' placeholder='What influenced your mood today?'>").Append(MindWellServices.Encode(notes)).Append("</textarea><small id='moodNotesHelp' class='form-helper'>A short wellbeing insight helps make your trend easier to understand. Drafts stay on this page until you save.</small></div>")
                .Append("<div class='button-row'><button class='btn btn-primary' type='submit'>Save Mood</button><a class='btn btn-outline' href='MoodHistory.aspx'>View Mood History</a></div>")
                .Append("</section>");
            if (moodsResult.HasError)
                return sb.Append(MindWellServices.RenderDataUnavailable("Your saved mood entries could not be loaded. Please try again later.")).ToString();
            sb.Append("<section class='row cols-3'>")
                .Append("<article class='card mood-summary-card'><h2>Latest</h2><p class='stat-number'>").Append(count == 0 ? "—" : latest + "/5").Append("</p><p>").Append(count == 0 ? "No entries yet" : MoodLabel(latest)).Append("</p></article>")
                .Append("<article class='card mood-summary-card'><h2>Average</h2><p class='stat-number'>").Append(average).Append("</p><p>Across saved entries</p></article>")
                .Append("<article class='card mood-summary-card'><h2>Entries</h2><p class='stat-number'>").Append(count).Append("</p><p>Your private mood log</p></article>")
                .Append("</section>")
                .Append("<section class='card mood-chart-card'><h2>Mood Trend</h2><p class='form-helper'>A little perspective on your days. Every feeling counts.</p><div class='chart-filter' role='group' aria-label='Mood chart range'><button type='button' class='chart-filter-btn chart-filter-btn--active' data-range='weekly' aria-pressed='true'>Weekly</button><button type='button' class='chart-filter-btn' data-range='monthly' aria-pressed='false'>Monthly</button></div><p id='moodChartEmpty' hidden>No mood entries in this range yet.</p><p id='moodChartSummary' class='form-helper' role='status'></p><div class='mood-chart-wrap'><canvas id='moodChart' role='img' aria-label='Saved mood entries over time' aria-describedby='moodChartSummary'></canvas></div><details class='mood-data-table'><summary>View saved entries as a table</summary><table><caption class='sr-only'>All saved mood entries</caption><thead><tr><th scope='col'>Date</th><th scope='col'>Mood</th></tr></thead><tbody>");
            foreach (DataRow row in moods.Rows)
                sb.Append("<tr><td>").Append(MindWellServices.Encode(row["EntryDate"])).Append("</td><td>").Append(MoodLabel(MindWellServices.ToInt(row["MoodLevel"], 3))).Append("</td></tr>");
            sb.Append("</tbody></table></details></section><script id='mood-data' type='application/json'>").Append(json).Append("</script>");
            return sb.ToString();
        }

        private static void AppendMoodOption(StringBuilder sb, int value, string label, int selectedMood)
        {
            sb.Append("<label class='mood-option-card'><input type='radio' name='moodLevel' value='").Append(value).Append("'");
            if (value == selectedMood)
                sb.Append(" checked");
            sb.Append(" /><span class='mood-emoji'>").Append(WellbeingUi.MoodFace(value)).Append("</span><strong>").Append(label).Append("</strong><small>").Append(value).Append("/5</small></label>");
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
