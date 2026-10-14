// Purpose: Public resource library for published worksheets, links, and downloadable wellbeing materials.
using System;
using System.Data;
using System.Text;

namespace MindWell.Public
{
    public partial class Resources : System.Web.UI.Page
    {
        protected string RenderResources()
        {
            // Loads published resources with their optional module names for browsing by visitors and users.
            QueryResult resourcesResult = MindWellServices.TryQuery(@"SELECT r.Title, r.Description, r.ResourceType, r.ResourceUrl, r.ModuleID, lm.ModuleName FROM Resources r LEFT JOIN LearningModules lm ON r.ModuleID=lm.ModuleID WHERE r.IsPublished=1 AND " + MindWellServices.CleanDemoTextWhere("r.Title") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(r.Description, '')") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(r.ResourceUrl, '')") + " AND (r.ModuleID IS NULL OR " + MindWellServices.ActiveModuleWhere + ") ORDER BY r.CreatedDate DESC");
            DataTable resources = resourcesResult.Data;
            bool authenticated = SecurityHelper.IsUserAuthenticated();
            var sb = new StringBuilder("<div class='library-page' data-library><section class='library-hero'><div><span class='small-label'>Tools for everyday wellbeing</span><h1>Resource Library</h1><p>Research articles, learning guides, and trusted mental health references to support your wellbeing journey.</p></div><div class='library-hero-art'>")
                .Append(WellbeingUi.Scene(0, "resources")).Append("</div></section>");
            if (resourcesResult.HasError)
                return MindWellServices.RenderDataUnavailable("Learning content is temporarily unavailable: resources could not be loaded.");

            if (resources.Rows.Count == 0)
                return sb.Append(MindWellServices.RenderEmptyState("No resources are available yet.", "Published wellbeing references will appear here when they are ready.")).Append("</div>").ToString();

            sb.Append(WellbeingUi.LibraryTools(resources, "resources", true)).Append("<div class='resource-grid library-grid'>");

            foreach (DataRow row in resources.Rows)
            {
                string resourceUrl = Convert.ToString(row["ResourceUrl"]);
                string safeUrl = MindWellServices.IsSafeDisplayUrl(resourceUrl) ? MindWellServices.SafeDisplayUrl(resourceUrl, this) : string.Empty;
                int moduleId = MindWellServices.ToInt(row["ModuleID"], 0);
                string title = Convert.ToString(row["Title"]);
                const string readingPrefix = "Further reading: ";
                if (title.StartsWith(readingPrefix, StringComparison.OrdinalIgnoreCase)) title = title.Substring(readingPrefix.Length);
                sb.Append("<article class='card resource-card library-card' data-library-item data-topic='").Append(moduleId)
                    .Append("' data-kind='").Append(MindWellServices.Encode(Convert.ToString(row["ResourceType"]).Trim())).Append("'><div class='library-card-art topic-surface-").Append(moduleId).Append("'>")
                    .Append(WellbeingUi.Scene(moduleId)).Append("</div><div class='library-card-body'><p class='library-meta'><span class='library-type'>").Append(MindWellServices.Encode(row["ResourceType"]))
                    .Append("</span><span>").Append(MindWellServices.Encode(row["ModuleName"]))
                    .Append("</span></p><h2>").Append(MindWellServices.Encode(title))
                    .Append("</h2><p class='library-excerpt'>").Append(MindWellServices.Encode(row["Description"]))
                    .Append("</p><div class='resource-actions'>");
                if (!string.IsNullOrEmpty(safeUrl))
                    sb.Append("<a class='btn btn-outline' href='").Append(MindWellServices.Encode(safeUrl)).Append("' target='_blank' rel='noopener noreferrer'>Open Source</a>");

                if (row["ModuleID"] != DBNull.Value)
                {
                    if (authenticated)
                    {
                        sb.Append("<a class='btn btn-primary' href='ModuleDetails.aspx?ModuleID=").Append(row["ModuleID"]).Append("'>View Module</a>")
                            .Append("<a class='btn btn-secondary' href='../User/Assignment.aspx?ModuleID=").Append(row["ModuleID"]).Append("'>Start Activity</a>");
                    }
                    else
                    {
                        sb.Append("<a class='btn btn-primary' href='ModuleDetails.aspx?ModuleID=").Append(row["ModuleID"]).Append("' data-requires-auth='true'>View Module</a>");
                    }
                }

                sb.Append("</div></div></article>");
            }
            sb.Append("</div><section class='card library-empty' data-library-empty hidden><h2>No matching resources</h2><p>Try another phrase or clear the filters to see the full library.</p></section></div>");
            if (!authenticated)
            {
                sb.Append(@"
<div id='moduleGateBackdrop' class='register-gate-backdrop' hidden></div>
<div id='moduleGateModal' class='register-gate-modal' role='dialog' aria-modal='true' aria-labelledby='moduleGateTitle' hidden>
  <p class='eyebrow'>Sign in to continue</p>
  <h2 id='moduleGateTitle'>Sign in to open this module</h2>
  <p>Sign in to access full module details and guided activities.</p>
  <div class='button-row'>
    <a class='btn btn-primary' href='../Auth/Login.aspx' data-login-return='true'>Sign In</a>
    <button type='button' class='btn btn-ghost' data-module-gate-close>Cancel</button>
  </div>
</div>");
            }

            return sb.ToString();
        }
    }
}
