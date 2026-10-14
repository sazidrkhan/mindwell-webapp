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
                ScalarResult nameResult = MindWellServices.TryScalar("SELECT lm.ModuleName FROM LearningModules lm WHERE lm.ModuleID=@ModuleID AND " + MindWellServices.ActiveModuleWhere, new SqlParameter("@ModuleID", moduleId));
                if (nameResult.HasError) return MindWellServices.RenderDataUnavailable("Article topics could not be loaded. Please try again.");
                object name = nameResult.Value;
                moduleName = name == null || name == DBNull.Value ? string.Empty : name.ToString();
            }

            string sql = @"SELECT a.ArticleID, a.Title, a.PreviewContent, a.Content, a.IsMembersOnly, lm.ModuleID, lm.ModuleName FROM Articles a INNER JOIN LearningModules lm ON a.ModuleID=lm.ModuleID WHERE a.IsPublished=1 AND NULLIF(LTRIM(RTRIM(a.Title)), '') IS NOT NULL AND " + MindWellServices.ActiveModuleWhere + " AND " + MindWellServices.CleanDemoTextWhere("a.Title") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(a.PreviewContent, '')") + @" AND (@ModuleID=0 OR a.ModuleID=@ModuleID) ORDER BY lm.OrderIndex, a.CreatedDate DESC";
            QueryResult articlesResult = MindWellServices.TryQuery(sql, new SqlParameter("@ModuleID", moduleId));
            if (articlesResult.HasError) return MindWellServices.RenderDataUnavailable("Articles could not be loaded. Please try again.");
            DataTable articles = articlesResult.Data;
            bool authenticated = SecurityHelper.IsUserAuthenticated();
            string title = moduleId > 0 && !string.IsNullOrEmpty(moduleName) ? moduleName + " Articles" : "Articles / Blog";
            var sb = new StringBuilder("<div class='library-page' data-library><section class='library-hero'><div><span class='small-label'>A little understanding goes a long way</span><h1>")
                .Append(MindWellServices.Encode(title))
                .Append("</h1><p>Thoughtful reads for real life. Explore ideas for a steadier, kinder approach to your wellbeing.</p></div><div class='library-hero-art'>")
                .Append(WellbeingUi.Scene(moduleId, "articles")).Append("</div></section>");
            if (articles.Rows.Count == 0)
                return sb.Append(MindWellServices.RenderEmptyState("No articles are available yet.", "Published articles will appear here when they are ready.")).Append("</div>").ToString();
            sb.Append(WellbeingUi.LibraryTools(articles, "articles", false)).Append("<div class='row library-grid'>");
            foreach (DataRow row in articles.Rows)
            {
                // Previews are plain text so public listings never render raw article markup.
                string preview = MindWellServices.ArticlePreview(row["PreviewContent"], row["Content"], 220);
                bool membersOnly = Convert.ToBoolean(row["IsMembersOnly"]);
                string badge = membersOnly ? (authenticated ? "Member lesson" : "Full lesson after sign in") : "Public article";
                string cta = membersOnly ? (authenticated ? "Read full lesson" : "View preview") : "Read Article";
                int topic = MindWellServices.ToInt(row["ModuleID"], 0);
                sb.Append("<article class='card article-card library-card' data-library-item data-topic='").Append(topic)
                    .Append("'><div class='library-card-art topic-surface-").Append(topic).Append("'>").Append(WellbeingUi.Scene(topic, "articles"))
                    .Append("</div><div class='library-card-body'><p class='library-meta'><span>").Append(MindWellServices.Encode(row["ModuleName"])).Append("</span>")
                    .Append(" <span class='access-badge'>").Append(badge).Append("</span>")
                    .Append("</p><h2>").Append(MindWellServices.Encode(row["Title"])).Append("</h2><p class='library-excerpt'>")
                    .Append(MindWellServices.Encode(preview))
                    .Append("</p><p class='library-actions'><a class='btn btn-primary' href='ArticleDetails.aspx?ArticleID=").Append(row["ArticleID"]).Append("'>").Append(cta).Append("</a></p></div></article>");
            }
            return sb.Append("</div><section class='card library-empty' data-library-empty hidden><h2>No matching articles</h2><p>Try another phrase or clear the filters to see all articles.</p></section></div>").ToString();
        }
    }
}
