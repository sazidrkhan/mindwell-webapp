// Purpose: Registers new learner accounts with validation, password hashing, and duplicate-account checks.
using System;
using System.Data;
using System.Data.SqlClient;

namespace MindWell.Auth
{
    public partial class Register : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Load register page
            }
        }

        protected void RegisterButton_Click(object sender, EventArgs e)
        {
            // Applies server-side validation before any database insert so bad registrations fail safely.
            if (!SecurityHelper.ValidateCsrfOrEndRequest())
                return;

            // Validate form
            if (!Page.IsValid)
            {
                return;
            }

            string username = (UsernameTextBox.Text ?? string.Empty).Trim();
            string email = SecurityHelper.SafeTrim(EmailTextBox.Text, 100);
            string password = SecurityHelper.SafeTrim(PasswordTextBox.Text, 100);
            string confirmPassword = SecurityHelper.SafeTrim(ConfirmPasswordTextBox.Text, 100);

            if (!TermsCheckBox.Checked)
            {
                ShowError("Please confirm that you agree to the platform terms and privacy practices.");
                return;
            }

            if (username.Length < 3 || username.Length > 50)
            {
                ShowError("Username must be between 3 and 50 characters.");
                return;
            }

            if (!SecurityHelper.IsValidUsername(username))
            {
                ShowError("Username must use only letters, numbers, or underscores.");
                return;
            }

            if (!SecurityHelper.IsValidEmail(email))
            {
                ShowError("Please enter a valid email address.");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Passwords do not match.");
                return;
            }

            if (!PasswordHelper.IsValidPassword(password))
            {
                ShowError("Password must be at least 8 characters and include uppercase, lowercase, number, and special character.");
                return;
            }

            object existing = MindWellServices.Scalar("SELECT COUNT(*) FROM Users WHERE Username = @Username OR Email = @Email",
                new SqlParameter("@Username", username),
                new SqlParameter("@Email", email));

            if (MindWellServices.ToInt(existing, 0) > 0)
            {
                ShowError("That username or email is already registered.");
                return;
            }

            string passwordHash = PasswordHelper.HashPassword(password);
            // New registrations are inserted as active standard users; administrator changes happen separately.
            int inserted = MindWellServices.NonQuery(@"
INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, RoleID, CreatedDate, IsActive, IsBanned, PreferredTheme, TotpSecret, IsTotpEnabled)
VALUES (@Username, @Email, @PasswordHash, @FirstName, @LastName, 2, GETDATE(), 1, 0, 'light', NULL, 0)",
                new SqlParameter("@Username", username),
                new SqlParameter("@Email", email),
                new SqlParameter("@PasswordHash", passwordHash),
                new SqlParameter("@FirstName", username),
                new SqlParameter("@LastName", string.Empty));

            if (inserted == 0)
            {
                ShowError("Registration failed. Please verify the database is initialized and try again.");
                return;
            }

            if (IsSafeReturnUrl(Request.QueryString["returnUrl"]))
                Response.Redirect("~/Auth/Login.aspx?registered=1&returnUrl=" + Server.UrlEncode(Request.QueryString["returnUrl"]), false);
            else
                Response.Redirect("~/Auth/Login.aspx?registered=1", false);
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

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorPanel.Visible = true;
        }
    }
}
