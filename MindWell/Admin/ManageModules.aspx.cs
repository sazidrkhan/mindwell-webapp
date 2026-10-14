// Purpose: Admin module-management page for inserting, updating, archiving,
// and safely deleting learning modules used throughout the site.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.Admin
{
    public partial class ManageModules : AdminPageBase
    {
        protected string RenderManageModules()
        {
            // Processes form actions before rendering the insert form and current module table.
            string message = HandleModulePost();
            int editModuleId = MindWellServices.ToInt(Request.QueryString["edit"], 0);
            QueryResult modulesResult = MindWellServices.TryQuery("SELECT lm.ModuleID, lm.ModuleName, lm.Description, lm.OrderIndex FROM LearningModules lm WHERE " + MindWellServices.ActiveModuleWhere + " AND " + MindWellServices.CleanDemoTextWhere("lm.ModuleName") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(lm.Description, '')") + " ORDER BY lm.OrderIndex, lm.ModuleName");
            if (modulesResult.HasError)
                return message + MindWellServices.RenderDataUnavailable("Learning content is temporarily unavailable: module data could not be loaded.");

            DataTable modules = modulesResult.Data;

            var sb = new StringBuilder("<p class='back-link'><a class='btn btn-outline btn-sm' href='Dashboard.aspx'>Back to Admin Dashboard</a></p><section class='section-header'><h1>Manage Modules</h1><p>Add, update, archive, or safely delete learning modules.</p></section>")
                .Append(message)
                .Append("<details class='card admin-entry-panel'><summary>Insert Module</summary><div class='admin-entry-body'><p id='moduleAddHelp' class='form-helper'>Use a descriptive name and plain-text summary.</p><input type='hidden' name='moduleId' value='0' /><label for='moduleName'>Name</label><input id='moduleName' name='moduleName' autocomplete='off' aria-describedby='moduleAddHelp' data-validation-group='module-add' /><label for='moduleDescription'>Description</label><textarea id='moduleDescription' class='module-description' name='description' rows='4' autocomplete='off' aria-describedby='moduleAddHelp' data-validation-group='module-add'></textarea><label for='moduleOrderIndex'>Order</label><input id='moduleOrderIndex' type='number' name='orderIndex' value='1' autocomplete='off' aria-describedby='moduleAddHelp' /><p class='page-actions'><button name='moduleAction' value='insert' class='btn btn-primary'>Add Module</button></p></div></details><details class='card admin-list-panel' open><summary>Existing Modules (").Append(modules.Rows.Count).Append(" shown)</summary><div class='admin-list-body'><div class='table-responsive'><table class='data-table module-table'><tr><th>Name</th><th>Description</th><th>Order</th><th>Actions</th></tr>");

            foreach (DataRow row in modules.Rows)
            {
                int id = MindWellServices.ToInt(row["ModuleID"], 0);
                if (id == editModuleId)
                    AppendModuleEditRow(sb, row);
                else
                    AppendModuleDisplayRow(sb, row);
            }

            return sb.Append("</table></div></div></details>").ToString();
        }

        private string HandleModulePost()
        {
            // Routes insert/update/archive/delete actions from shared moduleAction button values.
            if (!IsPostBack || Request.Form["moduleAction"] == null)
                return string.Empty;

            string[] parts = Request.Form["moduleAction"].Split(':');
            string action = parts.Length > 0 ? parts[0] : string.Empty;
            int id = parts.Length > 1 ? MindWellServices.ToInt(parts[1], 0) : MindWellServices.ToInt(Request.Form["moduleId"], 0);
            string suffix = id > 0 ? "_" + id : string.Empty;
            string name = SecurityHelper.SafeTrim(Request.Form["moduleName" + suffix], 100);
            string description = SecurityHelper.SafeTrim(Request.Form["description" + suffix], 500);
            int orderIndex = MindWellServices.ToInt(Request.Form["orderIndex" + suffix], 1);

            if (action == "insert")
            {
                string validation = ValidateModuleInput(name, description, orderIndex);
                if (!string.IsNullOrEmpty(validation))
                    return validation;

                int rowsAffected = MindWellServices.NonQuery("INSERT INTO LearningModules (ModuleName, Description, OrderIndex, CreatedDate) VALUES (@Name,@Description,@OrderIndex,GETDATE())", new SqlParameter("@Name", name), new SqlParameter("@Description", description), new SqlParameter("@OrderIndex", orderIndex));
                SecurityHelper.LogAction(SecurityHelper.GetCurrentUserId(), "AdminModuleInsert", "LearningModules", 0, null, name);
                return rowsAffected > 0 ? "<div class='alert alert-success'>Module added.</div>" : "<div class='alert alert-warning'>Module was not added.</div>";
            }

            if (id <= 0)
                return "<div class='alert alert-danger'>Invalid module selected.</div>";

            if (action == "update")
            {
                string validation = ValidateModuleInput(name, description, orderIndex);
                if (!string.IsNullOrEmpty(validation))
                    return validation;

                int rowsAffected = MindWellServices.NonQuery("UPDATE LearningModules SET ModuleName=@Name, Description=@Description, OrderIndex=@OrderIndex, UpdatedDate=GETDATE() WHERE ModuleID=@ModuleID", new SqlParameter("@Name", name), new SqlParameter("@Description", description), new SqlParameter("@OrderIndex", orderIndex), new SqlParameter("@ModuleID", id));
                SecurityHelper.LogAction(SecurityHelper.GetCurrentUserId(), "AdminModuleUpdate", "LearningModules", id, null, name);
                return rowsAffected > 0 ? "<div class='alert alert-success'>Module updated.</div>" : "<div class='alert alert-warning'>Module was not updated.</div>";
            }

            if (action == "archive")
            {
                int rowsAffected = MindWellServices.NonQuery("UPDATE LearningModules SET ModuleName=CASE WHEN ModuleName LIKE '[[]Archived] %' THEN ModuleName ELSE '[Archived] ' + ModuleName END, OrderIndex=OrderIndex + 1000, UpdatedDate=GETDATE() WHERE ModuleID=@ModuleID", new SqlParameter("@ModuleID", id));
                SecurityHelper.LogAction(SecurityHelper.GetCurrentUserId(), "AdminModuleArchive", "LearningModules", id, null, "archive");
                return rowsAffected > 0 ? "<div class='alert alert-warning'>Module archived.</div>" : "<div class='alert alert-warning'>Module was not archived.</div>";
            }

            if (action == "delete")
            {
                // Deletion is only allowed when no learning content or user progress depends on the module.
                bool? hasRelatedData = ModuleHasRelatedData(id);
                if (!hasRelatedData.HasValue)
                    return MindWellServices.RenderDataUnavailable("Module relationships could not be verified, so the module was not deleted.");
                if (hasRelatedData.Value)
                    return "<div class='alert alert-warning'>Module has related articles, quizzes, or progress. Archive it instead, or remove related data first.</div>";

                int rowsAffected = MindWellServices.NonQuery("DELETE FROM LearningModules WHERE ModuleID=@ModuleID", new SqlParameter("@ModuleID", id));
                SecurityHelper.LogAction(SecurityHelper.GetCurrentUserId(), "AdminModuleDelete", "LearningModules", id, null, "delete");
                return rowsAffected > 0 ? "<div class='alert alert-success'>Module deleted.</div>" : "<div class='alert alert-warning'>Module was not deleted.</div>";
            }

            return string.Empty;
        }

        private string ValidateModuleInput(string name, string description, int orderIndex)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
                return "<div class='alert alert-danger'>Module name and description are required.</div>";

            if (orderIndex < 1 || orderIndex > 999)
                return "<div class='alert alert-danger'>Module order must be between 1 and 999.</div>";

            return string.Empty;
        }

        private bool? ModuleHasRelatedData(int moduleId)
        {
            ScalarResult articleResult = MindWellServices.TryScalar("SELECT COUNT(*) FROM Articles WHERE ModuleID=@ModuleID", new SqlParameter("@ModuleID", moduleId));
            ScalarResult quizResult = MindWellServices.TryScalar("SELECT COUNT(*) FROM Quizzes WHERE ModuleID=@ModuleID", new SqlParameter("@ModuleID", moduleId));
            ScalarResult progressResult = MindWellServices.TryScalar("SELECT COUNT(*) FROM UserProgress WHERE ModuleID=@ModuleID", new SqlParameter("@ModuleID", moduleId));
            if (articleResult.HasError || quizResult.HasError || progressResult.HasError)
                return null;

            int articleCount = MindWellServices.ToInt(articleResult.Value, 0);
            int quizCount = MindWellServices.ToInt(quizResult.Value, 0);
            int progressCount = MindWellServices.ToInt(progressResult.Value, 0);
            return articleCount + quizCount + progressCount > 0;
        }

        private void AppendModuleDisplayRow(StringBuilder sb, DataRow row)
        {
            // Default rows are read-only so module text stays readable until edit mode is requested.
            int id = MindWellServices.ToInt(row["ModuleID"], 0);
            sb.Append("<tr><td><span class='field-value'>").Append(MindWellServices.Encode(row["ModuleName"]))
                .Append("</span></td><td><span class='field-value'>").Append(MindWellServices.Encode(row["Description"]))
                .Append("</span></td><td><span class='field-value'>").Append(row["OrderIndex"])
                .Append("</span></td><td><div class='button-row'><a class='btn btn-primary btn-sm' href='ManageModules.aspx?edit=").Append(id)
                .Append("'>Edit</a><button formnovalidate data-confirm='Are you sure you want to archive this module? It will be hidden from learners.' name='moduleAction' value='archive:").Append(id)
                .Append("' class='btn btn-sm'>Archive</button><button formnovalidate data-confirm='Are you sure you want to delete this test record?' name='moduleAction' value='delete:").Append(id)
                .Append("' class='btn btn-danger btn-sm'>Delete</button></div></td></tr>");
        }

        private void AppendModuleEditRow(StringBuilder sb, DataRow row)
        {
            // Edit rows expose all module fields and reuse the same action column as display rows.
            int id = MindWellServices.ToInt(row["ModuleID"], 0);
            string description = Convert.ToString(row["Description"]);
            sb.Append("<tr class='editing-row'><td><label for='moduleName_").Append(id).Append("'>Name</label><input id='moduleName_").Append(id).Append("' name='moduleName_").Append(id).Append("' autocomplete='off' aria-describedby='moduleEditHelp_").Append(id).Append("' value='").Append(MindWellServices.Encode(row["ModuleName"])).Append("' data-validation-group='module-edit' /></td><td><label for='moduleDescription_").Append(id).Append("'>Description</label><textarea id='moduleDescription_").Append(id).Append("' class='module-description' name='description_").Append(id).Append("' autocomplete='off' aria-describedby='moduleEditHelp_").Append(id).Append("' rows='").Append(GetTextareaRows(description)).Append("' data-validation-group='module-edit'>").Append(MindWellServices.Encode(description)).Append("</textarea></td><td><label for='moduleOrderIndex_").Append(id).Append("'>Order</label><input id='moduleOrderIndex_").Append(id).Append("' type='number' name='orderIndex_").Append(id).Append("' autocomplete='off' aria-describedby='moduleEditHelp_").Append(id).Append("' value='").Append(row["OrderIndex"]).Append("' /><small id='moduleEditHelp_").Append(id).Append("' class='form-helper'>Update fields, then save this module.</small></td><td><div class='button-row'><button name='moduleAction' value='update:").Append(id).Append("' class='btn btn-primary btn-sm'>Save</button><a class='btn btn-sm' href='ManageModules.aspx'>Cancel</a><button formnovalidate data-confirm='Are you sure you want to archive this module? It will be hidden from learners.' name='moduleAction' value='archive:").Append(id).Append("' class='btn btn-sm'>Archive</button><button formnovalidate data-confirm='Are you sure you want to delete this test record?' name='moduleAction' value='delete:").Append(id).Append("' class='btn btn-danger btn-sm'>Delete</button></div></td></tr>");
        }

        private int GetTextareaRows(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 4;

            int lineCount = value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
            int wrappedLineCount = (value.Length / 48) + 1;
            int rows = Math.Max(lineCount, wrappedLineCount);
            return Math.Min(Math.Max(rows, 4), 10);
        }
    }
}
