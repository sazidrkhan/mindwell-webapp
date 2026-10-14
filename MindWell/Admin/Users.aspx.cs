// Purpose: Admin user-management page for searching users, editing profile fields,
// changing roles/status, and deactivating accounts through a table-based UI.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.Admin
{
    public partial class Users : AdminPageBase
    {
        private static readonly string ExcludeTestDataWhere = MindWellServices.ExcludeTestDataWhere("u.Username", "u.Email");

        protected string RenderUsers()
        {
            // Handles any submitted admin action first, then reloads the table so changes are reflected immediately.
            string message = HandleUserPost();
            if (Response.IsRequestBeingRedirected)
                return string.Empty;
            if (string.IsNullOrEmpty(message))
                message = TakeUsersMessage();

            string search = SecurityHelper.SafeTrim(Request.QueryString["q"], 50);
            int editUserId = MindWellServices.ToInt(Request.QueryString["edit"], 0);
            int resetRequestId = MindWellServices.ToInt(Request.QueryString["resetRequest"], 0);
            QueryResult rolesResult = MindWellServices.TryQuery("SELECT RoleID, RoleName FROM UserRoles ORDER BY RoleID");
            QueryResult usersResult = MindWellServices.TryQuery(@"SELECT u.UserID, u.Username, u.Email, u.FirstName, u.LastName, u.RoleID, r.RoleName, u.IsActive, u.IsBanned, u.CreatedDate FROM Users u INNER JOIN UserRoles r ON u.RoleID=r.RoleID WHERE " + ExcludeTestDataWhere + @" AND (@Search='' OR u.Username LIKE '%' + @Search + '%' OR u.Email LIKE '%' + @Search + '%') ORDER BY u.CreatedDate DESC", new SqlParameter("@Search", search));
            if (rolesResult.HasError || usersResult.HasError)
                return message + MindWellServices.RenderDataUnavailable("User management data could not be loaded.");

            DataTable roles = rolesResult.Data;
            DataTable users = usersResult.Data;

            var sb = new StringBuilder("<p class='back-link'><a class='btn btn-outline btn-sm' href='Dashboard.aspx'>Back to Admin Dashboard</a></p><section class='section-header'><h1>User Management</h1><p>Search, update profile fields, change roles, and control user account status.</p></section>")
                .Append(message)
                .Append(RenderResetRequestAlert(resetRequestId, editUserId))
                .Append(RenderPasswordResetNotification())
                .Append("<section class='card'><div class='form-group'><label for='userSearch'>Search users</label><div class='button-row'><input id='userSearch' name='q' autocomplete='off' placeholder='Search users' value='")
                .Append(MindWellServices.Encode(search))
                .Append("' /> <button type='button' class='btn btn-primary' onclick=\"window.location='Users.aspx?q='+encodeURIComponent(document.getElementById('userSearch').value)\">Search</button></div></div><div class='table-scroll table-responsive'><table class='data-table admin-users-table'><tr><th>Username</th><th>Email</th><th>First Name / Last Name</th><th>Role</th><th>Status</th><th>Actions</th></tr>");

            foreach (DataRow row in users.Rows)
            {
                int id = MindWellServices.ToInt(row["UserID"], 0);
                if (id == editUserId)
                    AppendUserEditRow(sb, row, roles);
                else
                    AppendUserDisplayRow(sb, row);
            }

            return sb.Append("</table></div></section>").ToString();
        }

        private string HandleUserPost()
        {
            // Central POST handler for edit-mode saves and account-state actions.
            if (!IsPostBack || Request.Form["userAction"] == null)
                return string.Empty;

            string[] parts = Request.Form["userAction"].Split(':');
            string action = parts.Length > 0 ? parts[0] : string.Empty;
            int id = parts.Length > 1 ? MindWellServices.ToInt(parts[1], 0) : MindWellServices.ToInt(Request.Form["userId"], 0);
            int resetRequestId = MindWellServices.ToInt(Request.QueryString["resetRequest"], 0);
            int currentAdminId = SecurityHelper.GetCurrentUserId() ?? 0;

            if (id <= 0)
                return "<div class='alert alert-danger'>Invalid user selected.</div>";

            if ((action == "delete" || action == "ban") && id == currentAdminId)
                return "<div class='alert alert-warning'>You cannot deactivate your own admin account.</div>";

            if (action == "save")
            {
                string username = SecurityHelper.SafeTrim(Request.Form["username_" + id], 100);
                if (!SecurityHelper.IsValidUsernameLength(username))
                    return "<div class='alert alert-danger'>Username must be between 3 and 50 characters.</div>";

                // Save uses the editable row fields to update profile, role, active, and banned status together.
                MindWellServices.NonQuery(@"UPDATE Users SET Username=@Username, Email=@Email, FirstName=@FirstName, LastName=@LastName, RoleID=@RoleID, IsActive=@IsActive, IsBanned=@IsBanned WHERE UserID=@UserID",
                    new SqlParameter("@Username", username),
                    new SqlParameter("@Email", SecurityHelper.SafeTrim(Request.Form["email_" + id], 100)),
                    new SqlParameter("@FirstName", SecurityHelper.SafeTrim(Request.Form["firstName_" + id], 50)),
                    new SqlParameter("@LastName", SecurityHelper.SafeTrim(Request.Form["lastName_" + id], 50)),
                    new SqlParameter("@RoleID", MindWellServices.ToInt(Request.Form["roleId_" + id], 2)),
                    new SqlParameter("@IsActive", Request.Form["isActive_" + id] == "1"),
                    new SqlParameter("@IsBanned", Request.Form["isBanned_" + id] == "1"),
                    new SqlParameter("@UserID", id));
                SecurityHelper.LogAction(currentAdminId, "AdminUserUpdate", "Users", id, null, "save");
                SetUsersMessage("User updated.");
                RedirectUsersPage(id);
                return string.Empty;
            }

            if (action == "resetPwd")
            {
                string tempPassword = Request.Form["tempPassword"] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(tempPassword) || tempPassword.Length < 8)
                    return "<div class='alert alert-danger'>Temporary password must be at least 8 characters.</div>";

                MindWellServices.NonQuery("UPDATE Users SET PasswordHash=@h, PasswordResetRequired=1 WHERE UserID=@id",
                    new SqlParameter("@h", PasswordHelper.HashPassword(tempPassword)),
                    new SqlParameter("@id", id));
                if (resetRequestId > 0)
                    MarkPasswordResetRequestHandled(resetRequestId, id, currentAdminId);
                SecurityHelper.LogAction(currentAdminId, "AdminTempPasswordSet", "Users", id, null, "resetPwd");
                SetUsersMessage("Temporary password set. The user must set a new password at next sign in.");
                RedirectUsersPage(id);
                return string.Empty;
            }

            if (action == "ban")
                MindWellServices.NonQuery("UPDATE Users SET IsBanned=1 WHERE UserID=@UserID", new SqlParameter("@UserID", id));
            if (action == "activate")
                MindWellServices.NonQuery("UPDATE Users SET IsActive=1, IsBanned=0 WHERE UserID=@UserID", new SqlParameter("@UserID", id));
            if (action == "delete")
                MindWellServices.NonQuery("UPDATE Users SET IsActive=0 WHERE UserID=@UserID", new SqlParameter("@UserID", id));

            SecurityHelper.LogAction(currentAdminId, "AdminUserAction", "Users", id, null, Request.Form["userAction"]);
            SetUsersMessage("User action completed.");
            RedirectUsersPage(id);
            return string.Empty;
        }

        private string TakeUsersMessage()
        {
            object message = Session["UsersMsg"];
            if (message == null)
                return string.Empty;

            Session.Remove("UsersMsg");
            return "<div class='alert alert-success'>" + MindWellServices.Encode(message) + "</div>";
        }

        private void SetUsersMessage(string message)
        {
            Session["UsersMsg"] = message;
        }

        private void RedirectUsersPage()
        {
            RedirectUsersPage(0);
        }

        private void RedirectUsersPage(int focusUserId)
        {
            string target = focusUserId > 0 ? "Users.aspx#user-" + focusUserId : "Users.aspx";
            Response.Redirect(target, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void AppendUserDisplayRow(StringBuilder sb, DataRow row)
        {
            // Read-only rows keep the management table compact until an administrator clicks Edit.
            int id = MindWellServices.ToInt(row["UserID"], 0);
            bool active = Convert.ToBoolean(row["IsActive"]);
            bool banned = Convert.ToBoolean(row["IsBanned"]);
            string status = active ? "Active" : "Inactive";
            string access = banned ? "Banned" : "Allowed";

            sb.Append("<tr id='user-").Append(id).Append("'><td><span class='field-value'>").Append(MindWellServices.Encode(row["Username"]))
                .Append("</span></td><td><span class='field-value'>").Append(MindWellServices.Encode(row["Email"]))
                .Append("</span></td><td><span class='field-value'>").Append(MindWellServices.Encode(row["FirstName"]))
                .Append(" ").Append(MindWellServices.Encode(row["LastName"]))
                .Append("</span></td><td><span class='field-value'>").Append(MindWellServices.Encode(row["RoleName"]))
                .Append("</span></td><td><span class='field-value'>").Append(status).Append("</span><span class='field-value muted'>").Append(access)
                .Append("</span></td><td><div class='button-row'><a class='btn btn-primary btn-sm' href='Users.aspx?edit=").Append(id)
                .Append("#user-").Append(id).Append("'>Edit</a></div></td></tr>");
        }

        private void AppendUserEditRow(StringBuilder sb, DataRow row, DataTable roles)
        {
            // Edit mode swaps display labels for inputs/selects while preserving the same table column alignment.
            int id = MindWellServices.ToInt(row["UserID"], 0);
            bool active = Convert.ToBoolean(row["IsActive"]);
            bool banned = Convert.ToBoolean(row["IsBanned"]);

            sb.Append("<tr id='user-").Append(id).Append("' class='editing-row'><td><input type='hidden' id='userId_").Append(id).Append("' name='userId' value='").Append(id).Append("' /><label for='username_").Append(id).Append("'>Username</label><input id='username_").Append(id).Append("' name='username_").Append(id).Append("' autocomplete='username' aria-describedby='userEditHelp_").Append(id).Append("' value='").Append(MindWellServices.Encode(row["Username"])).Append("' /></td><td><label for='email_").Append(id).Append("'>Email</label><input id='email_").Append(id).Append("' name='email_").Append(id).Append("' autocomplete='email' aria-describedby='userEditHelp_").Append(id).Append("' value='").Append(MindWellServices.Encode(row["Email"])).Append("' /></td><td><label for='firstName_").Append(id).Append("'>First name</label><input id='firstName_").Append(id).Append("' name='firstName_").Append(id).Append("' autocomplete='given-name' aria-describedby='userEditHelp_").Append(id).Append("' placeholder='First Name' value='").Append(MindWellServices.Encode(row["FirstName"])).Append("' /><label for='lastName_").Append(id).Append("'>Last name</label><input id='lastName_").Append(id).Append("' name='lastName_").Append(id).Append("' autocomplete='family-name' aria-describedby='userEditHelp_").Append(id).Append("' placeholder='Last Name' value='").Append(MindWellServices.Encode(row["LastName"])).Append("' /><small id='userEditHelp_").Append(id).Append("' class='form-helper'>Update profile fields, role, and account status.</small></td><td><label for='roleId_").Append(id).Append("'>Role</label><select id='roleId_").Append(id).Append("' name='roleId_").Append(id).Append("' autocomplete='off' aria-describedby='userEditHelp_").Append(id).Append("'>");
            AppendRoleOptions(sb, roles, MindWellServices.ToInt(row["RoleID"], 2));
            sb.Append("</select></td><td><label for='isActive_").Append(id).Append("'>Status</label><select id='isActive_").Append(id).Append("' name='isActive_").Append(id).Append("' autocomplete='off' aria-describedby='userEditHelp_").Append(id).Append("'><option value='1'").Append(active ? " selected" : string.Empty).Append(">Active</option><option value='0'").Append(!active ? " selected" : string.Empty).Append(">Inactive</option></select><label for='isBanned_").Append(id).Append("'>Access</label><select id='isBanned_").Append(id).Append("' name='isBanned_").Append(id).Append("' autocomplete='off' aria-describedby='userEditHelp_").Append(id).Append("'><option value='0'").Append(!banned ? " selected" : string.Empty).Append(">Allowed</option><option value='1'").Append(banned ? " selected" : string.Empty).Append(">Banned</option></select></td><td><div class='button-row'><button type='submit' name='userAction' value='save:").Append(id).Append("' class='btn btn-primary btn-sm'>Save</button><a class='btn btn-sm' href='Users.aspx#user-").Append(id).Append("'>Cancel</a></div></td></tr>")
                .Append("<tr class='editing-row password-reset-row'><td colspan='6'><div class='password-reset-panel'><h3>Reset Password</h3><div class='button-row password-reset-actions'><div class='password-field'><label for='tempPassword_").Append(id).Append("'>Temporary password</label><input id='tempPassword_").Append(id).Append("' type='password' name='tempPassword' placeholder='Set temporary password' class='form-control' autocomplete='new-password' aria-describedby='tempPasswordHelp_").Append(id).Append(" passwordGeneratorStatus_").Append(id).Append("' />")
                .Append(PasswordToggleMarkup())
                .Append("</div><button type='button' class='btn btn-outline btn-sm' data-password-generate>Generate</button><button type='submit' name='userAction' value='resetPwd' class='btn btn-primary btn-sm'>Set Temp Password</button></div><small id='passwordGeneratorStatus_").Append(id).Append("' class='form-helper password-generator-status' data-password-generator-status role='status' aria-live='polite'></small><small id='tempPasswordHelp_").Append(id).Append("' class='form-helper'>The user will be asked to choose a new password after signing in.</small></div></td></tr>");
        }

        private string RenderResetRequestAlert(int resetRequestId, int editUserId)
        {
            if (resetRequestId <= 0 || editUserId <= 0)
                return string.Empty;

            QueryResult requestResult = MindWellServices.TryQuery(@"
SELECT TOP 1 c.ContactID, c.Email, c.CreatedDate
FROM Contacts c
INNER JOIN Users u ON c.Email = u.Email
WHERE c.ContactID=@ContactID
  AND c.Subject='Password reset request'
  AND u.UserID=@UserID",
                new SqlParameter("@ContactID", resetRequestId),
                new SqlParameter("@UserID", editUserId));

            if (requestResult.HasError)
                return MindWellServices.RenderDataUnavailable("Password reset request context is temporarily unavailable.");

            DataTable request = requestResult.Data;
            if (request.Rows.Count == 0)
                return "<div class='alert alert-warning'>Password reset request context was not found. Review Manage Feedback if the request still needs attention.</div>";

            return "<div class='alert alert-info'>Password reset request #" + resetRequestId + " for " + MindWellServices.Encode(request.Rows[0]["Email"]) + ". Set a temporary password below, then share it through your approved support process.</div>";
        }

        private string RenderPasswordResetNotification()
        {
            QueryResult requestsResult = MindWellServices.TryQuery(@"
SELECT TOP 5 c.ContactID, c.Email, c.CreatedDate, u.UserID, u.Username
FROM Contacts c
LEFT JOIN Users u ON c.Email = u.Email
WHERE c.Subject = 'Password reset request'
  AND ISNULL(c.IsResolved, 0) = 0
ORDER BY c.CreatedDate DESC");
            ScalarResult requestCountResult = MindWellServices.TryScalar("SELECT COUNT(*) FROM Contacts WHERE Subject = 'Password reset request' AND ISNULL(IsResolved, 0) = 0");
            if (requestsResult.HasError || requestCountResult.HasError)
                return MindWellServices.RenderDataUnavailable("Password reset notifications are temporarily unavailable.");

            DataTable requests = requestsResult.Data;
            int requestCount = MindWellServices.ToInt(requestCountResult.Value, 0);

            var sb = new StringBuilder("<section class='reset-notification-bar' aria-label='Password reset notifications'><button type='button' class='reset-notification-toggle' data-reset-notification-toggle aria-expanded='false' aria-controls='resetNotificationPanel'><span>Password reset requests</span><span class='reset-notification-badge'>")
                .Append(requestCount)
                .Append("</span></button><div id='resetNotificationPanel' class='reset-notification-popover' data-reset-notification-popover hidden><div class='admin-table-summary'><h2>Password Reset Requests</h2><a class='btn btn-outline btn-sm' href='ManageFeedback.aspx?feedbackQuery=password%20reset%20request'>View all requests</a></div>");

            if (requests.Rows.Count == 0)
                return sb.Append("<p>No open reset requests right now.</p></div></section>").ToString();

            sb.Append("<div class='reset-request-list'>");
            foreach (DataRow row in requests.Rows)
            {
                int contactId = MindWellServices.ToInt(row["ContactID"], 0);
                int userId = MindWellServices.ToInt(row["UserID"], 0);
                string email = Convert.ToString(row["Email"]);
                string username = Convert.ToString(row["Username"]);
                sb.Append("<article class='reset-request-card'><div><strong>").Append(MindWellServices.Encode(email)).Append("</strong>");
                if (!string.IsNullOrWhiteSpace(username))
                    sb.Append("<span class='form-helper'>Matched user: ").Append(MindWellServices.Encode(username)).Append("</span>");
                sb.Append("<span class='form-helper'>Requested ").Append(MindWellServices.Encode(row["CreatedDate"])).Append("</span></div><p class='page-actions'>");

                if (userId > 0)
                {
                    sb.Append("<a class='btn btn-primary btn-sm' href='Users.aspx?edit=").Append(userId)
                        .Append("&resetRequest=").Append(contactId).Append("#user-").Append(userId).Append("'>Open user reset</a>");
                }
                else
                {
                    sb.Append("<a class='btn btn-outline btn-sm' href='ManageFeedback.aspx?feedbackQuery=").Append(Server.UrlEncode(email))
                        .Append("&feedbackStatus=New'>Review in feedback</a>");
                }

                sb.Append("</p></article>");
            }

            return sb.Append("</div></div></section>").ToString();
        }

        private void MarkPasswordResetRequestHandled(int resetRequestId, int userId, int adminId)
        {
            string note = "Temporary password set by admin " + adminId + ".";
            MindWellServices.NonQuery(@"
UPDATE c
SET Status='Resolved',
    IsResolved=1,
    UpdatedDate=GETDATE(),
    ResolvedDate=GETDATE(),
    AdminNotes=CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(c.AdminNotes, ''))), '') IS NULL THEN @Note ELSE c.AdminNotes + CHAR(10) + @Note END
FROM Contacts c
INNER JOIN Users u ON c.Email = u.Email
WHERE c.ContactID=@ContactID
  AND c.Subject='Password reset request'
  AND u.UserID=@UserID",
                new SqlParameter("@Note", note),
                new SqlParameter("@ContactID", resetRequestId),
                new SqlParameter("@UserID", userId));
        }

        private string PasswordToggleMarkup()
        {
            return "<button type=\"button\" class='password-toggle password-eye' data-password-toggle aria-label='Show password' aria-pressed='false'><span class='password-eye-open' aria-hidden='true'><svg viewBox='0 0 24 24'><path d='M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z' /><circle cx='12' cy='12' r='2.7' /></svg></span><span class='password-eye-closed' aria-hidden='true'><svg viewBox='0 0 24 24'><path d='M3 3l18 18' /><path d='M10.6 10.6A2.7 2.7 0 0 0 12 14.7c.8 0 1.52-.34 2.02-.88' /><path d='M7.35 7.53C4.28 9.25 2.5 12 2.5 12s3.5 6 9.5 6c1.66 0 3.1-.46 4.31-1.14' /><path d='M14.2 6.25C19.05 7.12 21.5 12 21.5 12s-.78 1.34-2.24 2.73' /></svg></span><span class='sr-only'>Show password</span></button>";
        }

        private void AppendRoleOptions(StringBuilder sb, DataTable roles, int selectedRoleId)
        {
            foreach (DataRow role in roles.Rows)
            {
                int roleId = MindWellServices.ToInt(role["RoleID"], 0);
                sb.Append("<option value='").Append(roleId).Append("'");
                if (roleId == selectedRoleId)
                    sb.Append(" selected");
                sb.Append(">").Append(MindWellServices.Encode(role["RoleName"])).Append("</option>");
            }
        }
    }
}
