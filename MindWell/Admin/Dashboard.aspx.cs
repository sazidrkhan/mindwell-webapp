// Purpose: Admin dashboard summarizing platform counts and linking to management modules.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.Admin
{
    public partial class Dashboard : AdminPageBase
    {
        private const string PendingAdminTotpSecretKey = "PendingAdminTotpSecret";
        private const string AdminMfaSkippedKey = "AdminMfaSkipped";

        protected string RenderDashboard()
        {
            int adminId = SecurityHelper.GetCurrentUserId() ?? 0;
            string adminMfaMessage = HandleAdminMfaPost(adminId);
            if (Response.IsRequestBeingRedirected)
                return string.Empty;

            bool isAdminMfaSkipped = Session[AdminMfaSkippedKey] != null && Session[AdminMfaSkippedKey].ToString() == "1";
            if (!IsAdminTotpEnabled(adminId) && !isAdminMfaSkipped)
                return RenderAdminMfaEnrollment(adminId, adminMfaMessage);
            bool isAdminTotpEnabled = IsAdminTotpEnabled(adminId);
            if (isAdminTotpEnabled)
                Session.Remove(AdminMfaSkippedKey);

            // Dashboard cards use scalar SQL queries to provide quick operational totals for administrators.
            string activeModuleSql = MindWellServices.ActiveModuleCountSql;
            string activeQuizSql = MindWellServices.ActiveQuizCountSql;
            string activeFilterNames = MindWellServices.ActiveModuleWhere + MindWellServices.ActiveQuizWhere;
            if (string.IsNullOrEmpty(activeFilterNames))
                return MindWellServices.RenderDataUnavailable("Active module and quiz filters are unavailable.");

            var sb = new StringBuilder();
            sb.Append("<section class='section-header'><h1>Admin Dashboard</h1><p>Platform overview and recent activity.</p></section>")
                .Append(adminMfaMessage)
                .Append(!isAdminTotpEnabled ? RenderAdminMfaReminder() : string.Empty)
                .Append("<div class='row cols-3'>")
                .Append(Card("Users", "SELECT COUNT(*) FROM Users WHERE IsActive=1"))
                .Append(Card("Modules", activeModuleSql))
                .Append(Card("Resources", "SELECT COUNT(*) FROM Resources WHERE IsPublished=1"))
                .Append(Card("Quizzes", activeQuizSql))
                .Append(Card("Forum Posts", "SELECT COUNT(*) FROM ForumTopics"))
                .Append(Card("Contact Messages", "SELECT COUNT(*) FROM Contacts"))
                .Append("</div>")
                .Append("<section class='card'><h2>Admin Tasks</h2>")
                .Append("<div class='button-row'>")
                .Append("<a class='btn btn-primary' href='Users.aspx'>Manage Users</a>")
                .Append("<a class='btn btn-primary' href='ManageModules.aspx'>Manage Modules</a>")
                .Append("<a class='btn btn-primary' href='Content.aspx'>Manage Resources</a>")
                .Append("<a class='btn btn-primary' href='ManageQuizzes.aspx'>Manage Quizzes</a>")
                .Append("<a class='btn btn-primary' href='ManageFeedback.aspx'>Manage Feedback</a>")
                .Append("</div></section>");
            return sb.ToString();
        }

        private string Card(string title, string sql)
        {
            return "<div class='card stat-card'><h2>" + title + "</h2><p class='stat-number'>" + MindWellServices.ToInt(MindWellServices.Scalar(sql), 0) + "</p></div>";
        }

        private string HandleAdminMfaPost(int adminId)
        {
            if (!IsPostBack)
                return string.Empty;

            string adminMfaAction = Request.Unvalidated.Form["adminMfaAction"];
            if (adminMfaAction == "startAdminTotp")
            {
                Session.Remove(AdminMfaSkippedKey);
                Session[PendingAdminTotpSecretKey] = TotpHelper.GenerateSecret();
                return "<div class='alert alert-info'>Admin MFA required. Scan the QR code and enter the generated code.</div>";
            }

            if (adminMfaAction == "skip")
            {
                Session[AdminMfaSkippedKey] = "1";
                Session.Remove(PendingAdminTotpSecretKey);
                SecurityHelper.LogAction(adminId, "SkipAdminMfa", "Users", adminId, null, "Admin skipped multi-factor authentication setup for now");
                Response.Redirect("Dashboard.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return string.Empty;
            }

            if (adminMfaAction != "confirmAdminTotp")
                return string.Empty;

            string pendingSecret = Session[PendingAdminTotpSecretKey] as string;
            if (string.IsNullOrWhiteSpace(pendingSecret))
                return "<div class='alert alert-danger'>Admin MFA setup expired. Start again to generate a fresh QR code.</div>";

            if (!TotpHelper.VerifyCode(pendingSecret, Request.Unvalidated.Form["adminTotpCode"]))
                return "<div class='alert alert-danger'>That authenticator code was not valid. Please try again.</div>";

            MindWellServices.NonQuery("UPDATE Users SET TotpSecret=@TotpSecret, IsTotpEnabled=1 WHERE UserID=@UserID",
                new SqlParameter("@TotpSecret", pendingSecret),
                new SqlParameter("@UserID", adminId));
            Session.Remove(PendingAdminTotpSecretKey);
            Session.Remove(AdminMfaSkippedKey);
            SecurityHelper.LogAction(adminId, "EnableAdminMfa", "Users", adminId, null, "Admin enabled required multi-factor authentication");
            return "<div class='alert alert-success'>Admin MFA is enabled.</div>";
        }

        private bool IsAdminTotpEnabled(int adminId)
        {
            object enabled = MindWellServices.Scalar("SELECT IsTotpEnabled FROM Users WHERE UserID=@UserID", new SqlParameter("@UserID", adminId));
            return enabled != null && enabled != DBNull.Value && Convert.ToBoolean(enabled);
        }

        private string RenderAdminMfaEnrollment(int adminId, string message)
        {
            string pendingSecret = Session[PendingAdminTotpSecretKey] as string;
            if (string.IsNullOrWhiteSpace(pendingSecret))
            {
                pendingSecret = TotpHelper.GenerateSecret();
                Session[PendingAdminTotpSecretKey] = pendingSecret;
            }

            string username = SecurityHelper.GetCurrentUsername() ?? "admin";
            string uri = TotpHelper.GenerateProvisioningUri(username, pendingSecret, "MindWell Admin");
            var sb = new StringBuilder();
            sb.Append("<section class='card totp-card admin-mfa-card'>")
                .Append("<div class='security-status security-status-pending'>Admin MFA required</div>")
                .Append("<h1>Secure Admin Dashboard</h1>")
                .Append("<p>Administrators must set up authenticator-app verification before viewing management data.</p>")
                .Append(message)
                .Append(TotpHelper.GenerateQrCodeSvg(uri))
                .Append("<p class='totp-secret'>Manual key: <code>").Append(MindWellServices.Encode(pendingSecret)).Append("</code></p>")
                .Append("<div class='form-group'><label>6-digit authenticator code</label><input class='totp-code-input' name='adminTotpCode' maxlength='6' inputmode='numeric' autocomplete='one-time-code' /></div>")
                .Append("<div class='button-row totp-action-row'><button class='btn btn-primary admin-mfa-button totp-action-button' type='submit' name='adminMfaAction' value='confirmAdminTotp'>Enable MFA and open dashboard</button>")
                .Append("<button class='btn btn-outline admin-mfa-button totp-action-button' type='submit' name='adminMfaAction' value='startAdminTotp'>Generate a new QR code</button>")
                .Append("<button class='btn btn-outline admin-mfa-button totp-action-button' type='submit' name='adminMfaAction' value='skip'>Skip for now</button></div>")
                .Append("</section>");
            return sb.ToString();
        }

        private string RenderAdminMfaReminder()
        {
            return "<div class='alert alert-warning'>Multi-factor authentication is not enabled for this admin account. Visit <a href='../User/Profile.aspx'>Profile</a> to set it up when you are ready.</div>";
        }
    }
}
