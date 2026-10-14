// Purpose: Admin content page for maintaining learning articles and resource-library links.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.Admin
{
    public partial class Content : AdminPageBase
    {
        protected string RenderContent()
        {
            var form = Request.Unvalidated.Form;
            string message = HandleContentPost();
            int editArticleId = MindWellServices.ToInt(Request.QueryString["editArticle"], 0);
            int editResourceId = MindWellServices.ToInt(Request.QueryString["editResource"], 0);
            string contentQuery = SecurityHelper.SafeTrim(Request.QueryString["contentQuery"], 100);
            string contentStatus = SecurityHelper.SafeTrim(Request.QueryString["contentStatus"], 30);
            if (string.IsNullOrEmpty(contentStatus))
                contentStatus = "Published";
            int contentModuleId = 0;
            if (Request.QueryString["contentModuleId"] != null)
                SecurityHelper.TryGetPositiveIntQuery("contentModuleId", out contentModuleId);

            string articleSql = @"SELECT a.ArticleID, a.ModuleID, a.Title, a.PreviewContent, a.Content, a.ImageURL, a.IsMembersOnly, a.IsPublished, lm.ModuleName FROM Articles a INNER JOIN LearningModules lm ON a.ModuleID=lm.ModuleID WHERE NULLIF(LTRIM(RTRIM(a.Title)), '') IS NOT NULL AND " + MindWellServices.CleanDemoTextWhere("a.Title") + @" AND (@Query='' OR a.Title LIKE '%' + @Query + '%') AND (@ModuleID=0 OR a.ModuleID=@ModuleID) AND (@Status='' OR @Status='All' OR (@Status='Published' AND a.IsPublished=1) OR (@Status='Unpublished/Archived' AND a.IsPublished=0)) ORDER BY lm.OrderIndex, a.Title";
            string resourceSql = @"SELECT ResourceID, ModuleID, Title, Description, ResourceType, ResourceUrl, IsPublished FROM Resources WHERE NULLIF(LTRIM(RTRIM(Title)), '') IS NOT NULL AND " + MindWellServices.CleanDemoTextWhere("Title") + @" AND (@Query='' OR Title LIKE '%' + @Query + '%') AND (@ModuleID=0 OR ModuleID=@ModuleID) AND (@Status='' OR @Status='All' OR (@Status='Published' AND IsPublished=1) OR (@Status='Unpublished/Archived' AND IsPublished=0)) ORDER BY CreatedDate DESC";
            QueryResult articlesResult = MindWellServices.TryQuery(articleSql, new SqlParameter("@Query", contentQuery), new SqlParameter("@ModuleID", contentModuleId), new SqlParameter("@Status", contentStatus));
            QueryResult resourcesResult = MindWellServices.TryQuery(resourceSql, new SqlParameter("@Query", contentQuery), new SqlParameter("@ModuleID", contentModuleId), new SqlParameter("@Status", contentStatus));
            if (articlesResult.HasError || resourcesResult.HasError)
                return message + MindWellServices.RenderDataUnavailable("Learning content is temporarily unavailable: article or resource data could not be loaded.");

            DataTable articles = articlesResult.Data;
            DataTable resources = resourcesResult.Data;
            QueryResult moduleOptionsResult = MindWellServices.TryGetModuleOptions();
            if (moduleOptionsResult.HasError)
                return message + MindWellServices.RenderDataUnavailable("Learning content is temporarily unavailable: content module options could not be loaded.");

            DataTable moduleOptions = moduleOptionsResult.Data;
            string moduleOptionMarkup = MindWellServices.RenderModuleOptions(moduleOptions, 1);
            string filterModuleOptions = "<option value='0'>All modules</option>" + MindWellServices.RenderModuleOptions(moduleOptions, contentModuleId);
            string articlePanelOpen = editArticleId > 0 ? " open" : string.Empty;
            string resourcePanelOpen = editResourceId > 0 ? " open" : string.Empty;

            var sb = new StringBuilder("<p class='back-link'><a class='btn btn-outline btn-sm' href='Dashboard.aspx'>Back to Admin Dashboard</a></p><section class='section-header'><h1>Manage Resources</h1><p>Published clean records are shown by default; use filters to review archived resources and article references.</p></section>")
                .Append(message)
                .Append("<section class='card'><h2>Filters</h2><div class='admin-filter-grid'><label for='contentQuery'>Search<input id='contentQuery' name='contentQuery' autocomplete='off' value='").Append(MindWellServices.Encode(contentQuery)).Append("' /></label><label for='contentModuleId'>Module<select id='contentModuleId' name='contentModuleId' autocomplete='off'>").Append(filterModuleOptions).Append("</select></label><label for='contentStatus'>Status<select id='contentStatus' name='contentStatus' autocomplete='off'>").Append(RenderStatusFilterOptions(contentStatus)).Append("</select></label><button type='button' class='btn btn-primary' onclick=\"window.location='Content.aspx?contentQuery='+encodeURIComponent(document.getElementById('contentQuery').value)+'&contentModuleId='+encodeURIComponent(document.getElementById('contentModuleId').value)+'&contentStatus='+encodeURIComponent(document.getElementById('contentStatus').value)\">Apply Filters</button></div></section>")
                .Append("<details class='card admin-entry-panel'><summary>Add Article</summary><div class='admin-entry-body'><label for='articleModule'>Module</label><select id='articleModule' name='moduleId' autocomplete='off'>")
                .Append(moduleOptionMarkup)
                .Append("</select><label for='articleTitle'>Title</label><input id='articleTitle' name='title' autocomplete='off' /><label for='articlePreviewContent'>Plain text preview</label><textarea id='articlePreviewContent' name='previewContent' rows='3' autocomplete='off'></textarea><label for='articleContent'>Full article body</label><textarea id='articleContent' name='content' rows='10' autocomplete='off'></textarea><label for='articleImageUrl'>ImageURL</label><input id='articleImageUrl' type='url' name='ImageURL' autocomplete='url' placeholder='https://www.who.int/health-topics/mental-health' /><label for='articleMembersOnly'><input id='articleMembersOnly' type='checkbox' name='IsMembersOnly' value='1' autocomplete='off' checked /> Registered users only</label><label for='articlePublished'><input id='articlePublished' type='checkbox' name='IsPublished' value='1' autocomplete='off' checked /> Published</label><p class='page-actions'><button class='btn btn-primary' name='contentAction' value='saveArticle'>Add Article</button></p></div></details><details class='card admin-entry-panel'><summary>Add Resource</summary><div class='admin-entry-body'><label for='resourceModule'>Module</label><select id='resourceModule' name='resourceModuleId' autocomplete='off'>")
                .Append(moduleOptionMarkup)
                .Append("</select><label for='resourceTitle'>Title</label><input id='resourceTitle' name='resourceTitle' autocomplete='off' /><label for='resourceDescription'>Description</label><input id='resourceDescription' name='resourceDescription' autocomplete='off' /><label for='resourceType'>Type</label><input id='resourceType' name='resourceType' autocomplete='off' value='Official source' /><label for='resourceUrl'>ResourceUrl</label><input id='resourceUrl' type='url' name='resourceUrl' autocomplete='url' placeholder='https://www.apa.org/topics/stress/tips' /><label for='resourceIsPublished'><input id='resourceIsPublished' type='checkbox' name='resourceIsPublished' value='1' autocomplete='off' checked /> Published</label><p class='page-actions'><button class='btn btn-primary' name='contentAction' value='saveResource'>Add Resource</button></p></div></details><details class='card admin-list-panel'").Append(articlePanelOpen).Append("><summary>Articles (").Append(articles.Rows.Count).Append(" shown)<span class='admin-status-chip'>Status: ").Append(MindWellServices.Encode(contentStatus)).Append("</span></summary><div class='admin-list-body'><div class='table-responsive'><table class='data-table admin-compact-table admin-content-table'><tr><th>Title</th><th>Module</th><th>Members</th><th>Published</th><th>Actions</th></tr>");

            foreach (DataRow row in articles.Rows)
            {
                if (MindWellServices.ToInt(row["ArticleID"], 0) == editArticleId)
                    AppendArticleEditRow(sb, row, moduleOptions);
                else
                    AppendArticleDisplayRow(sb, row);
            }

            sb.Append("</table></div></div></details><details class='card admin-list-panel'").Append(resourcePanelOpen).Append("><summary>Resources (").Append(resources.Rows.Count).Append(" shown)<span class='admin-status-chip'>Status: ").Append(MindWellServices.Encode(contentStatus)).Append("</span></summary><div class='admin-list-body'><div class='table-responsive'><table class='data-table resource-table admin-compact-table admin-content-table'><tr><th>Title</th><th>Type</th><th>URL</th><th>Published</th><th>Actions</th></tr>");
            foreach (DataRow row in resources.Rows)
            {
                if (MindWellServices.ToInt(row["ResourceID"], 0) == editResourceId)
                    AppendResourceEditRow(sb, row, moduleOptions);
                else
                    AppendResourceDisplayRow(sb, row);
            }

            sb.Append("</table></div></div></details>");
            return sb.ToString();
        }

        private string HandleContentPost()
        {
            if (!IsPostBack || Request.Unvalidated.Form["contentAction"] == null)
                return string.Empty;

            var form = Request.Unvalidated.Form;
            string action = form["contentAction"] ?? string.Empty;
            if (action == "saveArticle")
                return SaveArticle(0, string.Empty);
            if (action.StartsWith("saveArticleEdit:", StringComparison.Ordinal))
                return SaveArticle(MindWellServices.ToInt(action.Split(':')[1], 0), "_" + action.Split(':')[1]);
            if (action.StartsWith("publishArticle:", StringComparison.Ordinal))
                return SetArticlePublished(MindWellServices.ToInt(action.Split(':')[1], 0), true);
            if (action.StartsWith("unpublishArticle:", StringComparison.Ordinal))
                return SetArticlePublished(MindWellServices.ToInt(action.Split(':')[1], 0), false);
            if (action.StartsWith("deleteArticle:", StringComparison.Ordinal))
            {
                int rowsAffected = MindWellServices.NonQuery("DELETE FROM Articles WHERE ArticleID=@ArticleID", new SqlParameter("@ArticleID", MindWellServices.ToInt(action.Split(':')[1], 0)));
                return rowsAffected > 0 ? "<div class='alert alert-success'>Article deleted.</div>" : "<div class='alert alert-warning'>Article was not deleted.</div>";
            }

            if (action == "saveResource")
                return SaveResource(0, string.Empty);
            if (action.StartsWith("saveResourceEdit:", StringComparison.Ordinal))
                return SaveResource(MindWellServices.ToInt(action.Split(':')[1], 0), "_" + action.Split(':')[1]);
            if (action.StartsWith("publishResource:", StringComparison.Ordinal))
                return SetResourcePublished(MindWellServices.ToInt(action.Split(':')[1], 0), true);
            if (action.StartsWith("unpublishResource:", StringComparison.Ordinal))
                return SetResourcePublished(MindWellServices.ToInt(action.Split(':')[1], 0), false);
            if (action.StartsWith("deleteResource:", StringComparison.Ordinal))
            {
                int rowsAffected = MindWellServices.NonQuery("DELETE FROM Resources WHERE ResourceID=@ResourceID", new SqlParameter("@ResourceID", MindWellServices.ToInt(action.Split(':')[1], 0)));
                return rowsAffected > 0 ? "<div class='alert alert-success'>Resource deleted.</div>" : "<div class='alert alert-warning'>Resource was not deleted.</div>";
            }

            return string.Empty;
        }

        private string SaveArticle(int articleId, string suffix)
        {
            var form = Request.Unvalidated.Form;
            string validation = ValidateArticleInput(form["title" + suffix], form["previewContent" + suffix], form["content" + suffix], form["ImageURL" + suffix]);
            if (!string.IsNullOrEmpty(validation))
                return validation;

            int rowsAffected;
            if (articleId > 0)
            {
                rowsAffected = MindWellServices.NonQuery(@"UPDATE Articles SET ModuleID=@ModuleID, Title=@Title, PreviewContent=@PreviewContent, Content=@Content, ImageURL=@ImageURL, IsMembersOnly=@IsMembersOnly, IsPublished=@IsPublished, UpdatedDate=GETDATE() WHERE ArticleID=@ArticleID",
                    new SqlParameter("@ModuleID", MindWellServices.ToInt(form["moduleId" + suffix], 1)),
                    new SqlParameter("@Title", SecurityHelper.SafeTrim(form["title" + suffix], 200)),
                    new SqlParameter("@PreviewContent", SecurityHelper.SafeTrim(form["previewContent" + suffix], 700)),
                    new SqlParameter("@Content", form["content" + suffix] ?? string.Empty),
                    new SqlParameter("@ImageURL", string.IsNullOrWhiteSpace(form["ImageURL" + suffix]) ? (object)DBNull.Value : SecurityHelper.SafeTrim(form["ImageURL" + suffix], 255)),
                    new SqlParameter("@IsMembersOnly", form["IsMembersOnly" + suffix] == "1"),
                    new SqlParameter("@IsPublished", form["IsPublished" + suffix] == "1"),
                    new SqlParameter("@ArticleID", articleId));
            }
            else
            {
                rowsAffected = MindWellServices.NonQuery(@"INSERT INTO Articles (ModuleID, Title, PreviewContent, Content, ImageURL, IsMembersOnly, IsPublished, CreatedDate) VALUES (@ModuleID,@Title,@PreviewContent,@Content,@ImageURL,@IsMembersOnly,@IsPublished,GETDATE())",
                    new SqlParameter("@ModuleID", MindWellServices.ToInt(form["moduleId"], 1)),
                    new SqlParameter("@Title", SecurityHelper.SafeTrim(form["title"], 200)),
                    new SqlParameter("@PreviewContent", SecurityHelper.SafeTrim(form["previewContent"], 700)),
                    new SqlParameter("@Content", form["content"] ?? string.Empty),
                    new SqlParameter("@ImageURL", string.IsNullOrWhiteSpace(form["ImageURL"]) ? (object)DBNull.Value : SecurityHelper.SafeTrim(form["ImageURL"], 255)),
                    new SqlParameter("@IsMembersOnly", form["IsMembersOnly"] == "1"),
                    new SqlParameter("@IsPublished", form["IsPublished"] == "1"));
            }

            return rowsAffected > 0 ? "<div class='alert alert-success'>Article saved.</div>" : "<div class='alert alert-warning'>Article was not saved.</div>";
        }

        private string SaveResource(int resourceId, string suffix)
        {
            var form = Request.Unvalidated.Form;
            string resourceUrl = SecurityHelper.SafeTrim(form["resourceUrl" + suffix], 255);
            string title = SecurityHelper.SafeTrim(form["resourceTitle" + suffix], 200);
            if (string.IsNullOrWhiteSpace(title))
                return "<div class='alert alert-danger'>Resource title is required.</div>";
            if (!MindWellServices.IsSafeDisplayUrl(resourceUrl))
                return "<div class='alert alert-danger'>ResourceUrl must be an http, https, or local MindWell URL.</div>";

            int rowsAffected;
            if (resourceId > 0)
            {
                rowsAffected = MindWellServices.NonQuery("UPDATE Resources SET ModuleID=@ModuleID, Title=@Title, Description=@Description, ResourceType=@Type, ResourceUrl=@Url, IsPublished=@IsPublished WHERE ResourceID=@ResourceID", new SqlParameter("@ModuleID", MindWellServices.ToInt(form["resourceModuleId" + suffix], 1)), new SqlParameter("@Title", title), new SqlParameter("@Description", SecurityHelper.SafeTrim(form["resourceDescription" + suffix], 500)), new SqlParameter("@Type", SecurityHelper.SafeTrim(form["resourceType" + suffix], 50)), new SqlParameter("@Url", resourceUrl), new SqlParameter("@IsPublished", form["resourceIsPublished" + suffix] == "1"), new SqlParameter("@ResourceID", resourceId));
            }
            else
            {
                rowsAffected = MindWellServices.NonQuery("INSERT INTO Resources (ModuleID, Title, Description, ResourceType, ResourceUrl, IsPublished, CreatedDate) VALUES (@ModuleID,@Title,@Description,@Type,@Url,@IsPublished,GETDATE())", new SqlParameter("@ModuleID", MindWellServices.ToInt(form["resourceModuleId"], 1)), new SqlParameter("@Title", title), new SqlParameter("@Description", SecurityHelper.SafeTrim(form["resourceDescription"], 500)), new SqlParameter("@Type", SecurityHelper.SafeTrim(form["resourceType"], 50)), new SqlParameter("@Url", resourceUrl), new SqlParameter("@IsPublished", form["resourceIsPublished"] == "1"));
            }

            return rowsAffected > 0 ? "<div class='alert alert-success'>Resource saved.</div>" : "<div class='alert alert-warning'>Resource was not saved.</div>";
        }

        private string SetArticlePublished(int articleId, bool published)
        {
            int rowsAffected = MindWellServices.NonQuery("UPDATE Articles SET IsPublished=@Published, UpdatedDate=GETDATE() WHERE ArticleID=@ArticleID", new SqlParameter("@Published", published), new SqlParameter("@ArticleID", articleId));
            return rowsAffected > 0 ? "<div class='alert alert-success'>Article saved.</div>" : "<div class='alert alert-warning'>Article was not changed.</div>";
        }

        private string SetResourcePublished(int resourceId, bool published)
        {
            int rowsAffected = MindWellServices.NonQuery("UPDATE Resources SET IsPublished=@Published WHERE ResourceID=@ResourceID", new SqlParameter("@Published", published), new SqlParameter("@ResourceID", resourceId));
            return rowsAffected > 0 ? "<div class='alert alert-success'>Resource saved.</div>" : "<div class='alert alert-warning'>Resource was not changed.</div>";
        }

        private void AppendArticleDisplayRow(StringBuilder sb, DataRow row)
        {
            int articleId = MindWellServices.ToInt(row["ArticleID"], 0);
            bool published = Convert.ToBoolean(row["IsPublished"]);
            sb.Append("<tr><td>").Append(MindWellServices.Encode(row["Title"])).Append("</td><td>").Append(MindWellServices.Encode(row["ModuleName"])).Append("</td><td>").Append(MindWellServices.Encode(row["IsMembersOnly"])).Append("</td><td>").Append(MindWellServices.Encode(row["IsPublished"])).Append("</td><td><div class='button-row'><a class='btn btn-primary btn-sm' href='Content.aspx?editArticle=").Append(articleId).Append("'>Edit</a><button formnovalidate data-confirm='Are you sure you want to unpublish this article? It will be hidden from learners.' class='btn btn-sm' name='contentAction' value='").Append(published ? "unpublishArticle:" : "publishArticle:").Append(articleId).Append("'>").Append(published ? "Unpublish" : "Publish").Append("</button><button formnovalidate data-confirm='Are you sure you want to delete this test record?' class='btn btn-danger btn-sm' name='contentAction' value='deleteArticle:").Append(articleId).Append("'>Delete</button></div></td></tr>");
        }

        private void AppendArticleEditRow(StringBuilder sb, DataRow row, DataTable moduleOptions)
        {
            int articleId = MindWellServices.ToInt(row["ArticleID"], 0);
            string encodedId = MindWellServices.Encode(articleId);
            bool isMembersOnly = Convert.ToBoolean(row["IsMembersOnly"]);
            bool isPublished = Convert.ToBoolean(row["IsPublished"]);
            sb.Append("<tr class='editing-row'><td colspan='5'><label for='articleModule_").Append(encodedId).Append("'>Module</label><select id='articleModule_").Append(encodedId).Append("' name='moduleId_").Append(articleId).Append("' autocomplete='off'>").Append(MindWellServices.RenderModuleOptions(moduleOptions, MindWellServices.ToInt(row["ModuleID"], 1))).Append("</select><label for='articleTitle_").Append(encodedId).Append("'>Title</label><input id='articleTitle_").Append(encodedId).Append("' name='title_").Append(articleId).Append("' autocomplete='off' value='").Append(MindWellServices.Encode(row["Title"])).Append("' /><label for='articlePreviewContent_").Append(encodedId).Append("'>Preview</label><textarea id='articlePreviewContent_").Append(encodedId).Append("' name='previewContent_").Append(articleId).Append("' rows='3' autocomplete='off'>").Append(MindWellServices.Encode(row["PreviewContent"])).Append("</textarea><label for='articleContent_").Append(encodedId).Append("'>Full article body</label><textarea id='articleContent_").Append(encodedId).Append("' name='content_").Append(articleId).Append("' rows='8' autocomplete='off'>").Append(MindWellServices.Encode(row["Content"])).Append("</textarea><label for='articleImageUrl_").Append(encodedId).Append("'>ImageURL</label><input id='articleImageUrl_").Append(encodedId).Append("' type='url' name='ImageURL_").Append(articleId).Append("' autocomplete='url' value='").Append(MindWellServices.Encode(row["ImageURL"])).Append("' /><label for='articleMembersOnly_").Append(encodedId).Append("'><input id='articleMembersOnly_").Append(encodedId).Append("' type='checkbox' name='IsMembersOnly_").Append(articleId).Append("' value='1' autocomplete='off'").Append(isMembersOnly ? " checked" : string.Empty).Append(" /> Registered users only</label><label for='articlePublished_").Append(encodedId).Append("'><input id='articlePublished_").Append(encodedId).Append("' type='checkbox' name='IsPublished_").Append(articleId).Append("' value='1' autocomplete='off'").Append(isPublished ? " checked" : string.Empty).Append(" /> Published</label><p class='button-row'><button class='btn btn-primary btn-sm' name='contentAction' value='saveArticleEdit:").Append(articleId).Append("'>Save</button><a class='btn btn-sm' href='Content.aspx'>Cancel</a></p></td></tr>");
        }

        private void AppendResourceDisplayRow(StringBuilder sb, DataRow row)
        {
            int resourceId = MindWellServices.ToInt(row["ResourceID"], 0);
            bool published = Convert.ToBoolean(row["IsPublished"]);
            string resourceUrl = MindWellServices.IsSafeDisplayUrl(Convert.ToString(row["ResourceUrl"])) ? MindWellServices.SafeDisplayUrl(Convert.ToString(row["ResourceUrl"]), this) : string.Empty;
            sb.Append("<tr><td>").Append(MindWellServices.Encode(row["Title"])).Append("</td><td>").Append(MindWellServices.Encode(row["ResourceType"])).Append("</td><td>");
            if (!string.IsNullOrEmpty(resourceUrl))
                sb.Append("<a class='resource-url' href='").Append(MindWellServices.Encode(resourceUrl)).Append("' target='_blank' rel='noopener noreferrer'>").Append(MindWellServices.Encode(resourceUrl)).Append("</a>");
            sb.Append("</td><td>").Append(MindWellServices.Encode(row["IsPublished"])).Append("</td><td><div class='button-row'><a class='btn btn-primary btn-sm' href='Content.aspx?editResource=").Append(resourceId).Append("'>Edit</a><button formnovalidate data-confirm='Are you sure you want to unpublish this resource? It will be hidden from learners.' class='btn btn-sm' name='contentAction' value='").Append(published ? "unpublishResource:" : "publishResource:").Append(resourceId).Append("'>").Append(published ? "Unpublish" : "Publish").Append("</button><button formnovalidate data-confirm='Are you sure you want to delete this test record?' class='btn btn-danger btn-sm' name='contentAction' value='deleteResource:").Append(resourceId).Append("'>Delete</button></div></td></tr>");
        }

        private string RenderStatusFilterOptions(string selected)
        {
            string[] statuses = { "All", "Published", "Unpublished/Archived" };
            var sb = new StringBuilder();
            foreach (string status in statuses)
            {
                sb.Append("<option");
                if (string.Equals(status, selected, StringComparison.OrdinalIgnoreCase))
                    sb.Append(" selected");
                sb.Append(">").Append(status).Append("</option>");
            }

            return sb.ToString();
        }

        private void AppendResourceEditRow(StringBuilder sb, DataRow row, DataTable moduleOptions)
        {
            int resourceId = MindWellServices.ToInt(row["ResourceID"], 0);
            string encodedId = MindWellServices.Encode(resourceId);
            bool published = Convert.ToBoolean(row["IsPublished"]);
            sb.Append("<tr class='editing-row'><td colspan='5'><label for='resourceModule_").Append(encodedId).Append("'>Module</label><select id='resourceModule_").Append(encodedId).Append("' name='resourceModuleId_").Append(resourceId).Append("' autocomplete='off'>").Append(MindWellServices.RenderModuleOptions(moduleOptions, MindWellServices.ToInt(row["ModuleID"], 1))).Append("</select><label for='resourceTitle_").Append(encodedId).Append("'>Title</label><input id='resourceTitle_").Append(encodedId).Append("' name='resourceTitle_").Append(resourceId).Append("' autocomplete='off' value='").Append(MindWellServices.Encode(row["Title"])).Append("' /><label for='resourceDescription_").Append(encodedId).Append("'>Description</label><input id='resourceDescription_").Append(encodedId).Append("' name='resourceDescription_").Append(resourceId).Append("' autocomplete='off' value='").Append(MindWellServices.Encode(row["Description"])).Append("' /><label for='resourceType_").Append(encodedId).Append("'>Type</label><input id='resourceType_").Append(encodedId).Append("' name='resourceType_").Append(resourceId).Append("' autocomplete='off' value='").Append(MindWellServices.Encode(row["ResourceType"])).Append("' /><label for='resourceUrl_").Append(encodedId).Append("'>ResourceUrl</label><input id='resourceUrl_").Append(encodedId).Append("' type='url' name='resourceUrl_").Append(resourceId).Append("' autocomplete='url' value='").Append(MindWellServices.Encode(row["ResourceUrl"])).Append("' /><label for='resourceIsPublished_").Append(encodedId).Append("'><input id='resourceIsPublished_").Append(encodedId).Append("' type='checkbox' name='resourceIsPublished_").Append(resourceId).Append("' value='1' autocomplete='off'").Append(published ? " checked" : string.Empty).Append(" /> Published</label><p class='button-row'><button class='btn btn-primary btn-sm' name='contentAction' value='saveResourceEdit:").Append(resourceId).Append("'>Save</button><a class='btn btn-sm' href='Content.aspx'>Cancel</a></p></td></tr>");
        }

        private string ValidateArticleInput(string title, string preview, string content, string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(preview) || string.IsNullOrWhiteSpace(content))
                return "<div class='alert alert-danger'>Title, preview, and full article body are needed.</div>";

            if (!MindWellServices.IsSafeDisplayUrl(imageUrl))
                return "<div class='alert alert-danger'>ImageURL must be an http, https, or local MindWell URL.</div>";

            return string.Empty;
        }
    }
}
