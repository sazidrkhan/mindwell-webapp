// Purpose: Public article-detail page for rendering one published article and its module back-link.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.Public
{
    public partial class ArticleDetails : System.Web.UI.Page
    {
        protected string RenderArticleDetails()
        {
            // Only published articles are displayed so unpublished/admin-hidden content is not exposed.
            int id;
            if (!SecurityHelper.TryGetPositiveIntQuery("ArticleID", out id))
                return RenderArticleBackActions(0) + "<section class='card'><h1>Article not found</h1><p>This article is not available.</p></section>";

            QueryResult articleResult = MindWellServices.TryQuery(@"SELECT a.Title, a.PreviewContent, a.Content, a.ImageURL, a.IsMembersOnly, lm.ModuleID, lm.ModuleName FROM Articles a INNER JOIN LearningModules lm ON a.ModuleID=lm.ModuleID WHERE a.ArticleID=@ArticleID AND a.IsPublished=1 AND " + MindWellServices.ActiveModuleWhere + " AND " + MindWellServices.CleanDemoTextWhere("a.Title") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(a.PreviewContent, '')"), new SqlParameter("@ArticleID", id));
            if (articleResult.HasError) return MindWellServices.RenderDataUnavailable("This article could not be loaded. Please try again.");
            DataTable article = articleResult.Data;
            if (article.Rows.Count == 0)
                return RenderArticleBackActions(0) + "<section class='card'><h1>Article not found</h1><p>This article is not available.</p></section>";

            DataRow row = article.Rows[0];
            int moduleId = MindWellServices.ToInt(row["ModuleID"], 0);
            bool membersOnly = Convert.ToBoolean(row["IsMembersOnly"]);
            bool canReadFullArticle = !membersOnly || SecurityHelper.IsUserAuthenticated();
            string body = canReadFullArticle
                ? MindWellServices.RenderArticleBody(row["Content"])
                : MindWellServices.RenderArticleBody(row["PreviewContent"]);
            string image = string.Empty;
            if (canReadFullArticle)
            {
                string imageUrl = MindWellServices.SafeDisplayUrl(Convert.ToString(row["ImageURL"]), this);
                image = string.IsNullOrWhiteSpace(imageUrl) ? string.Empty : "<figure class='article-cover'><img class='article-cover-image' src='" + MindWellServices.Encode(imageUrl) + "' alt='' loading='lazy' decoding='async' /></figure>";
            }
            string loginUrl = "../Auth/Login.aspx?returnUrl=" + Server.UrlEncode(Request.RawUrl);

            var sb = new StringBuilder();
            sb.Append("<div class='article-page'>").Append(RenderArticleBackActions(moduleId))
                .Append("<article class='card article-reader'><header class='article-reader-head'><div><p class='content-kicker'>")
                .Append(MindWellServices.Encode(row["ModuleName"])).Append("</p>");
            if (membersOnly)
                sb.Append("<p><span class='access-badge'>").Append(canReadFullArticle ? "Member lesson" : "Full lesson after sign in").Append("</span></p>");

            sb.Append("<h1>").Append(MindWellServices.Encode(row["Title"])).Append("</h1>")
                .Append("</div><div class='article-heading-art'>").Append(WellbeingUi.Scene(moduleId, "articles")).Append("</div></header>")
                .Append(image).Append("<div class='article-body'>").Append(body).Append("</div>");

            sb.Append("</article>");
            if (!canReadFullArticle)
            {
                sb.Append("<section class='card article-member-card'><p class='content-kicker'>Member lesson</p><h2>Sign in to read the full article</h2><p>MindWell article previews are public; full lessons are available to registered learners.</p><p class='page-actions article-lock-actions'><a class='btn btn-primary' href='").Append(loginUrl).Append("'>Sign In</a></p></section>");
            }

            if (canReadFullArticle)
                sb.Append(RenderFurtherReading(moduleId));
            return sb.Append("</div>").ToString();
        }

        private string RenderArticleBackActions(int moduleId)
        {
            var sb = new StringBuilder("<p class='page-actions article-back-actions'><a class='btn btn-outline btn-sm' href='Articles.aspx'>Back to Articles</a>");
            if (moduleId > 0)
                sb.Append(" <a class='btn btn-outline btn-sm' href='ModuleDetails.aspx?ModuleID=").Append(moduleId).Append("'>Back to Module</a>");
            return sb.Append("</p>").ToString();
        }

        private string RenderFurtherReading(int moduleId)
        {
            DataTable resources = MindWellServices.Query(@"SELECT TOP 4 Title, ResourceUrl FROM Resources WHERE ModuleID=@ModuleID AND IsPublished=1 AND Title LIKE 'Further reading:%' AND " + MindWellServices.CleanDemoTextWhere("Title") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(ResourceUrl, '')") + " ORDER BY CreatedDate DESC", new SqlParameter("@ModuleID", moduleId));
            if (resources.Rows.Count == 0)
                return string.Empty;

            var sb = new StringBuilder("<section class='card'><h2>Further reading</h2><ul class='learning-list'>");
            foreach (DataRow resource in resources.Rows)
            {
                string url = MindWellServices.SafeDisplayUrl(Convert.ToString(resource["ResourceUrl"]), this);
                if (string.IsNullOrEmpty(url))
                    continue;

                sb.Append("<li><a href='").Append(MindWellServices.Encode(url)).Append("' rel='noopener noreferrer' target='_blank'>")
                    .Append(MindWellServices.Encode(resource["Title"])).Append("</a></li>");
            }

            return sb.Append("</ul></section>").ToString();
        }
    }
}
