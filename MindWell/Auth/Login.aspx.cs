// Purpose: Handles sign-in, logout redirection, session setup, and login CSRF lifecycle.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.Security;

namespace MindWell.Auth
{
    public partial class Login : System.Web.UI.Page
    {
        private const string PendingMfaUserIdKey = "PendingMfaUserID";
        private const string PendingMfaUsernameKey = "PendingMfaUsername";
        private const string PendingMfaFirstNameKey = "PendingMfaFirstName";
        private const string PendingMfaRoleKey = "PendingMfaRole";
        private const string PendingMfaRememberKey = "PendingMfaRememberMe";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Logout always redirects to a fresh login request so abandoned sessions do not render stale CSRF tokens.
            if (Request.QueryString["logout"] == "1")
            {
                SecurityHelper.ClearUserSession();
                ClearPendingMfa();
                Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
                Response.Cache.SetNoStore();
                Response.Redirect("~/Auth/Login.aspx?signedout=1", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                SecurityHelper.ResetCsrfToken();
                ClearPendingMfa();
            }

            if (Request.QueryString["signedout"] == "1")
            {
                ErrorLabel.Text = "You have been signed out.";
                ErrorPanel.Visible = true;
            }

            if (Request.QueryString["registered"] == "1")
            {
                ErrorLabel.Text = "Account created. Please sign in.";
                ErrorPanel.CssClass = "alert alert-success";
                ErrorPanel.Visible = true;
            }

            if (SecurityHelper.IsUserAuthenticated())
            {
                if (IsSafeReturnUrl(Request.QueryString["returnUrl"]))
                    Response.Redirect(Request.QueryString["returnUrl"], false);
                else
                    Response.Redirect(SecurityHelper.IsUserAdmin() ? "~/Admin/Dashboard.aspx" : "~/User/Dashboard.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                // Load login page
            }
        }

        protected void LoginButton_Click(object sender, EventArgs e)
        {
            // Validates the submitted credentials, restores session state, and routes users by role.
            if (!SecurityHelper.ValidateCsrfOrEndRequest())
                return;

            if (string.Equals(Request.Form["loginStep"], "mfa", StringComparison.OrdinalIgnoreCase))
            {
                HandleMfaVerification();
                return;
            }

            HandlePasswordLogin();
        }

        private void HandlePasswordLogin()
        {
            // Validate form
            if (!Page.IsValid)
            {
                return;
            }

            string username = SecurityHelper.SafeTrim(UsernameTextBox.Text, 50);
            string password = SecurityHelper.SafeTrim(PasswordTextBox.Text, 100);

            DataTable user = MindWellServices.Query(@"
SELECT TOP 1 u.UserID, u.Username, u.PasswordHash, u.FirstName, u.IsActive, u.IsBanned, u.TotpSecret, u.IsTotpEnabled, r.RoleName
FROM Users u
INNER JOIN UserRoles r ON u.RoleID = r.RoleID
WHERE u.Username = @Username OR u.Email = @Username",
                new SqlParameter("@Username", username));

            if (user.Rows.Count == 0 || !PasswordHelper.VerifyPassword(password, user.Rows[0]["PasswordHash"].ToString()))
            {
                ErrorLabel.Text = "Invalid username or password.";
                ErrorPanel.Visible = true;
                return;
            }

            if (!Convert.ToBoolean(user.Rows[0]["IsActive"]) || Convert.ToBoolean(user.Rows[0]["IsBanned"]))
            {
                ErrorLabel.Text = "This account is not currently active. Please contact support.";
                ErrorPanel.Visible = true;
                return;
            }

            int userId = Convert.ToInt32(user.Rows[0]["UserID"]);
            string storedUsername = user.Rows[0]["Username"].ToString();
            string firstName = user.Rows[0]["FirstName"].ToString();
            string role = user.Rows[0]["RoleName"].ToString();
            bool isTotpEnabled = user.Rows[0]["IsTotpEnabled"] != DBNull.Value && Convert.ToBoolean(user.Rows[0]["IsTotpEnabled"]);
            string totpSecret = user.Rows[0]["TotpSecret"] == DBNull.Value ? string.Empty : user.Rows[0]["TotpSecret"].ToString();

            if (isTotpEnabled && !string.IsNullOrWhiteSpace(totpSecret))
            {
                Session["PendingMfaUserID"] = userId;
                Session["PendingMfaUsername"] = storedUsername;
                Session["PendingMfaFirstName"] = firstName;
                Session["PendingMfaRole"] = role;
                Session["PendingMfaRememberMe"] = RememberMeCheckBox.Checked;
                ShowMfaChallenge("Password accepted. Enter your authenticator code to continue.");
                return;
            }

            BeginAuthenticatedSession(userId, storedUsername, firstName, role, RememberMeCheckBox.Checked, "Successful login");
        }

        private void HandleMfaVerification()
        {
            object pendingId = Session[PendingMfaUserIdKey];
            int userId;
            if (pendingId == null || !int.TryParse(pendingId.ToString(), out userId))
            {
                ShowCredentialStepError("Your verification session expired. Please sign in again.");
                return;
            }

            DataTable user = MindWellServices.Query(@"
SELECT TOP 1 u.UserID, u.Username, u.FirstName, u.TotpSecret, u.IsTotpEnabled, u.IsActive, u.IsBanned, r.RoleName
FROM Users u
INNER JOIN UserRoles r ON u.RoleID = r.RoleID
WHERE u.UserID = @UserID",
                new SqlParameter("@UserID", userId));

            if (user.Rows.Count == 0 || !Convert.ToBoolean(user.Rows[0]["IsActive"]) || Convert.ToBoolean(user.Rows[0]["IsBanned"]))
            {
                ClearPendingMfa();
                ShowCredentialStepError("This account is not currently active. Please contact support.");
                return;
            }

            string totpSecret = user.Rows[0]["TotpSecret"] == DBNull.Value ? string.Empty : user.Rows[0]["TotpSecret"].ToString();
            bool isTotpEnabled = user.Rows[0]["IsTotpEnabled"] != DBNull.Value && Convert.ToBoolean(user.Rows[0]["IsTotpEnabled"]);
            if (!isTotpEnabled || string.IsNullOrWhiteSpace(totpSecret) || !TotpHelper.VerifyCode(totpSecret, MfaCodeTextBox.Text))
            {
                ShowMfaChallenge("Invalid authenticator code. Please try again.");
                return;
            }

            bool remember = Session[PendingMfaRememberKey] != null && Convert.ToBoolean(Session[PendingMfaRememberKey]);
            BeginAuthenticatedSession(
                userId,
                user.Rows[0]["Username"].ToString(),
                user.Rows[0]["FirstName"].ToString(),
                user.Rows[0]["RoleName"].ToString(),
                remember,
                "Successful login with multi-factor authentication");
        }

        private void BeginAuthenticatedSession(int userId, string username, string firstName, string role, bool rememberMe, string auditMessage)
        {
            ClearPendingMfa();
            Session["UserID"] = userId;
            Session["Username"] = username;
            Session["UserRole"] = role;
            Session["FirstName"] = firstName;
            FormsAuthentication.SetAuthCookie(username, rememberMe);

            bool passwordResetRequired = IsPasswordResetRequired(userId);
            if (passwordResetRequired)
                Session["ForcePasswordChange"] = true;
            else
                Session.Remove("ForcePasswordChange");

            MindWellServices.NonQuery("UPDATE Users SET LastLoginDate = GETDATE() WHERE UserID = @UserID", new SqlParameter("@UserID", userId));
            SecurityHelper.LogAction(userId, "Login", "Users", userId, null, auditMessage);

            if (passwordResetRequired)
                Response.Redirect("~/User/ChangePassword.aspx?force=1", false);
            else if (IsSafeReturnUrl(Request.QueryString["returnUrl"]))
                Response.Redirect(Request.QueryString["returnUrl"], false);
            else
                Response.Redirect(role == "Admin" ? "~/Admin/Dashboard.aspx" : "~/User/Dashboard.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private static bool IsSafeReturnUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!url.StartsWith("/") && !url.StartsWith("~/")) return false;
            if (url.StartsWith("//")) return false;
            if (url.Contains("://")) return false;
            if (url.IndexOf(':') >= 0) return false;
            return true;
        }

        protected string RenderRegisterUrl()
        {
            string returnUrl = Request.QueryString["returnUrl"];
            return IsSafeReturnUrl(returnUrl)
                ? "Register.aspx?returnUrl=" + Server.UrlEncode(returnUrl)
                : "Register.aspx";
        }

        private bool IsPasswordResetRequired(int userId)
        {
            object required = MindWellServices.Scalar("SELECT PasswordResetRequired FROM Users WHERE UserID=@UserID", new SqlParameter("@UserID", userId));
            return required != null && required != DBNull.Value && Convert.ToBoolean(required);
        }

        private void ShowMfaChallenge(string message)
        {
            CredentialPanel.Visible = false;
            MfaPanel.Visible = true;
            ErrorPanel.CssClass = message.IndexOf("accepted", StringComparison.OrdinalIgnoreCase) >= 0 ? "alert alert-success" : "alert alert-danger";
            ErrorLabel.Text = message;
            ErrorPanel.Visible = true;
        }

        private void ShowCredentialStepError(string message)
        {
            CredentialPanel.Visible = true;
            MfaPanel.Visible = false;
            ErrorPanel.CssClass = "alert alert-danger";
            ErrorLabel.Text = message;
            ErrorPanel.Visible = true;
        }

        private void ClearPendingMfa()
        {
            if (Session == null)
                return;

            Session.Remove(PendingMfaUserIdKey);
            Session.Remove(PendingMfaUsernameKey);
            Session.Remove(PendingMfaFirstNameKey);
            Session.Remove(PendingMfaRoleKey);
            Session.Remove(PendingMfaRememberKey);
        }
    }
}
