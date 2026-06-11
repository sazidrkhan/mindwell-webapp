// Purpose: Public article listing with optional module filtering and article previews.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.Public
{
    public partial class Articles : System.Web.UI.Page
    {
        protected string RenderArticles()
        {
            // Optional ModuleID filter lets module pages deep-link into the relevant article list.
            int moduleId = 0;
            if (Request.QueryString["ModuleID"] != null && !SecurityHelper.TryGetPositiveIntQuery("ModuleID", out moduleId))
                return "<section class='card empty-state'><h1>Articles</h1><p>Invalid module selected.</p><p class='page-actions'><a class='btn btn-outline' href='Modules.aspx'>Back to Modules</a></p></section>";

            string moduleName = string.Empty;
            if (moduleId > 0)
            {
                object name = MindWellServices.Scalar("SELECT lm.ModuleName FROM LearningModules lm WHERE lm.ModuleID=@ModuleID AND " + MindWellServices.ActiveModuleWhere, new SqlParameter("@ModuleID", moduleId));
                moduleName = name == null || name == DBNull.Value ? string.Empty : name.ToString();
            }

            string sql = @"SELECT a.ArticleID, a.Title, a.PreviewContent, a.Content, a.IsMembersOnly, lm.ModuleID, lm.ModuleName FROM Articles a INNER JOIN LearningModules lm ON a.ModuleID=lm.ModuleID WHERE a.IsPublished=1 AND NULLIF(LTRIM(RTRIM(a.Title)), '') IS NOT NULL AND " + MindWellServices.ActiveModuleWhere + " AND " + MindWellServices.CleanDemoTextWhere("a.Title") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(a.PreviewContent, '')") + @" AND (@ModuleID=0 OR a.ModuleID=@ModuleID) ORDER BY lm.OrderIndex, a.CreatedDate DESC";
            DataTable articles = MindWellServices.Query(sql, new SqlParameter("@ModuleID", moduleId));
            bool authenticated = SecurityHelper.IsUserAuthenticated();
            string title = moduleId > 0 && !string.IsNullOrEmpty(moduleName) ? moduleName + " Articles" : "Articles / Blog";
            var sb = new StringBuilder("<section class='section-header'><h1>")
                .Append(MindWellServices.Encode(title))
                .Append("</h1><p>Educational articles on mental health and wellbeing.</p></section><div class='row'>");
            foreach (DataRow row in articles.Rows)
            {
                // Previews are plain text so public listings never render raw article markup.
                string preview = MindWellServices.ArticlePreview(row["PreviewContent"], row["Content"], 220);
                bool membersOnly = Convert.ToBoolean(row["IsMembersOnly"]);
                string badge = membersOnly ? (authenticated ? "Member lesson" : "Full lesson after sign in") : "Public article";
                string cta = membersOnly ? (authenticated ? "Read full lesson" : "View preview") : "Read Article";
                sb.Append("<article class='card'><p><strong>").Append(MindWellServices.Encode(row["ModuleName"])).Append("</strong>")
                    .Append(" <span class='access-badge'>").Append(badge).Append("</span>")
                    .Append("</p><h2>").Append(MindWellServices.Encode(row["Title"])).Append("</h2><p>")
                    .Append(MindWellServices.Encode(preview))
                    .Append("</p><p><a class='btn btn-primary' href='ArticleDetails.aspx?ArticleID=").Append(row["ArticleID"]).Append("'>").Append(cta).Append("</a></p></article>");
            }
            return sb.Append("</div>").ToString();
        }
    }
}
