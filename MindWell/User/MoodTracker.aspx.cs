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
            int selectedMood = 3;
            if (IsPostBack && Request.Form["moodAction"] == "save")
            {
                DateTime date = DateTime.Today;
                int mood = MindWellServices.ToInt(Request.Form["moodLevel"], 3);
                if (mood < 1) mood = 1;
                if (mood > 5) mood = 5;
                selectedMood = mood;
                string notes = SecurityHelper.SafeTrim(Request.Form["notes"], 500);
                if (!SecurityHelper.IsPlainTextSafe(notes))
                {
                    message = "<div class='alert alert-danger'>" + SecurityHelper.PlainTextValidationMessage + "</div>";
                }
                else
                {
                    MindWellServices.NonQuery(@"IF EXISTS (SELECT 1 FROM MoodEntries WHERE UserID=@UserID AND EntryDate=@EntryDate) UPDATE MoodEntries SET MoodLevel=@MoodLevel, Notes=@Notes WHERE UserID=@UserID AND EntryDate=@EntryDate ELSE INSERT INTO MoodEntries (UserID, EntryDate, MoodLevel, Notes, CreatedDate) VALUES (@UserID, @EntryDate, @MoodLevel, @Notes, GETDATE())",
                        new SqlParameter("@UserID", userId), new SqlParameter("@EntryDate", date), new SqlParameter("@MoodLevel", mood), new SqlParameter("@Notes", notes));
                    message = "<div class='alert alert-success'>Mood entry saved.</div>";
                }
            }

            DataTable moods = MindWellServices.Query("SELECT CONVERT(varchar(10), EntryDate, 120) AS EntryDate, MoodLevel FROM MoodEntries WHERE UserID=@UserID ORDER BY EntryDate", new SqlParameter("@UserID", userId));
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
                .Append("<div class='form-group'><label>Notes</label><textarea name='notes' placeholder='What influenced your mood today?'></textarea><small class='form-helper'>A short wellbeing insight helps make your trend easier to understand.</small></div>")
                .Append("<div class='button-row'><button class='btn btn-primary' type='submit'>Save Mood</button><a class='btn btn-outline' href='MoodHistory.aspx'>View Mood History</a></div>")
                .Append("</section>")
                .Append("<section class='row cols-3'>")
                .Append("<article class='card mood-summary-card'><h2>Latest</h2><p class='stat-number'>").Append(latest).Append("/5</p><p>").Append(MoodLabel(latest)).Append("</p></article>")
                .Append("<article class='card mood-summary-card'><h2>Average</h2><p class='stat-number'>").Append(average).Append("</p><p>Across saved entries</p></article>")
                .Append("<article class='card mood-summary-card'><h2>Entries</h2><p class='stat-number'>").Append(count).Append("</p><p>Your private mood log</p></article>")
                .Append("</section>")
                .Append("<section class='card mood-chart-card'><h2>Mood Trend</h2><div class='chart-filter'><button type='button' class='chart-filter-btn chart-filter-btn--active' data-range='weekly'>Weekly</button><button type='button' class='chart-filter-btn' data-range='monthly'>Monthly</button></div><p id='moodChartEmpty' style='display:none;text-align:center;padding:2rem 0;color:var(--text-secondary);'>No mood entries in this range yet.</p><canvas id='moodChart'></canvas></section>")
                .Append("<script>var moodData=").Append(json).Append(@";
(function(){
  var moodNames={1:'Very Low',2:'Low',3:'Okay',4:'Good',5:'Great'};
  function sliceData(days){
    var cutoff=new Date();cutoff.setDate(cutoff.getDate()-days);
    return moodData.filter(function(x){return new Date(x.label)>=cutoff;});
  }
  function buildChart(days){
    var ctx=document.getElementById('moodChart');
    var empty=document.getElementById('moodChartEmpty');
    if(!ctx)return;
    if(ctx._mwChart){ctx._mwChart.destroy();ctx._mwChart=null;}
    var d=sliceData(days);
    if(d.length===0){ctx.style.display='none';if(empty)empty.style.display='block';return;}
    ctx.style.display='';if(empty)empty.style.display='none';
    var tickColor=getComputedStyle(document.documentElement).getPropertyValue('--text-secondary').trim()||'#5D6975';
    ctx._mwChart=new Chart(ctx,{type:'line',data:{labels:d.map(function(x){return x.label;}),datasets:[{label:'Mood',data:d.map(function(x){return x.value;}),borderColor:'#2F6F6D',backgroundColor:'rgba(47,111,109,.18)',tension:.35,fill:true,pointRadius:4}]},options:{responsive:true,scales:{y:{min:1,max:5,ticks:{stepSize:1,color:tickColor,callback:function(v){return moodNames[v]||'';}},grid:{color:'rgba(107,168,168,.1)'}},x:{ticks:{color:tickColor}}}}});
  }
  if(window.Chart){
    buildChart(7);
    document.querySelectorAll('.chart-filter-btn').forEach(function(btn){
      btn.addEventListener('click',function(){
        document.querySelectorAll('.chart-filter-btn').forEach(function(b){b.classList.remove('chart-filter-btn--active');});
        btn.classList.add('chart-filter-btn--active');
        buildChart(btn.getAttribute('data-range')==='monthly'?30:7);
      });
    });
  }
})();
</script>");
            return sb.ToString();
        }

        private static void AppendMoodOption(StringBuilder sb, int value, string label, int selectedMood)
        {
            sb.Append("<label class='mood-option-card'><input type='radio' name='moodLevel' value='").Append(value).Append("'");
            if (value == selectedMood)
                sb.Append(" checked");
            sb.Append(" /><span class='mood-emoji'>").Append(value).Append("</span><strong>").Append(label).Append("</strong><small>").Append(value).Append("/5</small></label>");
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
