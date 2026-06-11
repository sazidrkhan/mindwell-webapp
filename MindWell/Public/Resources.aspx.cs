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
            DataTable resources = MindWellServices.Query(@"SELECT r.Title, r.Description, r.ResourceType, r.ResourceUrl, r.ModuleID, lm.ModuleName FROM Resources r LEFT JOIN LearningModules lm ON r.ModuleID=lm.ModuleID WHERE r.IsPublished=1 AND " + MindWellServices.CleanDemoTextWhere("r.Title") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(r.Description, '')") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(r.ResourceUrl, '')") + " AND (r.ModuleID IS NULL OR " + MindWellServices.ActiveModuleWhere + ") ORDER BY r.CreatedDate DESC");
            bool authenticated = SecurityHelper.IsUserAuthenticated();
            var sb = new StringBuilder("<section class='section-header'><h1>Resource Library</h1><p>Research articles, learning guides, and trusted mental health references to support your wellbeing journey.</p></section><div class='resource-grid'>");
            if (resources.Rows.Count == 0)
                return MindWellServices.RenderDataUnavailable("Learning content is temporarily unavailable: resources returned no rows.");

            foreach (DataRow row in resources.Rows)
            {
                string resourceUrl = Convert.ToString(row["ResourceUrl"]);
                string safeUrl = MindWellServices.IsSafeDisplayUrl(resourceUrl) ? MindWellServices.SafeDisplayUrl(resourceUrl, this) : string.Empty;
                sb.Append("<article class='card resource-card'><p><strong>").Append(MindWellServices.Encode(row["ResourceType"]))
                    .Append("</strong> ").Append(MindWellServices.Encode(row["ModuleName"]))
                    .Append("</p><h2>").Append(MindWellServices.Encode(row["Title"]))
                    .Append("</h2><p>").Append(MindWellServices.Encode(row["Description"]))
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

                sb.Append("</div></article>");
            }
            sb.Append("</div>");
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
